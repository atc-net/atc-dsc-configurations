namespace Atc.Dsc.Configurations.Cli.Commands;

/// <summary>
/// <c>atc-dsc update</c> — clears the local profile cache and forces a fresh
/// fetch from the GitHub repository.
/// </summary>
public sealed class UpdateCommand(IProfileRepository repository)
    : AsyncCommand
{
    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(
        Spectre.Console.Cli.CommandContext context,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();

        repository.InvalidateCache();
        AnsiConsole.MarkupLine("[dim]Cache cleared.[/]");

        var profiles = await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                "Fetching profiles from GitHub...",
                _ => repository.ListProfilesAsync(cancellationToken));

        AnsiConsole.MarkupLine($"[green]\u2713[/] Refreshed [bold]{profiles.Count}[/] profiles from GitHub.");
        return ConsoleExitStatusCodes.Success;
    }
}