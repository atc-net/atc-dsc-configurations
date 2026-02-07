namespace Atc.Dsc.Configurations.Cli.Tui;

/// <summary>
/// Launches the interactive Terminal.Gui TUI with a two-panel layout
/// (profile list + detail view) for browsing and applying DSC configurations.
/// This is the default experience when running <c>atc-dsc</c> with no arguments.
/// </summary>
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle via app.Run")]
public sealed class TerminalGuiRunner : IInteractiveRunner
{
    private readonly IProfileRepository repository;
    private readonly IProfileParser parser;
    private readonly IDscClient dscClient;
    private readonly EnvironmentInfo envInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalGuiRunner"/> class.
    /// </summary>
    /// <param name="repository">the profile repository for loading profiles.</param>
    /// <param name="parser">the parser for profile content.</param>
    /// <param name="dscClient">the DSC client.</param>
    /// <param name="envInfo">the current environment information.</param>
    public TerminalGuiRunner(
        IProfileRepository repository,
        IProfileParser parser,
        IDscClient dscClient,
        EnvironmentInfo envInfo)
    {
        this.repository = repository;
        this.parser = parser;
        this.dscClient = dscClient;
        this.envInfo = envInfo;
    }

    /// <inheritdoc />
    public Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        using var app = Application.Create().Init();
        using var registration = cancellationToken.Register(() => app.Invoke(() => app.RequestStop()));

        var mainWindow = new MainWindow(app, repository, parser, dscClient, envInfo);

        app.Run(mainWindow);

        return Task.FromResult(0);
    }
}