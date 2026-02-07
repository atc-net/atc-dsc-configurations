namespace Atc.Dsc.Configurations.Cli.Contracts;

/// <summary>
/// Lightweight metadata for a profile, returned by the repository listing
/// without needing to download and parse the full YAML content.
/// </summary>
public record ProfileSummary(
    string FileName,
    string Name,
    string? Description);