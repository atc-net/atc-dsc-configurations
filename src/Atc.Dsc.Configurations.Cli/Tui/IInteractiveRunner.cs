namespace Atc.Dsc.Configurations.Cli.Tui;

/// <summary>
/// Abstraction for the interactive TUI experience. Decouples Terminal.Gui
/// from the rest of the codebase so the TUI framework can be swapped if needed.
/// </summary>
public interface IInteractiveRunner
{
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}