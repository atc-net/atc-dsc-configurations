namespace Atc.Dsc.Configurations.Cli.Services;

/// <summary>
/// Persists and queries execution history entries.
/// </summary>
public interface IExecutionHistoryService
{
    /// <summary>
    /// Gets all history entries, most recent first.
    /// </summary>
    /// <returns>A read-only list of all entries.</returns>
    IReadOnlyList<HistoryEntry> GetAll();

    /// <summary>
    /// Gets the most recent entry for a given profile file name.
    /// </summary>
    /// <param name="fileName">the profile file name.</param>
    /// <returns>The latest entry, or <see langword="null"/> if none exists.</returns>
    HistoryEntry? GetLatest(string fileName);

    /// <summary>
    /// Records an execution result to history.
    /// </summary>
    /// <param name="result">the execution result.</param>
    /// <param name="fileName">the profile file name.</param>
    /// <returns>A task representing the async save operation.</returns>
    Task RecordAsync(
        ExecutionResult result,
        string fileName);
}