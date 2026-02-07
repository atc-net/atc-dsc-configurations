namespace Atc.Dsc.Configurations.Cli.Tui;

/// <summary>
/// Modal dialog that runs DSC test/apply operations using the structured
/// <see cref="IDscClient"/> and renders colored, formatted per-resource
/// results with status symbols, durations, and a final summary.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
public sealed class ExecutionDialog : Dialog
{
    private readonly IApplication app;
    private readonly IProfileRepository repository;
    private readonly IDscClient dscClient;
    private readonly IReadOnlyList<ProfileSummary> profiles;
    private readonly ExecutionMode mode;

    private readonly ColoredOutputView outputView;
    private readonly Label progressLabel;
    private readonly Button closeButton;
    private CancellationTokenSource? cts;

    // Color palette
    private static readonly Terminal.Gui.Drawing.Attribute DefaultAttr = new(ColorName16.Gray, ColorName16.Black);
    private static readonly Terminal.Gui.Drawing.Attribute HeaderAttr = new(ColorName16.White, ColorName16.Black);
    private static readonly Terminal.Gui.Drawing.Attribute GreenAttr = new(ColorName16.BrightGreen, ColorName16.Black);
    private static readonly Terminal.Gui.Drawing.Attribute RedAttr = new(ColorName16.BrightRed, ColorName16.Black);
    private static readonly Terminal.Gui.Drawing.Attribute YellowAttr = new(ColorName16.BrightYellow, ColorName16.Black);
    private static readonly Terminal.Gui.Drawing.Attribute CyanAttr = new(ColorName16.BrightCyan, ColorName16.Black);
    private static readonly Terminal.Gui.Drawing.Attribute DimAttr = new(ColorName16.DarkGray, ColorName16.Black);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionDialog"/> class.
    /// </summary>
    /// <param name="app">the Terminal.Gui application instance.</param>
    /// <param name="repository">the profile repository for downloading profiles.</param>
    /// <param name="dscClient">the DSC client.</param>
    /// <param name="profiles">the profiles to execute.</param>
    /// <param name="mode">the execution mode (test or apply).</param>
    public ExecutionDialog(
        IApplication app,
        IProfileRepository repository,
        IDscClient dscClient,
        IReadOnlyList<ProfileSummary> profiles,
        ExecutionMode mode)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(dscClient);
        ArgumentNullException.ThrowIfNull(profiles);

        this.app = app;
        this.repository = repository;
        this.dscClient = dscClient;
        this.profiles = profiles;
        this.mode = mode;

        var modeTitle = mode == ExecutionMode.Test ? "Testing" : "Applying";
        Title = $"{modeTitle} {profiles.Count} profile(s)";
        Width = Dim.Percent(90);
        Height = Dim.Percent(85);

        progressLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Text = "Starting...",
        };

        outputView = new ColoredOutputView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            CanFocus = true,
        };

        closeButton = new Button
        {
            Text = "Cancel",
            IsDefault = true,
        };
        closeButton.Accepting += (_, e) =>
        {
            cts?.Cancel();
            this.app.RequestStop();
            e.Handled = true;
        };

        Add(progressLabel, outputView);
        AddButton(closeButton);

        Initialized += (_, _) => _ = RunExecutionAsync();
    }

    private async Task RunExecutionAsync()
    {
        using var tokenSource = new CancellationTokenSource();
        cts = tokenSource;

        var sw = Stopwatch.StartNew();

        try
        {
            var (passed, failed) = await ExecuteProfilesAsync(
                dscClient,
                tokenSource.Token);

            sw.Stop();
            RenderFinalSummary(passed, failed, sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            UpdateProgress("Cancelled.");
            AppendLine(string.Empty, DefaultAttr);
            AppendLine("  Execution cancelled by user.", YellowAttr);
            FlushOutput();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            UpdateProgress($"Error: {ex.Message}");
            AppendLine(string.Empty, DefaultAttr);
            AppendLine($"  Fatal error: {ex.Message}", RedAttr);
            FlushOutput();
        }
        finally
        {
            cts = null;
        }

        app.Invoke(() =>
        {
            closeButton.Text = "Close";
            closeButton.SetNeedsLayout();
        });
    }

    private async Task<(int Passed, int Failed)> ExecuteProfilesAsync(
        IDscClient client,
        CancellationToken cancellationToken)
    {
        var passed = 0;
        var failed = 0;

        for (var i = 0; i < profiles.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var profile = profiles[i];
            RenderProfileHeader(profile, i);

            try
            {
                var success = await ExecuteSingleProfileAsync(client, profile, cancellationToken);
                if (success)
                {
                    passed++;
                }
                else
                {
                    failed++;
                }
            }
            catch (OperationCanceledException)
            {
                AppendLine("  CANCELLED", YellowAttr);
                break;
            }

            FlushOutput();
        }

        return (passed, failed);
    }

    private void RenderProfileHeader(
        ProfileSummary profile,
        int index)
    {
        var modeLabel = mode == ExecutionMode.Test ? "TEST" : "APPLY";

        UpdateProgress($"[{index + 1}/{profiles.Count}] {profile.Name}...");

        var profileHeader = $"-- {profile.Name} -- {modeLabel} ";
        profileHeader += new string('-', System.Math.Max(0, 60 - profileHeader.Length));

        AppendLine(string.Empty, DefaultAttr);
        AppendLine(profileHeader, HeaderAttr);

        FlushOutput();
    }

    private async Task<bool> ExecuteSingleProfileAsync(
        IDscClient client,
        ProfileSummary profile,
        CancellationToken cancellationToken)
    {
        try
        {
            var tempPath = await repository.DownloadToTempAsync(
                profile.FileName,
                cancellationToken);

            var result = mode == ExecutionMode.Test
                ? await client.TestAsync(tempPath, cancellationToken)
                : await client.ApplyAsync(tempPath, cancellationToken);

            RenderResults(result);

            return result.Success;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLine($"  x FAIL: {ex.Message}", RedAttr);
            return false;
        }
    }

    private void RenderFinalSummary(
        int passed,
        int failed,
        TimeSpan elapsed)
    {
        var summaryAttr = failed == 0 ? GreenAttr : RedAttr;
        AppendLine(string.Empty, DefaultAttr);
        AppendLine(
            $"  Done: {passed} passed, {failed} failed ({elapsed.TotalSeconds:F1}s)",
            summaryAttr);
        FlushOutput();

        UpdateProgress($"Done: {passed} passed, {failed} failed ({elapsed.TotalSeconds:F1}s)");
    }

    private void RenderResults(ExecutionResult result)
    {
        foreach (var res in result.Results)
        {
            var (symbol, attr) = GetSymbolAndColor(res.State);
            var durationStr = res.Duration.HasValue
                ? $"{res.Duration.Value.TotalSeconds:F1}s"
                : string.Empty;

            var statusText = res.StatusText
                ?? res.State
                    .ToString()
                    .ToLowerInvariant();
            var nameCol = res.Name.Length > 35 ? res.Name[..32] + "..." : res.Name;
            var statusCol = $"({statusText})";

            AppendLine(
                $"  {symbol} {nameCol,-35} {statusCol,-24} {durationStr,6}",
                attr);

            if (res.ErrorMessage is not null)
            {
                AppendLine($"      error: {res.ErrorMessage}", RedAttr);
            }
        }

        // Profile summary line
        var resultLabel = result.Success ? "PASS" : "FAIL";
        var resultAttr = result.Success ? GreenAttr : RedAttr;
        AppendLine($"  Duration: {result.Duration.TotalSeconds:F1}s | Result: {resultLabel}", resultAttr);
    }

    private static (string Symbol, Terminal.Gui.Drawing.Attribute Attr) GetSymbolAndColor(
        ResourceState state)
        => state switch
        {
            ResourceState.Compliant => ("+", GreenAttr),
            ResourceState.NonCompliant => ("!", YellowAttr),
            ResourceState.Changed => ("~", CyanAttr),
            ResourceState.Executed => (">", CyanAttr),
            ResourceState.Failed => ("x", RedAttr),
            ResourceState.Skipped => ("-", DimAttr),
            _ => ("?", DefaultAttr),
        };

    private void UpdateProgress(string text)
    {
        app.Invoke(() =>
        {
            progressLabel.Text = text;
        });
    }

    private void AppendLine(
        string text,
        Terminal.Gui.Drawing.Attribute attr)
    {
        outputView.AppendLine(text, attr);
    }

    private void FlushOutput()
    {
        app.Invoke(() =>
        {
            outputView.ScrollToEnd();
            outputView.SetNeedsDraw();
        });
    }
}