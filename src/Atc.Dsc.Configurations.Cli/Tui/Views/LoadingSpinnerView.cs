namespace Atc.Dsc.Configurations.Cli.Tui.Views;

/// <summary>
/// Displays a braille dot spinner animation with a text label.
/// Uses a timer to cycle through frames at 80ms intervals.
/// </summary>
internal sealed class LoadingSpinnerView : View
{
    private static readonly string[] Frames =
        ["\u280b", "\u2819", "\u2839", "\u2838", "\u283c", "\u2834", "\u2826", "\u2827", "\u2807", "\u280f"];

    private readonly IApplication app;
    private Timer? timer;
    private int frameIndex;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadingSpinnerView"/> class.
    /// </summary>
    /// <param name="app">the Terminal.Gui application instance for UI thread dispatch.</param>
    public LoadingSpinnerView(IApplication app)
    {
        this.app = app;
        CanFocus = false;
        Visible = false;
    }

    /// <summary>
    /// Gets or sets the label text shown next to the spinner.
    /// </summary>
    internal string Label { get; set; } = string.Empty;

    /// <summary>
    /// Starts the spinner animation.
    /// </summary>
    internal void Start()
    {
        frameIndex = 0;
        Visible = true;
        timer?.Dispose();
        timer = new Timer(OnTick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(80));
    }

    /// <summary>
    /// Stops the spinner animation and hides the view.
    /// </summary>
    internal void Stop()
    {
        timer?.Dispose();
        timer = null;
        Visible = false;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            disposed = true;
            timer?.Dispose();
            timer = null;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override bool OnDrawingContent(DrawContext? context)
    {
        var vp = Viewport;
        Move(0, 0);
        SetAttribute(DarkTheme.Cyan);
        AddStr(Frames[frameIndex % Frames.Length]);

        SetAttribute(DarkTheme.Default);
        var text = " " + Label;
        if (text.Length > vp.Width - 1)
        {
            text = text[..(vp.Width - 1)];
        }

        AddStr(text);

        var used = 1 + text.Length;
        if (used < vp.Width)
        {
            SetAttribute(DarkTheme.Default);
            AddStr(new string(' ', vp.Width - used));
        }

        return true;
    }

    private void OnTick(object? state)
    {
        if (disposed)
        {
            return;
        }

        frameIndex = (frameIndex + 1) % Frames.Length;
        app.Invoke(() => SetNeedsDraw());
    }
}