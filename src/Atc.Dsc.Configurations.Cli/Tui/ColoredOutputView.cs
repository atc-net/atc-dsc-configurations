namespace Atc.Dsc.Configurations.Cli.Tui;

/// <summary>
/// A scrollable view that renders lines of text with per-line color attributes.
/// Uses direct <c>SetAttribute</c>/<c>AddStr</c> drawing to guarantee colors
/// are respected (unlike <c>TextView</c> which uses scheme-based rendering).
/// </summary>
public sealed class ColoredOutputView : View
{
    private static readonly Terminal.Gui.Drawing.Attribute BgAttr = new(ColorName16.Gray, ColorName16.Black);

    private readonly List<(string Text, Terminal.Gui.Drawing.Attribute Attr)> lines = [];

    /// <summary>
    /// Appends a line of text with the specified color attribute.
    /// </summary>
    /// <param name="text">the text content to append.</param>
    /// <param name="attr">the color attribute for the line.</param>
    public void AppendLine(
        string text,
        Terminal.Gui.Drawing.Attribute attr)
    {
        lines.Add((text, attr));
        UpdateContentSize();
    }

    /// <summary>
    /// Scrolls the view to show the last line of output.
    /// </summary>
    public void ScrollToEnd()
    {
        var viewportHeight = Viewport.Height;
        var overflow = lines.Count - viewportHeight;
        if (overflow > 0)
        {
            ScrollVertical(overflow - Viewport.Y);
        }
    }

    /// <inheritdoc />
    protected override bool OnDrawingContent(DrawContext? context)
    {
        var vp = Viewport;

        for (var row = 0; row < vp.Height; row++)
        {
            var lineIndex = vp.Y + row;
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

    private void UpdateContentSize()
    {
        var maxWidth = 0;
        foreach (var (text, _) in lines)
        {
            if (text.Length > maxWidth)
            {
                maxWidth = text.Length;
            }
        }

        SetContentSize(new System.Drawing.Size(maxWidth, lines.Count));
    }
}