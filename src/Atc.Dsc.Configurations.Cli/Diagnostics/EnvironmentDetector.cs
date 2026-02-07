namespace Atc.Dsc.Configurations.Cli.Diagnostics;

/// <summary>
/// Default implementation that checks admin elevation via SID
/// and DSC CLI via <c>dsc --version</c>.
/// </summary>
public sealed class EnvironmentDetector : IEnvironmentDetector
{
    public async Task<EnvironmentInfo> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        var isAdmin = OperatingSystem.IsWindows() && CheckIsAdmin();

        var (dscAvailable, dscVersion) = await ProbeVersionAsync("dsc", cancellationToken);

        return new EnvironmentInfo(
            isAdmin,
            dscAvailable,
            dscVersion);
    }

    [SupportedOSPlatform("windows")]
    internal static bool CheckIsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.Owner?.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid) ?? false;
        }
        catch (SecurityException)
        {
            return false;
        }
    }

    internal static async Task<(bool Available, string? Version)> ProbeVersionAsync(
        string executable,
        CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var result = await CliWrap.Cli
                .Wrap(executable)
                .WithArguments("--version")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cts.Token);

            if (result.ExitCode != 0)
            {
                return (false, null);
            }

            var version = ExtractVersion(result.StandardOutput);
            return (true, version);
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or OperationCanceledException)
        {
            return (false, null);
        }
    }

    /// <summary>
    /// Extracts the version string from CLI output, trimming whitespace.
    /// </summary>
    /// <param name="output">the raw standard output from the CLI process.</param>
    /// <returns>The trimmed version string, or <see langword="null"/> if the output is empty.</returns>
    internal static string? ExtractVersion(string output)
    {
        var trimmed = output.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        // `dsc --version` returns "dsc 3.2.0-preview.11" — strip the command prefix
        var spaceIdx = trimmed.IndexOf(' ', StringComparison.Ordinal);
        return spaceIdx >= 0
            ? trimmed[(spaceIdx + 1)..].TrimStart()
            : trimmed;
    }
}