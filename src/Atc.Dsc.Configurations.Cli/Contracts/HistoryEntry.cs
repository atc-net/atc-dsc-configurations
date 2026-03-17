namespace Atc.Dsc.Configurations.Cli.Contracts;

/// <summary>
/// A single execution history record persisted to disk.
/// </summary>
public sealed record HistoryEntry(
    string ProfileName,
    string FileName,
    ExecutionMode Mode,
    bool Success,
    int TotalResources,
    int PassedResources,
    int FailedResources,
    double DurationSeconds,
    DateTimeOffset Timestamp);