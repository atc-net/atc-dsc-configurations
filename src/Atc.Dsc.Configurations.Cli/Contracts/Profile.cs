namespace Atc.Dsc.Configurations.Cli.Contracts;

/// <summary>
/// A fully parsed DSC configuration profile, containing its metadata
/// and the list of DSC resources it declares (packages, OS settings, etc.).
/// </summary>
[SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "Profile is the correct domain name")]
public record Profile(
    string Name,
    string FileName,
    string? Description,
    IReadOnlyList<Resource> Resources);