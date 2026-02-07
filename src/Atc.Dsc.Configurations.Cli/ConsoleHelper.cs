namespace Atc.Dsc.Configurations.Cli;

/// <summary>
/// Renders the CLI header banner using Atc.Console.Spectre.
/// </summary>
public static class ConsoleHelper
{
    public static void WriteHeader()
        => Console.Spectre.Helpers.ConsoleHelper.WriteHeader("ATC DSC CLI");
}