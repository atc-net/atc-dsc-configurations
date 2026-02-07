namespace Atc.Dsc.Configurations.Cli.Commands.Settings;

/// <summary>
/// Settings for the <see cref="ListCommand"/>.
/// </summary>
public sealed class ListCommandSettings : CommandSettings
{
    [CommandOption("--json")]
    [Description("Output as JSON")]
    public bool Json { get; init; }

    [CommandOption("--verbose")]
    [Description("Show detailed information")]
    public bool Verbose { get; init; }
}