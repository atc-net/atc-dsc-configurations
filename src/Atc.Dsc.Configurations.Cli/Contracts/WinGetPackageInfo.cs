namespace Atc.Dsc.Configurations.Cli.Contracts;

/// <summary>
/// Installed WinGet package with version and optional available upgrade.
/// </summary>
public sealed record WinGetPackageInfo(
    string Id,
    string InstalledVersion,
    string? AvailableVersion);