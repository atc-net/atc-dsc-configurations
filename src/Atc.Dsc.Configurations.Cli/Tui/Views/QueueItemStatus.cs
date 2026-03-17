namespace Atc.Dsc.Configurations.Cli.Tui.Views;

/// <summary>
/// Status of a profile in the execution queue.
/// </summary>
internal enum QueueItemStatus
{
    Queued,
    Running,
    Passed,
    Failed,
}