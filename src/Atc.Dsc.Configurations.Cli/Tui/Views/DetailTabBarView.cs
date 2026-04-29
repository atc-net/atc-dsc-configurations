namespace Atc.Dsc.Configurations.Cli.Tui.Views;

/// <summary>
/// Renders a single-row detail tab strip (Overview/Resources/Extensions/Raw YAML).
/// Active tab is bright; inactive tabs are dim. Click a tab header to switch.
/// </summary>
internal sealed class DetailTabBarView : View
{
    private readonly string[] labels;
    private readonly int[] columnStarts;
    private int activeIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailTabBarView"/> class.
    /// </summary>
    /// <param name="labels">the tab label strings.</param>
    public DetailTabBarView(string[] labels)
    {
        this.labels = labels;
        columnStarts = new int[labels.Length];
        CanFocus = false;
    }

    /// <summary>
    /// Gets or sets the callback invoked when the user clicks a tab. Argument
    /// is the zero-based tab index.
    /// </summary>
    internal Action<int>? TabClicked { get; set; }

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
            columnStarts[i] = col;
            col += DrawTab(i, isActive: i == activeIndex);
        }

        const string hint = "← prev  → next ";
        var gap = vp.Width - col - hint.Length;
        if (gap > 0)
        {
            SetAttribute(DarkTheme.Default);
            AddStr(new string(' ', gap));
            SetAttribute(DarkTheme.Dim);
            AddStr(hint);
        }
        else if (col < vp.Width)
        {
            SetAttribute(DarkTheme.Default);
            AddStr(new string(' ', vp.Width - col));
        }

        return true;
    }

    /// <inheritdoc />
    protected override bool OnMouseEvent(Mouse mouse)
    {
        if (!mouse.Flags.HasFlag(MouseFlags.LeftButtonClicked))
        {
            return false;
        }

        var x = mouse.Position?.X ?? -1;
        if (x < 0)
        {
            return false;
        }

        for (var i = labels.Length - 1; i >= 0; i--)
        {
            if (x >= columnStarts[i])
            {
                TabClicked?.Invoke(i);
                return true;
            }
        }

        return false;
    }

    private int DrawTab(
        int index,
        bool isActive)
    {
        const string prefix = " ";
        var label = labels[index];
        const string suffix = " ";
        var marker = isActive ? "│" : " ";

        var labelAttr = isActive ? DarkTheme.Header : DarkTheme.Dim;

        SetAttribute(DarkTheme.Default);
        AddStr(prefix);

        SetAttribute(labelAttr);
        AddStr(label);

        SetAttribute(DarkTheme.Default);
        AddStr(suffix);

        SetAttribute(DarkTheme.Dim);
        AddStr(marker);

        return prefix.Length + label.Length + suffix.Length + marker.Length;
    }
}