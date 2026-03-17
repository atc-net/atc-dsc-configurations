namespace Atc.Dsc.Configurations.Cli.Tui.Views;

/// <summary>
/// Displays the execution queue with per-profile status icons and a
/// progress bar. Running items show an animated braille spinner.
/// </summary>
internal sealed class ProfileQueueView : View
{
    private static readonly string[] SpinnerFrames =
        ["\u280b", "\u2819", "\u2839", "\u2838", "\u283c", "\u2834", "\u2826", "\u2827", "\u2807", "\u280f"];

    private readonly IApplication app;
    private string[] names = [];
    private QueueItemStatus[] statuses = [];
    private int completed;
    private int total;
    private int spinnerFrame;
    private Timer? spinnerTimer;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileQueueView"/> class.
    /// </summary>
    /// <param name="app">the application instance for UI thread dispatch.</param>
    public ProfileQueueView(IApplication app)
    {
        this.app = app;
        CanFocus = false;
    }

    /// <summary>
    /// Sets the profile list and starts the spinner timer.
    /// </summary>
    /// <param name="profiles">the profiles to display.</param>
    internal void SetProfiles(IReadOnlyList<ProfileSummary> profiles)
    {
        names = new string[profiles.Count];
        statuses = new QueueItemStatus[profiles.Count];
        total = profiles.Count;

        for (var i = 0; i < profiles.Count; i++)
        {
            names[i] = TruncateName(profiles[i].Name);
            statuses[i] = QueueItemStatus.Queued;
        }

        spinnerTimer?.Dispose();
        spinnerTimer = new Timer(
            OnSpinnerTick,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(80));
    }

    /// <summary>
    /// Updates a profile's queue status.
    /// </summary>
    /// <param name="index">the zero-based profile index.</param>
    /// <param name="status">the new status.</param>
    internal void UpdateStatus(
        int index,
        QueueItemStatus status)
    {
        if (index >= 0 && index < statuses.Length)
        {
            statuses[index] = status;
            if (status is QueueItemStatus.Passed or QueueItemStatus.Failed)
            {
                completed++;
            }

            app.Invoke(() => SetNeedsDraw());
        }
    }

    /// <summary>
    /// Stops the spinner timer. Call when execution completes.
    /// </summary>
    internal void StopSpinner()
    {
        spinnerTimer?.Dispose();
        spinnerTimer = null;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            disposed = true;
            spinnerTimer?.Dispose();
            spinnerTimer = null;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override bool OnDrawingContent(DrawContext? context)
    {
        var vp = Viewport;

        for (var row = 0; row < vp.Height; row++)
        {
            Move(0, row);

            if (row < names.Length)
            {
                DrawQueueItem(row, vp.Width);
            }
            else if (row == names.Length + 1)
            {
                DrawProgressBar(vp.Width);
            }
            else
            {
                SetAttribute(DarkTheme.Default);
                AddStr(new string(' ', vp.Width));
            }
        }

        return true;
    }

    private void DrawQueueItem(
        int index,
        int width)
    {
        var (symbol, attr) = GetSymbolAndAttr(statuses[index]);

        SetAttribute(attr);
        AddStr($" {symbol} ");

        var nameAttr = statuses[index] == QueueItemStatus.Running
            ? DarkTheme.Header
            : DarkTheme.Default;
        SetAttribute(nameAttr);

        var name = names[index];
        AddStr(name);

        var used = 3 + name.Length;
        if (used < width)
        {
            SetAttribute(DarkTheme.Default);
            AddStr(new string(' ', width - used));
        }
    }

    private void DrawProgressBar(int width)
    {
        if (total == 0)
        {
            SetAttribute(DarkTheme.Default);
            AddStr(new string(' ', width));
            return;
        }

        var label = $" {completed}/{total} ";
        var barWidth = width - label.Length - 1;
        if (barWidth < 1)
        {
            barWidth = 1;
        }

        var filled = total > 0 ? (int)(barWidth * ((double)completed / total)) : 0;
        var empty = barWidth - filled;

        SetAttribute(DarkTheme.Green);
        AddStr(" ");
        AddStr(new string('\u2588', filled));

        SetAttribute(DarkTheme.Dim);
        AddStr(new string('\u2591', empty));

        SetAttribute(DarkTheme.Default);
        AddStr(label);
    }

    private (string Symbol, Terminal.Gui.Drawing.Attribute Attr) GetSymbolAndAttr(
        QueueItemStatus status) => status switch
    {
        QueueItemStatus.Queued => ("\u25cb", DarkTheme.Dim),
        QueueItemStatus.Running => (
            SpinnerFrames[spinnerFrame % SpinnerFrames.Length],
            DarkTheme.Cyan),
        QueueItemStatus.Passed => ("\u2713", DarkTheme.Green),
        QueueItemStatus.Failed => ("\u2717", DarkTheme.Red),
        _ => ("?", DarkTheme.Default),
    };

    private static string TruncateName(string name)
    {
        var label = name.Replace(" configuration", string.Empty, StringComparison.OrdinalIgnoreCase);
        return label.Length > 20 ? label[..19] + "." : label;
    }

    private void OnSpinnerTick(object? state)
    {
        if (disposed)
        {
            return;
        }

        spinnerFrame = (spinnerFrame + 1) % SpinnerFrames.Length;
        app.Invoke(() => SetNeedsDraw());
    }
}