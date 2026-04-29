namespace Atc.Dsc.Configurations.Cli;

/// <summary>
/// Renders the CLI startup banner (Braille logo + ATC block + info rows)
/// and runs the opportunistic NuGet update check.
/// </summary>
public static class ConsoleHelper
{
    /// <summary>
    /// Prints the ATC DSC startup banner and runs the NuGet update check.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token forwarded to the update check.</param>
    /// <returns>A task that completes once the banner has been printed and the update check has finished.</returns>
    public static Task WriteHeaderAsync(
        CancellationToken cancellationToken = default)
    {
        System.Console.OutputEncoding = Encoding.UTF8;

        var version = typeof(ConsoleHelper).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0] ?? "dev";

        if (!System.Console.IsOutputRedirected)
        {
            StartupBanner.Print(version);
        }

        return UpdateCheckRunner.RunAsync(version, cancellationToken);
    }
}