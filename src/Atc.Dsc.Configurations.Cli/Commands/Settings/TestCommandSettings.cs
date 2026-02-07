namespace Atc.Dsc.Configurations.Cli.Commands.Settings;

/// <summary>
/// Settings for the <see cref="TestCommand"/>.
/// </summary>
public sealed class TestCommandSettings : CommandSettings
{
    [CommandArgument(0, "<profiles>")]
    [Description("Profile name(s) to test")]
    public string[] Profiles { get; init; } = [];

    [CommandOption("--json")]
    [Description("Output results as JSON")]
    public bool Json { get; init; }

    [CommandOption("--verbose")]
    [Description("Show detailed output")]
    public bool Verbose { get; init; }

    [CommandOption("--continue")]
    [Description("Continue testing remaining profiles on failure")]
    public bool Continue { get; init; }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        if (Profiles.Length == 0)
        {
            return ValidationResult.Error("Specify at least one profile to test.");
        }

        return ValidationResult.Success();
    }
}