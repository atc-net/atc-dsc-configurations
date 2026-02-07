namespace Atc.Dsc.Configurations.Cli.Contracts;

/// <summary>
/// Whether we are testing current state (read-only) or applying desired state (mutating).
/// </summary>
public enum ExecutionMode
{
    Test,
    Apply,
}