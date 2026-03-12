namespace Atc.Dsc.Configurations.Cli.Tui.Views;

/// <summary>
/// Represents a single action hint segment displayed in the status bar.
/// </summary>
/// <param name="Key">The key label (e.g. "Space", "t", "Enter").</param>
/// <param name="Label">The action description (e.g. "Toggle", "Test", "Apply").</param>
/// <param name="KeyAttr">The color attribute for the key portion.</param>
/// <param name="LabelAttr">The color attribute for the label portion.</param>
internal record ActionHint(
    string Key,
    string Label,
    Terminal.Gui.Drawing.Attribute KeyAttr,
    Terminal.Gui.Drawing.Attribute LabelAttr);