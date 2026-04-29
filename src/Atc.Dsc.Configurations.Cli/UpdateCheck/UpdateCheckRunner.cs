namespace Atc.Dsc.Configurations.Cli.UpdateCheck;

/// <summary>
/// Performs an opportunistic NuGet version check on startup and, when a newer
/// stable version is found, attempts an automatic <c>dotnet tool update</c>.
/// Adapted from atc-opc-ua's <c>UpdateCheckRunner</c>.
/// </summary>
internal static class UpdateCheckRunner
{
    private const string PackageId = "atc-dsc";
    private const string CacheDirectoryName = "atc-dsc";
    private const string CacheFileName = "update-check.json";
    private const string EnvironmentSuppressionVar = "ATC_DSC_NO_UPDATE_CHECK";

    private const string Dim = "\e[90m";
    private const string BrightWhite = "\e[97m";
    private const string Cyan = "\e[36m";
    private const string Reset = "\e[0m";

    [SuppressMessage("Major Code Smell", "S1075:URIs should not be hardcoded", Justification = "NuGet API endpoint is stable")]
    private static readonly Uri NuGetIndexUri = new("https://api.nuget.org/v3-flatcontainer/atc-dsc/index.json");

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan UpdateTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(10);

    private static readonly HttpClient SharedHttpClient = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private static bool suppressedForProcess;

    /// <summary>
    /// Suppresses the update check for the lifetime of this process.
    /// Used by the <c>--no-update-check</c> CLI flag preprocessor in <c>Program.cs</c>.
    /// </summary>
    internal static void SuppressForThisProcess()
        => suppressedForProcess = true;

    /// <summary>
    /// Runs the update check. Honours suppression flags and CI detection,
    /// uses a 24h on-disk cache, and only ever logs a non-fatal warning on failure.
    /// </summary>
    /// <param name="currentVersionString">Current assembly informational version (without <c>+meta</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once the check finishes (success, suppression, or any handled failure).</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Update check must never crash the application")]
    internal static async Task RunAsync(
        string currentVersionString,
        CancellationToken cancellationToken)
    {
        if (IsSuppressed())
        {
            return;
        }

        if (!Version.TryParse(currentVersionString, out var currentVersion))
        {
            return;
        }

        try
        {
            var cacheFilePath = GetCacheFilePath();
            var cachedResult = await ReadCacheAsync(cacheFilePath, cancellationToken);

            if (cachedResult is not null &&
                (DateTimeOffset.UtcNow - cachedResult.LastCheck) < CacheTtl)
            {
                HandleCacheHit(cachedResult, currentVersion);
                return;
            }

            var latestVersion = await FetchLatestVersionAsync(cancellationToken);
            if (latestVersion is null ||
                latestVersion <= currentVersion)
            {
                await WriteCacheAsync(
                    cacheFilePath,
                    currentVersion.ToString(3),
                    updatePerformed: false,
                    cancellationToken);

                return;
            }

            await PerformUpdateAsync(
                cacheFilePath,
                currentVersion.ToString(3),
                latestVersion.ToString(3),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested — expected.
        }
        catch (Exception)
        {
            // Update check is best-effort; never propagate.
        }
    }

    private static bool IsSuppressed()
        => suppressedForProcess
        || string.Equals(Environment.GetEnvironmentVariable(EnvironmentSuppressionVar), "1", StringComparison.Ordinal)
        || IsRunningInCi();

    private static bool IsRunningInCi()
        => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
        || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"))
        || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

    private static string GetCacheFilePath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            CacheDirectoryName,
            CacheFileName);

    private static async Task<UpdateCheckCache?> ReadCacheAsync(
        string cacheFilePath,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(cacheFilePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(cacheFilePath, cancellationToken);
            return JsonSerializer.Deserialize<UpdateCheckCache>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    private static void HandleCacheHit(
        UpdateCheckCache cachedResult,
        Version currentVersion)
    {
        if (cachedResult.UpdatePerformed ||
            !Version.TryParse(cachedResult.LatestVersion, out var cachedLatest) ||
            cachedLatest <= currentVersion)
        {
            return;
        }

        PrintUpdateAvailable(
            currentVersion.ToString(3),
            cachedResult.LatestVersion);
    }

    private static async Task<Version?> FetchLatestVersionAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(HttpTimeout);

            var json = await SharedHttpClient.GetStringAsync(NuGetIndexUri, timeoutCts.Token);
            var index = JsonSerializer.Deserialize<NuGetVersionIndex>(json, JsonOptions);

            return index?.Versions
                .Where(v => !v.Contains('-', StringComparison.Ordinal))
                .Select(x => Version.TryParse(x, out var parsed) ? parsed : null)
                .Where(x => x is not null)
                .Max();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    private static async Task PerformUpdateAsync(
        string cacheFilePath,
        string currentVersionString,
        string latestVersionString,
        CancellationToken cancellationToken)
    {
        var updateSucceeded = await TryAutoUpdateAsync(cancellationToken);
        if (updateSucceeded)
        {
            PrintUpdateSuccess(latestVersionString);
        }
        else
        {
            PrintUpdateAvailable(currentVersionString, latestVersionString);
        }

        await WriteCacheAsync(
            cacheFilePath,
            latestVersionString,
            updateSucceeded,
            cancellationToken);
    }

    private static async Task<bool> TryAutoUpdateAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"tool update -g {PackageId}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(UpdateTimeout);

            await process.WaitForExitAsync(timeoutCts.Token);

            return process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or OperationCanceledException)
        {
            return false;
        }
    }

    private static void PrintUpdateSuccess(string latestVersion)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"  {Cyan}ℹ{Reset}  {BrightWhite}Update successful!{Reset} {Dim}v{latestVersion} will be used on your next run.{Reset}");
        System.Console.WriteLine();
    }

    private static void PrintUpdateAvailable(
        string currentVersion,
        string latestVersion)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"  {Cyan}ℹ{Reset}  {Dim}Update available:{Reset} {BrightWhite}{currentVersion}{Reset} {Dim}→{Reset} {BrightWhite}{latestVersion}{Reset}");
        System.Console.WriteLine($"     {Dim}Run:{Reset} {BrightWhite}dotnet tool update -g {PackageId}{Reset}");
        System.Console.WriteLine();
    }

    private static async Task WriteCacheAsync(
        string cacheFilePath,
        string latestVersion,
        bool updatePerformed,
        CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(cacheFilePath);
            if (directory is not null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var cache = new UpdateCheckCache
            {
                LastCheck = DateTimeOffset.UtcNow,
                LatestVersion = latestVersion,
                UpdatePerformed = updatePerformed,
            };

            var json = JsonSerializer.Serialize(cache, JsonOptions);
            await File.WriteAllTextAsync(cacheFilePath, json, cancellationToken);
        }
        catch (IOException)
        {
            // Best-effort — cache write failure is not critical.
        }
    }
}