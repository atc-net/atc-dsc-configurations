namespace Atc.Dsc.Configurations.Cli.Parsers;

/// <summary>
/// Parses DSC v3 YAML configuration files into <see cref="Contracts.Profile"/> models.
/// </summary>
public sealed class YamlProfileParser : IProfileParser
{
    public Contracts.Profile Parse(
        string yamlContent,
        string fileName)
    {
        var yaml = new YamlStream();
        using var reader = new StringReader(yamlContent);
        yaml.Load(reader);

        if (yaml.Documents.Count == 0 ||
            yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            return new Contracts.Profile(
                ProfileFileNameExtensions.DeriveName(fileName),
                fileName,
                Description: null,
                Resources: []);
        }

        var description = GetMetadataDescription(root);
        var resources = ParseResourcesV3(root);

        return new Contracts.Profile(
            ProfileFileNameExtensions.DeriveName(fileName),
            fileName,
            description,
            resources);
    }

    private static string? GetMetadataDescription(YamlMappingNode root)
    {
        if (root.Children.TryGetValue(new YamlScalarNode("metadata"), out var metaNode) &&
            metaNode is YamlMappingNode metaMap)
        {
            return GetScalarValue(metaMap, "description");
        }

        return null;
    }

    private static string? GetScalarValue(
        YamlMappingNode node,
        string key)
        => node.Children.TryGetValue(new YamlScalarNode(key), out var value) &&
           value is YamlScalarNode scalar
            ? scalar.Value
            : null;

    /// <summary>
    /// DSC v3: top-level "resources" with name/type/properties fields.
    /// </summary>
    private static List<Resource> ParseResourcesV3(YamlMappingNode root)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode("resources"), out var resourcesNode) ||
            resourcesNode is not YamlSequenceNode resourceSequence)
        {
            return [];
        }

        var resources = new List<Resource>();

        foreach (var item in resourceSequence)
        {
            if (item is not YamlMappingNode resourceNode)
            {
                continue;
            }

            var name = GetScalarValue(resourceNode, "name") ?? "unnamed";
            var type = GetScalarValue(resourceNode, "type") ?? "unknown";
            var dependsOn = ParseDependsOn(resourceNode);
            var properties = ParseMap(resourceNode, "properties");

            resources.Add(new Resource(name, type, dependsOn, properties));
        }

        return resources;
    }

    private static List<string> ParseDependsOn(YamlMappingNode node)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode("dependsOn"), out var dependsOnNode) ||
            dependsOnNode is not YamlSequenceNode dependsOnSequence)
        {
            return [];
        }

        return dependsOnSequence
            .OfType<YamlScalarNode>()
            .Select(x => x.Value ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();
    }

    private static Dictionary<string, object>? ParseMap(
        YamlMappingNode node,
        string key)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode(key), out var propertiesNode) ||
            propertiesNode is not YamlMappingNode propertiesMap)
        {
            return null;
        }

        var dict = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var kvp in propertiesMap.Children)
        {
            if (kvp.Key is YamlScalarNode { Value: not null } keyNode)
            {
                dict[keyNode.Value] = ConvertYamlNode(kvp.Value);
            }
        }

        return dict.Count > 0
            ? dict
            : null;
    }

    private static object ConvertYamlNode(YamlNode node)
        => node switch
        {
            YamlScalarNode scalar => scalar.Value ?? string.Empty,
            YamlSequenceNode sequence => sequence
                .Select(ConvertYamlNode)
                .ToList(),
            YamlMappingNode map => map.Children
                .Where(kvp => kvp.Key is YamlScalarNode { Value: not null })
                .ToDictionary(
                    kvp => ((YamlScalarNode)kvp.Key).Value!,
                    kvp => ConvertYamlNode(kvp.Value),
                    StringComparer.Ordinal),
            _ => node.ToString(),
        };
}