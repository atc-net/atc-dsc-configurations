namespace Atc.Dsc.Configurations.Cli.Tui.Views;

/// <summary>
/// Renders a horizontal top tab bar with colored dot indicators
/// on a black background. Active tab is bright, inactive tabs are dim.
/// </summary>
internal sealed class TopTabBarView : View
{
    private readonly string[] labels;
    private int activeIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="TopTabBarView"/> class.
    /// </summary>
    /// <param name="labels">the tab label strings.</param>
    public TopTabBarView(string[] labels)
    {
        this.labels = labels;
        CanFocus = false;
    }

    /// <summary>
    /// Sets the active tab index and redraws.
    /// </summary>
    /// <param name="index">the zero-based tab index.</param>
    internal void SetActive(int index)
    {
        activeIndex = index;
        SetNeedsDraw();
    }

    /// <inheritdoc />
    protected override bool OnDrawingContent(DrawContext? context)
    {
        var vp = Viewport;
        Move(0, 0);

        var col = 0;

        for (var i = 0; i < labels.Length; i++)
        {
            var isActive = i == activeIndex;
            col += DrawTab(i, isActive, col, vp.Width);
        }

        // Fill remaining width
        if (col < vp.Width)
        {
            SetAttribute(DarkTheme.Default);
            AddStr(new string(' ', vp.Width - col));
        }

        return true;
    }

    private int DrawTab(
        int index,
        bool isActive,
        int col,
        int maxWidth)
    {
        // " ● Label (N)  " pattern
        var dot = isActive ? "\u25cf" : "\u25cb";
        var prefix = $" {dot} ";
        var hint = $" ({index + 1})";
        var suffix = "  ";
        var text = prefix + labels[index] + hint + suffix;

        if (col + text.Length > maxWidth)
        {
            text = text[..(maxWidth - col)];
        }

        var dotAttr = isActive ? DarkTheme.Green : DarkTheme.Dim;
        var labelAttr = isActive ? DarkTheme.Header : DarkTheme.Dim;

        // Draw dot
        SetAttribute(dotAttr);
        AddStr(prefix);

        // Draw label
        SetAttribute(labelAttr);
        AddStr(labels[index]);

        // Draw key hint
        SetAttribute(DarkTheme.Dim);
        AddStr($" ({index + 1})");

        // Draw suffix spacing
        SetAttribute(DarkTheme.Default);
        AddStr(suffix);

        return text.Length;
    }
}