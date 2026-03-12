namespace Atc.Dsc.Configurations.Cli.Tui.Views;

/// <summary>
/// Renders a horizontal bar of colored action hint segments.
/// Each segment shows a key and label with distinct color attributes,
/// separated by spaces in the status bar background color.
/// </summary>
internal sealed class ActionHintsView : View
{
    private static Terminal.Gui.Drawing.Attribute GapAttr
        => DarkTheme.StatusBar;

    private IReadOnlyList<ActionHint> hints = [];

    /// <summary>
    /// Sets the action hints to display and triggers a redraw.
    /// </summary>
    /// <param name="value">The list of action hints to render.</param>
    internal void SetHints(IReadOnlyList<ActionHint> value)
    {
        hints = value;
        SetNeedsDraw();
    }

    /// <inheritdoc />
    protected override bool OnDrawingContent(DrawContext? context)
    {
        var vp = Viewport;
        var col = 0;

        for (var i = 0; i < hints.Count; i++)
        {
            var hint = hints[i];
            var keyText = hint.Key;
            var labelText = " " + hint.Label;

            // Draw key portion
            col = DrawSegment(keyText, hint.KeyAttr, col, vp.Width);

            // Draw label portion
            col = DrawSegment(labelText, hint.LabelAttr, col, vp.Width);

            // Draw gap between hints
            if (i < hints.Count - 1)
            {
                col = DrawSegment(" ", GapAttr, col, vp.Width);
            }
        }

        // Fill remaining width with status bar background
        if (col < vp.Width)
        {
            Move(col, 0);
            SetAttribute(GapAttr);
            AddStr(new string(' ', vp.Width - col));
        }

        return true;
    }

    private int DrawSegment(
        string text,
        Terminal.Gui.Drawing.Attribute attr,
        int col,
        int maxWidth)
    {
        if (col >= maxWidth)
        {
            return col;
        }

        Move(col, 0);
        SetAttribute(attr);

        var available = maxWidth - col;
        if (text.Length > available)
        {
            AddStr(text[..available]);
            return maxWidth;
        }

        AddStr(text);
        return col + text.Length;
    }
}