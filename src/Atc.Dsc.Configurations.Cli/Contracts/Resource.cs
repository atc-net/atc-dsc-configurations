namespace Atc.Dsc.Configurations.Cli.Contracts;

/// <summary>
/// A single DSC resource entry within a profile — represents one action such as
/// installing a WinGet package, toggling a Windows setting, or running a script.
/// </summary>
public record Resource(
    string Name,
    string Type,
    IReadOnlyList<string> DependsOn,
    IReadOnlyDictionary<string, object>? Properties);