namespace Atc.Dsc.Configurations.Cli.Tui;

/// <summary>
/// A scrollable view that renders lines of text with per-line color attributes.
/// Uses direct <c>SetAttribute</c>/<c>AddStr</c> drawing to guarantee colors
/// are respected (unlike <c>TextView</c> which uses scheme-based rendering).
/// </summary>
public sealed class ColoredOutputView : View
{
    private static Terminal.Gui.Drawing.Attribute BgAttr => DarkTheme.Default;

    private readonly List<(string Text, Terminal.Gui.Drawing.Attribute Attr)> lines = [];
    private int topRow;

    /// <summary>
    /// Appends a line of text with the specified color attribute.
    /// </summary>
    /// <param name="text">the text content to append.</param>
    /// <param name="attr">the color attribute for the line.</param>
    public void AppendLine(
        string text,
        Terminal.Gui.Drawing.Attribute attr)
        => lines.Add((text, attr));

    /// <summary>
    /// Returns a snapshot of all lines and their color attributes.
    /// </summary>
    /// <returns>A read-only list of text/attribute pairs.</returns>
    public IReadOnlyList<(string Text, Terminal.Gui.Drawing.Attribute Attr)> GetLines()
        => lines.ToList();

    /// <summary>
    /// Removes all lines and resets the view.
    /// </summary>
    public void Clear()
    {
        lines.Clear();
        topRow = 0;
    }

    /// <summary>
    /// Adjusts the visible top row by <paramref name="delta"/> lines (positive
    /// scrolls down, negative scrolls up). Updates only this view's draw state —
    /// does not mutate <c>Viewport</c>, so parent layout/adornment redraws are
    /// not triggered.
    /// </summary>
    /// <param name="delta">the row delta to scroll by.</param>
    public void Scroll(int delta)
    {
        var maxTop = System.Math.Max(0, lines.Count - System.Math.Max(1, Viewport.Height));
        var newTop = System.Math.Clamp(topRow + delta, 0, maxTop);
        if (newTop == topRow)
        {
            return;
        }

        topRow = newTop;
        SetNeedsDraw();
    }

    /// <summary>
    /// Scrolls to the top of the buffer.
    /// </summary>
    public void ScrollToTop()
    {
        if (topRow == 0)
        {
            return;
        }

        topRow = 0;
        SetNeedsDraw();
    }

    /// <summary>
    /// Scrolls to the end of the buffer (last viewport-full).
    /// </summary>
    public void ScrollToEnd()
    {
        var maxTop = System.Math.Max(0, lines.Count - System.Math.Max(1, Viewport.Height));
        if (topRow == maxTop)
        {
            return;
        }

        topRow = maxTop;
        SetNeedsDraw();
    }

    /// <inheritdoc />
    protected override bool OnDrawingContent(DrawContext? context)
    {
        var vp = Viewport;

        for (var row = 0; row < vp.Height; row++)
        {
            var lineIndex = topRow + row;
            Move(0, row);

            if (lineIndex < lines.Count)
            {
                var (text, attr) = lines[lineIndex];
                SetAttribute(attr);

                if (text.Length >= vp.Width)
                {
                    AddStr(text[..vp.Width]);
                }
                else
                {
                    AddStr(text);

                    // Fill remaining width with background
                    SetAttribute(BgAttr);
                    AddStr(new string(' ', vp.Width - text.Length));
                }
            }
            else
            {
                // Empty row
                SetAttribute(BgAttr);
                AddStr(new string(' ', vp.Width));
            }
        }

        return true;
    }
}