namespace Atc.Dsc.Configurations.Cli.Clients;

/// <summary>
/// Executes DSC operations via the DSC v3 CLI (<c>dsc config test/set</c>).
/// Preferred executor — produces structured JSON output that is parsed
/// into per-resource results with duration, state, and property details.
/// </summary>
public sealed partial class DscClient : IDscClient
{
    [GeneratedRegex(@"\x1B\[\d{1,3}(?:;\d{1,3})*m", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    internal static partial Regex AnsiEscapeRegex();

    public Task<ExecutionResult> TestAsync(
        string filePath,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(filePath, "test", ExecutionMode.Test, cancellationToken);

    public Task<ExecutionResult> ApplyAsync(
        string filePath,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(filePath, "set", ExecutionMode.Apply, cancellationToken);

    private static async Task<ExecutionResult> ExecuteAsync(
        string filePath,
        string subCommand,
        ExecutionMode executionMode,
        CancellationToken cancellationToken)
    {
        var profileName = ProfileFileNameExtensions.DeriveName(Path.GetFileName(filePath));

        var sw = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(10));

            var result = await CliWrap.Cli
                .Wrap("dsc")
                .WithArguments(["--trace-level", "error", "config", subCommand, "--file", filePath])
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cts.Token);

            sw.Stop();

            return BuildResult(result, profileName, executionMode, sw.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return FailedResult(profileName, executionMode, sw.Elapsed, "DSC operation timed out after 10 minutes");
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or FileNotFoundException)
        {
            sw.Stop();
            return FailedResult(profileName, executionMode, sw.Elapsed, ex.Message);
        }
    }

    private static ExecutionResult BuildResult(
        BufferedCommandResult result,
        string profileName,
        ExecutionMode executionMode,
        TimeSpan elapsed)
    {
        var resourceResults = ParseOutput(result.StandardOutput, executionMode);

        if (result.ExitCode == 0 ||
            resourceResults.Count != 0 ||
            string.IsNullOrWhiteSpace(result.StandardError))
        {
            return new ExecutionResult(
                profileName,
                executionMode,
                result.ExitCode == 0,
                resourceResults,
                elapsed);
        }

        // When DSC fails with no parsed results, surface stderr as the error
        var stderrLines = AnsiEscapeRegex()
            .Replace(result.StandardError, string.Empty)
            .Trim();
        resourceResults.Add(new ResourceResult(
            "dsc",
            "error",
            ResourceState.Failed,
            stderrLines));

        return new ExecutionResult(
            profileName,
            executionMode,
            result.ExitCode == 0,
            resourceResults,
            elapsed);
    }

    private static ExecutionResult FailedResult(
        string profileName,
        ExecutionMode executionMode,
        TimeSpan elapsed,
        string errorMessage)
        => new(
            profileName,
            executionMode,
            Success: false,
            Results: [new ResourceResult("execution", "error", ResourceState.Failed, errorMessage)],
            elapsed);

    internal static List<ResourceResult> ParseOutput(
        string output,
        ExecutionMode executionMode)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        var results = new List<ResourceResult>();

        try
        {
            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;

            if (root.TryGetProperty("results", out var resultsArray) &&
                resultsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in resultsArray.EnumerateArray())
                {
                    results.Add(ParseResourceResult(item, executionMode));
                }
            }
        }
        catch (JsonException)
        {
            results.Add(new ResourceResult(
                "output",
                "parse-error",
                ResourceState.Failed,
                "Failed to parse DSC output as JSON"));
        }

        return results;
    }

    internal static ResourceResult ParseResourceResult(
        JsonElement item,
        ExecutionMode executionMode)
    {
        var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "unknown" : "unknown";
        var type = item.TryGetProperty("type", out var t) ? t.GetString() ?? "unknown" : "unknown";

        var duration = ExtractResourceDuration(item);

        if (!item.TryGetProperty("result", out var result))
        {
            return new ResourceResult(name, type, ResourceState.Failed, "No result in output", duration);
        }

        if (executionMode == ExecutionMode.Test)
        {
            // RunCommandOnSet resources have no test capability — mark as skipped in test mode
            var isSetOnly = type.Contains("RunCommandOnSet", StringComparison.OrdinalIgnoreCase);
            if (isSetOnly)
            {
                return new ResourceResult(
                    name,
                    type,
                    ResourceState.Skipped,
                    ErrorMessage: null,
                    duration,
                    "set only");
            }
        }

        var (state, statusText) = DetermineResourceState(result, type, executionMode);

        return new ResourceResult(
            name,
            type,
            state,
            ErrorMessage: null,
            duration,
            statusText);
    }

    private static TimeSpan? ExtractResourceDuration(JsonElement item)
    {
        if (item.TryGetProperty("metadata", out var itemMetadata) &&
            itemMetadata.TryGetProperty("Microsoft.DSC", out var itemDscMetadata) &&
            itemDscMetadata.TryGetProperty("duration", out var duration) &&
            duration.GetString() is { } durationString)
        {
            return ParseIsoDuration(durationString);
        }

        return null;
    }

    private static (ResourceState State, string StatusText) DetermineResourceState(
        JsonElement result,
        string type,
        ExecutionMode mode)
    {
        if (mode == ExecutionMode.Test)
        {
            var inDesiredState = result.TryGetProperty("inDesiredState", out var ids) && ids.GetBoolean();
            var state = inDesiredState ? ResourceState.Compliant : ResourceState.NonCompliant;
            var statusText = inDesiredState ? "in desired state" : "not in desired state";
            return (state, statusText);
        }

        // Set mode: check changedProperties
        var hasChangedProps = result.TryGetProperty("changedProperties", out var cp) &&
                              cp.ValueKind == JsonValueKind.Array &&
                              cp.GetArrayLength() > 0;

        if (hasChangedProps)
        {
            return (ResourceState.Changed, "changed");
        }

        var isScriptResource = type.Contains("RunCommandOnSet", StringComparison.OrdinalIgnoreCase) ||
                               type.Contains("Script", StringComparison.OrdinalIgnoreCase);

        return isScriptResource
            ? (ResourceState.Executed, "executed")
            : (ResourceState.Compliant, "no changes");
    }

    internal static TimeSpan? ParseIsoDuration(string isoDuration)
    {
        try
        {
            return XmlConvert.ToTimeSpan(isoDuration);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}