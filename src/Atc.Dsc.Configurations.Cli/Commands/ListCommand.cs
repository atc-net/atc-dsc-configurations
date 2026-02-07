namespace Atc.Dsc.Configurations.Cli.Commands;

/// <summary>
/// <c>atc-dsc list</c> — displays available DSC profiles in a formatted table
/// or as JSON. Non-interactive; suitable for piping and CI/CD scripts.
/// </summary>
public sealed class ListCommand(
    IProfileRepository repository,
    IProfileParser parser,
    JsonSerializerOptions jsonOptions)
    : AsyncCommand<ListCommandSettings>
{
    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(
        Spectre.Console.Cli.CommandContext context,
        ListCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ConsoleHelper.WriteHeader();

        var summaries = await repository.ListProfilesAsync(cancellationToken);

        if (settings.Json)
        {
            var json = JsonSerializer.Serialize(summaries, jsonOptions);
            AnsiConsole.WriteLine(json);
            return ConsoleExitStatusCodes.Success;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Available Profiles[/]");

        table.AddColumn("Profile");
        table.AddColumn("File");

        if (settings.Verbose)
        {
            table.AddColumn("Resources");
            table.AddColumn("Description");
        }

        foreach (var summary in summaries)
        {
            if (settings.Verbose)
            {
                var content = await repository.GetProfileContentAsync(summary.FileName, cancellationToken);
                var profile = parser.Parse(content, summary.FileName);

                table.AddRow(
                    $"[bold]{summary.Name.EscapeMarkup()}[/]",
                    $"[dim]{summary.FileName.EscapeMarkup()}[/]",
                    profile.Resources.Count.ToString(CultureInfo.InvariantCulture),
                    profile.Description?.EscapeMarkup() ?? "[dim]—[/]");
            }
            else
            {
                table.AddRow(
                    $"[bold]{summary.Name.EscapeMarkup()}[/]",
                    $"[dim]{summary.FileName.EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.Write(table);

        return ConsoleExitStatusCodes.Success;
    }
}