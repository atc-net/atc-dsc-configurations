namespace Atc.Dsc.Configurations.Cli.Extensions;

/// <summary>
/// Registers all CLI sub-commands on the Spectre.Console <see cref="CommandApp{TDefaultCommand}"/>.
/// </summary>
public static class CommandAppExtensions
{
    public static void ConfigureCommands(
        this CommandApp<InteractiveCommand> app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Configure(config =>
        {
            config
                .AddCommand<ListCommand>("list")
                .WithDescription("List available DSC profiles")
                .WithExample("list")
                .WithExample("list", "--verbose")
                .WithExample("list", "--json");

            config
                .AddCommand<ShowCommand>("show")
                .WithDescription("Show details for a specific profile")
                .WithExample("show", "dotnet")
                .WithExample("show", "dotnet", "--raw");

            config
                .AddCommand<TestCommand>("test")
                .WithDescription("Test one or more profiles against current system state")
                .WithExample("test", "dotnet")
                .WithExample("test", "dotnet", "azure")
                .WithExample("test", "dotnet", "--verbose")
                .WithExample("test", "dotnet", "--json")
                .WithExample("test", "dotnet", "azure", "--continue");

            config
                .AddCommand<ApplyCommand>("apply")
                .WithDescription("Apply one or more profiles to configure the system")
                .WithExample("apply", "dotnet")
                .WithExample("apply", "dotnet", "azure")
                .WithExample("apply", "--all")
                .WithExample("apply", "--file", "my-config.dsc.yaml")
                .WithExample("apply", "dotnet", "--yes")
                .WithExample("apply", "dotnet", "azure", "--continue");

            config
                .AddCommand<UpdateCommand>("update")
                .WithDescription("Force refresh profiles from GitHub")
                .WithExample("update");
        });
    }
}