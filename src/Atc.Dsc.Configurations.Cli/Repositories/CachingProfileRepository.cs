namespace Atc.Dsc.Configurations.Cli.Repositories;

/// <summary>
/// Decorator around any <see cref="IProfileRepository"/> that stores fetched
/// profiles in <c>%LOCALAPPDATA%\atc-dsc\cache\</c> with a configurable TTL.
/// Avoids redundant GitHub API calls within the TTL window.
/// </summary>
public sealed class CachingProfileRepository : IProfileRepository, IDisposable
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);
    private readonly IProfileRepository inner;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly string cacheDir;
    private readonly TimeSpan ttl;

    public CachingProfileRepository(
        IProfileRepository inner,
        JsonSerializerOptions jsonOptions,
        string? cacheDir = null,
        TimeSpan? ttl = null)
    {
        this.inner = inner;
        this.jsonOptions = jsonOptions;
        this.cacheDir = cacheDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "atc-dsc",
            "cache");
        this.ttl = ttl ?? DefaultTtl;

        Directory.CreateDirectory(this.cacheDir);
    }

    public async Task<IReadOnlyList<ProfileSummary>> ListProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        await semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            var indexPath = Path.Combine(cacheDir, "profiles-index.json");

            if (File.Exists(indexPath) &&
                !IsExpired(indexPath))
            {
                var cached = await File.ReadAllTextAsync(indexPath, cancellationToken);
                var profiles = JsonSerializer.Deserialize<List<ProfileSummary>>(cached, jsonOptions);
                if (profiles is not null)
                {
                    return profiles;
                }
            }

            var result = await inner.ListProfilesAsync(cancellationToken);
            var json = JsonSerializer.Serialize(result, jsonOptions);

            await File.WriteAllTextAsync(indexPath, json, cancellationToken);

            return result;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    public async Task<string> GetProfileContentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            var safeName = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(safeName))
            {
                throw new ArgumentException("Invalid file name.", nameof(fileName));
            }

            var cachedPath = Path.Combine(cacheDir, safeName);

            if (File.Exists(cachedPath) && !IsExpired(cachedPath))
            {
                return await File.ReadAllTextAsync(cachedPath, cancellationToken);
            }

            var content = await inner.GetProfileContentAsync(fileName, cancellationToken);
            await File.WriteAllTextAsync(cachedPath, content, cancellationToken);

            return content;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    public void InvalidateCache()
    {
        semaphoreSlim.Wait();

        try
        {
            if (!Directory.Exists(cacheDir))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(cacheDir))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Best-effort cleanup — file may be locked by antivirus or another process
                }
            }
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    public void Dispose()
        => semaphoreSlim.Dispose();

    private bool IsExpired(string filePath)
    {
        var lastWrite = File.GetLastWriteTimeUtc(filePath);
        return DateTime.UtcNow - lastWrite > ttl;
    }
}