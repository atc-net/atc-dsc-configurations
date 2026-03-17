namespace Atc.Dsc.Configurations.Cli.Services;

/// <summary>
/// Persists execution history to <c>~/.atc-dsc/history.json</c>.
/// Entries are capped at <see cref="MaxEntries"/> to prevent unbounded growth.
/// </summary>
public sealed class ExecutionHistoryService : IExecutionHistoryService, IDisposable
{
    internal const int MaxEntries = 500;

    private static readonly string DefaultHistoryDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".atc-dsc");

    private readonly string historyDir;
    private readonly string historyPath;
    private readonly List<HistoryEntry> entries;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionHistoryService"/> class.
    /// Loads existing history from disk synchronously.
    /// </summary>
    /// <param name="jsonOptions">the JSON serializer options.</param>
    public ExecutionHistoryService(JsonSerializerOptions jsonOptions)
        : this(jsonOptions, DefaultHistoryDir)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionHistoryService"/> class
    /// with a custom history directory (used for testing).
    /// </summary>
    /// <param name="jsonOptions">the JSON serializer options.</param>
    /// <param name="historyDir">the directory for the history file.</param>
    internal ExecutionHistoryService(
        JsonSerializerOptions jsonOptions,
        string historyDir)
    {
        this.jsonOptions = jsonOptions;
        this.historyDir = historyDir;
        historyPath = Path.Combine(historyDir, "history.json");
        entries = LoadFromDisk();
    }

    /// <inheritdoc />
    public IReadOnlyList<HistoryEntry> GetAll()
        => entries.AsReadOnly();

    /// <inheritdoc />
    public HistoryEntry? GetLatest(string fileName)
    {
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (string.Equals(entries[i].FileName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                return entries[i];
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task RecordAsync(
        ExecutionResult result,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(result);

        var passed = result.Results.Count(r => r.State is ResourceState.Compliant or ResourceState.Changed or ResourceState.Executed or ResourceState.Skipped);
        var failed = result.Results.Count(r => r.State == ResourceState.Failed);

        var entry = new HistoryEntry(
            result.ProfileName,
            fileName,
            result.Mode,
            result.Success,
            result.Results.Count,
            passed,
            failed,
            result.Duration.TotalSeconds,
            DateTimeOffset.UtcNow);

        await semaphore.WaitAsync();
        try
        {
            entries.Add(entry);
            TrimEntries();
            await SaveToDiskAsync();
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void TrimEntries()
    {
        if (entries.Count > MaxEntries)
        {
            entries.RemoveRange(0, entries.Count - MaxEntries);
        }
    }

    private List<HistoryEntry> LoadFromDisk()
    {
        if (!File.Exists(historyPath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(historyPath);
            return JsonSerializer.Deserialize<List<HistoryEntry>>(json, jsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        semaphore.Dispose();
    }

    private Task SaveToDiskAsync()
    {
        Directory.CreateDirectory(historyDir);
        var json = JsonSerializer.Serialize(entries, jsonOptions);
        return File.WriteAllTextAsync(historyPath, json);
    }
}