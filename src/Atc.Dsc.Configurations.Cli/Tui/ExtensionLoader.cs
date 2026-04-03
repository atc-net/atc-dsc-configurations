namespace Atc.Dsc.Configurations.Cli.Tui;

/// <summary>
/// Loads paired extension manifest files (VSCode, Visual Studio) for a
/// DSC profile and formats them for display.
/// </summary>
internal sealed class ExtensionLoader(IProfileRepository repository)
{
    private readonly HashSet<string> notFoundFiles = new(StringComparer.OrdinalIgnoreCase);

    internal async Task<string> LoadAsync(string profileFileName)
    {
        var baseName = profileFileName.Replace(
            ".dsc.yaml",
            string.Empty,
            StringComparison.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        var totalCount = 0;

        totalCount += await AppendExtensionsFromJsonAsync(
            sb,
            $"{baseName}-vscode-extensions.json",
            "VSCode Extensions",
            "name",
            "description");

        totalCount += await AppendExtensionsFromJsonAsync(
            sb,
            $"{baseName}-vs-extensions.json",
            "Visual Studio Extensions",
            "id",
            "name");

        totalCount += await AppendExtensionsFromJsonAsync(
            sb,
            $"{baseName}-npm-packages.json",
            "npm Global Packages",
            "name",
            "description",
            "packages");

        totalCount += await AppendExtensionsFromJsonAsync(
            sb,
            $"{baseName}-pip-packages.json",
            "pip Packages",
            "name",
            "description",
            "packages");

        totalCount += await AppendExtensionsFromJsonAsync(
            sb,
            $"{baseName}-az-extensions.json",
            "Azure CLI Extensions",
            "name",
            "description");

        return totalCount == 0
            ? "No paired extension files found for this profile."
            : sb.ToString();
    }

    private async Task<int> AppendExtensionsFromJsonAsync(
        StringBuilder sb,
        string fileName,
        string heading,
        string idField,
        string labelField,
        string rootArrayKey = "extensions")
    {
        if (notFoundFiles.Contains(fileName))
        {
            return 0;
        }

        var json = await repository.GetProfileContentAsync(fileName);
        if (string.IsNullOrWhiteSpace(json))
        {
            notFoundFiles.Add(fileName);
            return 0;
        }

        return RenderExtensions(sb, json, heading, idField, labelField, rootArrayKey);
    }

    private static int RenderExtensions(
        StringBuilder sb,
        string json,
        string heading,
        string idField,
        string labelField,
        string rootArrayKey = "extensions")
    {
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty(rootArrayKey, out var arr) ||
            arr.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        var count = arr.GetArrayLength();
        if (count == 0)
        {
            return 0;
        }

        if (sb.Length > 0)
        {
            sb.AppendLine();
        }

        sb.Append(' ');
        sb.Append(heading);
        sb.Append(" (");
        sb.Append(count);
        sb.AppendLine(")");
        sb.AppendLine(" " + new string('\u2500', 50));

        foreach (var ext in arr.EnumerateArray())
        {
            var id = ext.TryGetProperty(idField, out var n) ? n.GetString() ?? "?" : "?";
            var label = ext.TryGetProperty(labelField, out var d) ? d.GetString() : null;

            sb.Append("   ");
            sb.AppendLine(id);

            if (label is not null)
            {
                sb.Append("     ");
                sb.AppendLine(label);
            }
        }

        return count;
    }
}