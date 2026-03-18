namespace Atc.Dsc.Configurations.Cli.Contracts;

/// <summary>
/// Snapshot of the runtime environment captured once at startup:
/// admin elevation, DSC CLI, and WinGet availability.
/// </summary>
public sealed record EnvironmentInfo(
    bool IsAdmin,
    bool DscCliAvailable,
    string? DscCliVersion,
    bool WinGetAvailable,
    string? WinGetVersion);