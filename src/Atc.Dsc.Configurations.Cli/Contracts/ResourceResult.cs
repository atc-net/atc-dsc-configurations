namespace Atc.Dsc.Configurations.Cli.Contracts;

/// <summary>
/// The test/apply result for a single DSC resource within a profile execution.
/// </summary>
public record ResourceResult(
    string Name,
    string Type,
    ResourceState State,
    string? ErrorMessage,
    TimeSpan? Duration = null,
    string? StatusText = null);