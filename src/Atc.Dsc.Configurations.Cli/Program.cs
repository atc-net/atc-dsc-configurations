namespace Atc.Dsc.Configurations.Cli;

/// <summary>
/// Entry point — wires up DI, probes the environment, and launches the CLI/TUI.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        CleanupTempFiles();

        var consoleLoggerConfiguration = BuildLoggerConfiguration();

        ProgramCsHelper.SetMinimumLogLevelIfNeeded(args, consoleLoggerConfiguration);

        var serviceCollection = ServiceCollectionFactory.Create(consoleLoggerConfiguration);

        serviceCollection.AddLogging(builder =>
        {
            builder.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        });

        RegisterServices(serviceCollection);

        // Probe environment once at startup
        var envDetector = new EnvironmentDetector();
        serviceCollection.AddSingleton<IEnvironmentDetector>(envDetector);
        var envInfo = await envDetector.CheckAsync();
        serviceCollection.AddSingleton(envInfo);

        if (!envInfo.DscCliAvailable)
        {
            AnsiConsole.MarkupLine("[red bold]No DSC v3 CLI found.[/]");
            AnsiConsole.MarkupLine("[red]The 'dsc' command was not found on PATH.[/]");
            AnsiConsole.MarkupLine("[dim]Install with: winget install Microsoft.DSC.Preview[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        var app = CommandAppFactory.CreateWithRootCommand<InteractiveCommand>(serviceCollection);
        app.ConfigureCommands();

        if (IsNonInteractiveTerminal(args))
        {
            AnsiConsole.MarkupLine("[dim]Non-interactive terminal detected, falling back to 'list'.[/]");
            args = ["list"];
        }

        return await app.RunAsync(args);
    }

    private static ConsoleLoggerConfiguration BuildLoggerConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var consoleLoggerConfiguration = new ConsoleLoggerConfiguration();
        configuration
            .GetSection("ConsoleLogger")
            .Bind(consoleLoggerConfiguration);

        return consoleLoggerConfiguration;
    }

    private static void RegisterServices(ServiceCollection serviceCollection)
    {
        // HTTP client for GitHub API
        serviceCollection.AddHttpClient("github", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "atc-dsc-cli");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        serviceCollection.AddSingleton(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        });

        serviceCollection.AddSingleton<IProfileParser, YamlProfileParser>();
        serviceCollection.AddSingleton<IDscClient, DscClient>();
        serviceCollection.AddSingleton<IInteractiveRunner, TerminalGuiRunner>();

        serviceCollection.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("github");
            var github = new GitHubProfileRepository(httpClient);
            var options = sp.GetRequiredService<JsonSerializerOptions>();
            return new CachingProfileRepository(github, options);
        });

        serviceCollection.AddSingleton<IProfileRepository>(sp => sp.GetRequiredService<CachingProfileRepository>());
    }

    private static void CleanupTempFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "atc-dsc", "downloads");
        if (!Directory.Exists(tempDir))
        {
            return;
        }

        try
        {
            Directory.Delete(tempDir, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup — files may be in use
        }
    }

    private static bool IsNonInteractiveTerminal(string[] args)
        => args.Length == 0 &&
           (System.Console.IsInputRedirected || System.Console.IsOutputRedirected);
}