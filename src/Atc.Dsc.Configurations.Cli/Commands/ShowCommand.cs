namespace Atc.Dsc.Configurations.Cli.Commands;

/// <summary>
/// <c>atc-dsc show &lt;profile&gt;</c> — displays a tree view of a single profile's
/// resources, dependencies, and properties, or the raw YAML with <c>--raw</c>.
/// </summary>
public sealed class ShowCommand(
    IProfileRepository repository,
    IProfileParser parser)
    : AsyncCommand<ShowCommandSettings>
{
    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(
        Spectre.Console.Cli.CommandContext context,
        ShowCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ConsoleHelper.WriteHeader();

        var fileName = ProfileFileNameExtensions.ResolveFileName(settings.Profile);
        string content;

        try
        {
            content = await repository.GetProfileContentAsync(fileName, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or FileNotFoundException)
        {
            AnsiConsole.MarkupLine($"[red]Profile not found:[/] {settings.Profile.EscapeMarkup()}");
            return ConsoleExitStatusCodes.Failure;
        }

        if (settings.Raw)
        {
            AnsiConsole.WriteLine(content);
            return ConsoleExitStatusCodes.Success;
        }

        var profile = parser.Parse(content, fileName);

        var tree = new Tree($"[bold]{profile.Name.EscapeMarkup()}[/]");

        if (profile.Description is not null)
        {
            tree.AddNode($"[dim]{profile.Description.EscapeMarkup()}[/]");
        }

        var resourcesNode = tree.AddNode($"[bold]Resources[/] ({profile.Resources.Count})");

        foreach (var resource in profile.Resources)
        {
            var resNode = resourcesNode.AddNode($"[green]{resource.Name.EscapeMarkup()}[/]");
            resNode.AddNode($"[dim]Type:[/] {resource.Type.EscapeMarkup()}");

            if (resource.DependsOn.Count <= 0)
            {
                continue;
            }

            var dependsOnNode = resNode.AddNode("[dim]Depends on:[/]");
            foreach (var dependsOn in resource.DependsOn)
            {
                dependsOnNode.AddNode($"[dim]{dependsOn.EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.Write(tree);

        return ConsoleExitStatusCodes.Success;
    }
}