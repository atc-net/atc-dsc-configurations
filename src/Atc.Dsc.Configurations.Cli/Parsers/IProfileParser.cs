namespace Atc.Dsc.Configurations.Cli.Parsers;

/// <summary>
/// Parses raw DSC YAML content into a <see cref="Contracts.Profile"/> domain model.
/// Only reads structure for display — never modifies the YAML.
/// </summary>
public interface IProfileParser
{
    Contracts.Profile Parse(
        string yamlContent,
        string fileName);
}