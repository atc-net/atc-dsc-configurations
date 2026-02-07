namespace Atc.Dsc.Configurations.Cli.Contracts;

/// <summary>
/// Lifecycle state of a DSC resource during test or apply execution.
/// </summary>
public enum ResourceState
{
    Compliant,
    NonCompliant,
    Changed,
    Executed,
    Failed,
    Skipped,
}