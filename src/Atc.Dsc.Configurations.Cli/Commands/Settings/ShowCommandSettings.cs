namespace Atc.Dsc.Configurations.Cli.Commands.Settings;

/// <summary>
/// Settings for the <see cref="ShowCommand"/>.
/// </summary>
public sealed class ShowCommandSettings : CommandSettings
{
    [CommandArgument(0, "<profile>")]
    [Description("Profile name or file name to show")]
    public string Profile { get; init; } = string.Empty;

    [CommandOption("--raw")]
    [Description("Show raw YAML content")]
    public bool Raw { get; init; }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Profile))
        {
            return ValidationResult.Error("Profile name is required.");
        }

        return ValidationResult.Success();
    }
}