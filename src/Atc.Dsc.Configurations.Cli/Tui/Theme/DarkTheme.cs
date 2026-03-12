namespace Atc.Dsc.Configurations.Cli.Tui.Theme;

/// <summary>
/// Centralized dark theme with consistent black-background color attributes
/// for all TUI views. Inspired by the winget-tui aesthetic.
/// </summary>
internal static class DarkTheme
{
    // Base palette — all on black background
    internal static Terminal.Gui.Drawing.Attribute Default { get; } = new(ColorName16.Gray, ColorName16.Black);

    internal static Terminal.Gui.Drawing.Attribute Header { get; } = new(ColorName16.White, ColorName16.Black);

    internal static Terminal.Gui.Drawing.Attribute Green { get; } = new(ColorName16.BrightGreen, ColorName16.Black);

    internal static Terminal.Gui.Drawing.Attribute Red { get; } = new(ColorName16.BrightRed, ColorName16.Black);

    internal static Terminal.Gui.Drawing.Attribute Yellow { get; } = new(ColorName16.BrightYellow, ColorName16.Black);

    internal static Terminal.Gui.Drawing.Attribute Cyan { get; } = new(ColorName16.BrightCyan, ColorName16.Black);

    internal static Terminal.Gui.Drawing.Attribute Blue { get; } = new(ColorName16.BrightBlue, ColorName16.Black);

    internal static Terminal.Gui.Drawing.Attribute Dim { get; } = new(ColorName16.DarkGray, ColorName16.Black);

    // Semantic — status bar
    internal static Terminal.Gui.Drawing.Attribute StatusBar { get; } = new(ColorName16.Gray, ColorName16.DarkGray);

    internal static Terminal.Gui.Drawing.Attribute StatusBarKey { get; } = new(ColorName16.White, ColorName16.DarkGray);

    internal static Terminal.Gui.Drawing.Attribute StatusBarHighlight { get; } = new(ColorName16.BrightYellow, ColorName16.DarkGray);

    // Semantic — list selection: purple bg when focused, green text when unfocused
    private static readonly Terminal.Gui.Drawing.Color PurpleBg = new(120, 80, 200);

    internal static Terminal.Gui.Drawing.Attribute ListItemFocused { get; }
        = new(new Terminal.Gui.Drawing.Color(255, 255, 255), PurpleBg);

    private static readonly Terminal.Gui.Drawing.Color MossGreenBg = new(80, 160, 80);

    internal static Terminal.Gui.Drawing.Attribute ListItemUnfocused { get; }
        = new(new Terminal.Gui.Drawing.Color(0, 0, 0), MossGreenBg);

    // Semantic — frame borders
    internal static Terminal.Gui.Drawing.Attribute FrameBorder { get; } = new(ColorName16.DarkGray, ColorName16.Black);

    // Semantic — tabs
    internal static Terminal.Gui.Drawing.Attribute TabActive { get; } = new(ColorName16.BrightCyan, ColorName16.Black);

    internal static Terminal.Gui.Drawing.Attribute TabInactive { get; } = new(ColorName16.DarkGray, ColorName16.Black);

    // Action button colors
    internal static Terminal.Gui.Drawing.Attribute ActionTest { get; } = new(ColorName16.Black, ColorName16.BrightCyan);

    internal static Terminal.Gui.Drawing.Attribute ActionApply { get; } = new(ColorName16.Black, ColorName16.BrightGreen);

    /// <summary>
    /// Registers dark theme by overriding the built-in schemes.
    /// Must be called after <c>Application.Init()</c>.
    /// </summary>
    internal static void Register()
    {
        // Override the built-in "Base" scheme so all views inherit dark colors
        var darkBase = new Scheme
        {
            Normal = Header,
            Focus = Header,
            HotNormal = Cyan,
            HotFocus = Cyan,
            Disabled = Dim,
        };

        Terminal.Gui.Configuration.SchemeManager.AddScheme(
            nameof(Schemes.Base),
            darkBase);

        // Override the built-in "Dialog" scheme
        var darkDialog = new Scheme
        {
            Normal = Header,
            Focus = Header,
            HotNormal = Cyan,
            HotFocus = Cyan,
            Disabled = Dim,
        };

        Terminal.Gui.Configuration.SchemeManager.AddScheme(
            nameof(Schemes.Dialog),
            darkDialog);

        // Override the built-in "Menu" scheme (status bar, menus)
        var darkMenu = new Scheme
        {
            Normal = StatusBar,
            Focus = StatusBarKey,
            HotNormal = new Terminal.Gui.Drawing.Attribute(ColorName16.BrightCyan, ColorName16.DarkGray),
            HotFocus = new Terminal.Gui.Drawing.Attribute(ColorName16.BrightCyan, ColorName16.DarkGray),
            Disabled = Dim,
        };

        Terminal.Gui.Configuration.SchemeManager.AddScheme(
            nameof(Schemes.Menu),
            darkMenu);
    }

    /// <summary>
    /// Creates a <see cref="Scheme"/> for the profile list.
    /// Purple background when the list has focus (Focus role),
    /// green text when unfocused (Active role).
    /// Must be applied via <c>SetScheme()</c> on the ListView.
    /// </summary>
    /// <returns>A configured <see cref="Scheme"/> for list views.</returns>
    internal static Scheme CreateListScheme()
        => new()
        {
            Normal = Header,
            Focus = ListItemFocused,
            Active = ListItemUnfocused,
            HotNormal = Header,
            HotFocus = ListItemFocused,
            HotActive = ListItemUnfocused,
            Disabled = Dim,
        };
}