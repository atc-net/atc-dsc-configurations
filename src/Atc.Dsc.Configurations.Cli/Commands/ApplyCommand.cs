namespace Atc.Dsc.Configurations.Cli.Commands;

/// <summary>
/// <c>atc-dsc apply &lt;profiles&gt;</c> — downloads and applies one or more DSC profiles
/// to configure the system. Supports <c>--all</c>, <c>--file</c>,
/// and <c>--yes</c> for non-interactive confirmation.
/// </summary>
public sealed class ApplyCommand(
    IProfileRepository repository,
    IDscClient dscClient,
    EnvironmentInfo envInfo,
    JsonSerializerOptions jsonOptions)
    : AsyncCommand<ApplyCommandSettings>
{
    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(
        Spectre.Console.Cli.CommandContext context,
        ApplyCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ConsoleHelper.WriteHeader();

        if (!envInfo.IsAdmin)
        {
            RenderAdminWarning();
            return ConsoleExitStatusCodes.Failure;
        }

        List<(string Name, string Path)> filesToApply;

        try
        {
            filesToApply = await ResolveFilesAsync(settings, cancellationToken);
        }
        catch (Exception ex) when (ex is FileNotFoundException or HttpRequestException)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            return ConsoleExitStatusCodes.Failure;
        }

        if (filesToApply.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No profiles to apply.[/]");
            return ConsoleExitStatusCodes.Success;
        }

        if (!settings.Yes &&
            !await ConfirmApplyAsync(filesToApply))
        {
            return ConsoleExitStatusCodes.Success;
        }

        var (results, hasFailure) = await ApplyProfilesAsync(
            dscClient,
            filesToApply,
            settings.Continue,
            cancellationToken);

        RenderSummary(results, settings.Json);

        return hasFailure ? ConsoleExitStatusCodes.Failure : ConsoleExitStatusCodes.Success;
    }

    private static void RenderAdminWarning()
    {
        AnsiConsole.MarkupLine("[yellow bold]Apply requires administrator privileges.[/]");
        AnsiConsole.MarkupLine("[dim]Restart your terminal as administrator, or use 'atc-dsc test' instead.[/]");
    }

    private static Task<bool> ConfirmApplyAsync(
        List<(string Name, string Path)> filesToApply)
    {
        AnsiConsole.MarkupLine("[bold]The following profiles will be applied:[/]");

        foreach (var (name, _) in filesToApply)
        {
            AnsiConsole.MarkupLine($"  - {name.EscapeMarkup()}");
        }

        AnsiConsole.WriteLine();

        return AnsiConsole.ConfirmAsync("Proceed?");
    }

    private static async Task<(List<ExecutionResult> Results, bool HasFailure)> ApplyProfilesAsync(
        IDscClient client,
        List<(string Name, string Path)> filesToApply,
        bool continueOnFailure,
        CancellationToken cancellationToken)
    {
        var results = new List<ExecutionResult>();
        var hasFailure = false;

        foreach (var (name, path) in filesToApply)
        {
            var result = await AnsiConsole
                .Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync(
                    $"Applying {name}...",
                    _ => client.ApplyAsync(path, cancellationToken));

            results.Add(result);

            var statusIcon = result.Success ? "[green]PASS[/]" : "[red]FAIL[/]";
            AnsiConsole.MarkupLine($"  {statusIcon} {result.ProfileName.EscapeMarkup()} [dim]({result.Duration.TotalSeconds:F1}s)[/]");

            if (!result.Success)
            {
                hasFailure = true;
                RenderFailedResources(result);

                if (!continueOnFailure)
                {
                    break;
                }
            }
        }

        return (results, hasFailure);
    }

    private static void RenderFailedResources(ExecutionResult result)
    {
        foreach (var res in result.Results.Where(r => r.State == ResourceState.Failed))
        {
            AnsiConsole.MarkupLine($"    [red]\u2717 {res.Name.EscapeMarkup()}[/]");
            if (res.ErrorMessage is not null)
            {
                AnsiConsole.MarkupLine($"      {res.ErrorMessage.EscapeMarkup()}");
            }
        }
    }

    private void RenderSummary(
        List<ExecutionResult> results,
        bool json)
    {
        if (json)
        {
            var jsonText = JsonSerializer.Serialize(results, jsonOptions);
            AnsiConsole.WriteLine(jsonText);
        }

        AnsiConsole.WriteLine();

        var total = results.Count;
        var passed = results.Count(r => r.Success);
        var failed = total - passed;

        AnsiConsole.MarkupLine($"  [bold]Results:[/] {passed} passed, {failed} failed, {total} total");
    }

    private async Task<List<(string Name, string Path)>> ResolveFilesAsync(
        ApplyCommandSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.File is not null)
        {
            var fullPath = Path.GetFullPath(settings.File);

            return File.Exists(fullPath)
                ? [(Path.GetFileNameWithoutExtension(fullPath), fullPath)]
                : throw new FileNotFoundException($"File not found: {settings.File}", fullPath);
        }

        var profileNames = settings.Profiles ?? [];

        if (settings.All)
        {
            var allProfiles = await repository.ListProfilesAsync(cancellationToken);
            profileNames = allProfiles
                .Select(p => p.FileName)
                .ToArray();
        }

        var files = new List<(string, string)>();

        foreach (var profile in profileNames)
        {
            var fileName = ProfileFileNameExtensions.ResolveFileName(profile);
            var tempPath = await repository.DownloadToTempAsync(fileName, cancellationToken);
            files.Add((profile, tempPath));
        }

        return files;
    }
}