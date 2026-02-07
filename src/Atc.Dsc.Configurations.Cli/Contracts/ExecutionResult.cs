namespace Atc.Dsc.Configurations.Cli.Contracts;

/// <summary>
/// The outcome of running <c>dsc config test/set</c>
/// against a single profile — aggregates per-resource results and overall timing.
/// </summary>
public record ExecutionResult(
    string ProfileName,
    ExecutionMode Mode,
    bool Success,
    IReadOnlyList<ResourceResult> Results,
    TimeSpan Duration);