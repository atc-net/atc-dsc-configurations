namespace Atc.Dsc.Configurations.Cli.Commands;

/// <summary>
/// Default command when <c>atc-dsc</c> is invoked with no arguments.
/// Launches the interactive Terminal.Gui TUI for browsing and applying profiles.
/// </summary>
public sealed class InteractiveCommand(
    IInteractiveRunner runner,
    EnvironmentInfo envInfo)
    : AsyncCommand
{
    /// <inheritdoc />
    public override Task<int> ExecuteAsync(
        Spectre.Console.Cli.CommandContext context,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();

        RenderEnvironmentStatus(envInfo);

        return runner.RunAsync(cancellationToken);
    }

    private static void RenderEnvironmentStatus(EnvironmentInfo envInfo)
    {
        var executorPart = envInfo.DscCliAvailable
            ? $"[green]DSC{(envInfo.DscCliVersion is not null
                ? $" v{envInfo.DscCliVersion}"
                : string.Empty)}[/]"
            : "[red]No executor[/]";

        var adminPart = envInfo.IsAdmin
            ? "[green]Admin \u2713[/]"
            : "[yellow]Not admin (test only)[/]";

        AnsiConsole.Write(new Markup($"  {executorPart} \u00b7 {adminPart}"));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }
}