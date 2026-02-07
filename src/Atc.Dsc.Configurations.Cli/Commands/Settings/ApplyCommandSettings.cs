namespace Atc.Dsc.Configurations.Cli.Commands.Settings;

/// <summary>
/// Settings for the <see cref="ApplyCommand"/>.
/// </summary>
public sealed class ApplyCommandSettings : CommandSettings
{
    [CommandArgument(0, "[profiles]")]
    [Description("Profile name(s) to apply")]
    public string[]? Profiles { get; init; }

    [CommandOption("--all")]
    [Description("Apply all available profiles")]
    public bool All { get; init; }

    [CommandOption("--file")]
    [Description("Apply a local YAML file directly")]
    public string? File { get; init; }

    [CommandOption("--json")]
    [Description("Output results as JSON")]
    public bool Json { get; init; }

    [CommandOption("--yes")]
    [Description("Skip confirmation prompt")]
    public bool Yes { get; init; }

    [CommandOption("--continue")]
    [Description("Continue applying remaining profiles on failure")]
    public bool Continue { get; init; }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        if (Profiles is null or { Length: 0 } && !All && File is null)
        {
            return ValidationResult.Error("Specify profile(s), --all, or --file.");
        }

        return ValidationResult.Success();
    }
}