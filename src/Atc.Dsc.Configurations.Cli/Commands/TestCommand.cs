namespace Atc.Dsc.Configurations.Cli.Commands;

/// <summary>
/// <c>atc-dsc test &lt;profiles&gt;</c> — runs <c>dsc config test</c>
/// against one or more profiles and reports per-resource compliance state.
/// Read-only — does not modify the system.
/// </summary>
public sealed class TestCommand(
    IProfileRepository repository,
    IDscClient dscClient,
    JsonSerializerOptions jsonOptions)
    : AsyncCommand<TestCommandSettings>
{
    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(
        Spectre.Console.Cli.CommandContext context,
        TestCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ConsoleHelper.WriteHeader();

        var results = new List<ExecutionResult>();
        var hasFailure = false;

        foreach (var profile in settings.Profiles)
        {
            var fileName = ProfileFileNameExtensions.ResolveFileName(profile);

            var tempPath = await AnsiConsole
                .Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync(
                    $"Downloading {profile}...",
                    _ => repository.DownloadToTempAsync(fileName, cancellationToken));

            var result = await AnsiConsole
                .Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync(
                    $"Testing {profile}...",
                    _ => dscClient.TestAsync(tempPath, cancellationToken));

            results.Add(result);

            if (result.Success)
            {
                continue;
            }

            hasFailure = true;
            if (!settings.Continue)
            {
                break;
            }
        }

        if (settings.Json)
        {
            var json = JsonSerializer.Serialize(results, jsonOptions);
            AnsiConsole.WriteLine(json);
        }
        else
        {
            RenderResults(results, settings.Verbose);
        }

        return hasFailure
            ? ConsoleExitStatusCodes.Failure
            : ConsoleExitStatusCodes.Success;
    }

    private static void RenderResults(
        List<ExecutionResult> results,
        bool verbose)
    {
        foreach (var result in results)
        {
            var statusIcon = result.Success ? "[green]PASS[/]" : "[red]FAIL[/]";
            AnsiConsole.MarkupLine($"  {statusIcon} {result.ProfileName.EscapeMarkup()} [dim]({result.Duration.TotalSeconds:F1}s)[/]");

            if (!verbose && result.Success)
            {
                continue;
            }

            foreach (var res in result.Results)
            {
                var icon = res.State switch
                {
                    ResourceState.Compliant => "[green]\u2713[/]",
                    ResourceState.NonCompliant => "[yellow]![/]",
                    ResourceState.Changed => "[cyan]~[/]",
                    ResourceState.Executed => "[cyan]>[/]",
                    ResourceState.Failed => "[red]\u2717[/]",
                    ResourceState.Skipped => "[dim]-[/]",
                    _ => "[dim]?[/]",
                };

                AnsiConsole.MarkupLine($"    {icon} {res.Name.EscapeMarkup()} [dim]{res.Type.EscapeMarkup()}[/]");

                if (res.ErrorMessage is not null)
                {
                    AnsiConsole.MarkupLine($"      [red]{res.ErrorMessage.EscapeMarkup()}[/]");
                }
            }
        }

        AnsiConsole.WriteLine();

        var total = results.Count;
        var passed = results.Count(r => r.Success);
        var failed = total - passed;

        AnsiConsole.MarkupLine($"  [bold]Results:[/] {passed} passed, {failed} failed, {total} total");
    }
}