namespace Atc.Dsc.Configurations.Cli.Tui;

/// <summary>
/// Loads paired extension manifest files (VSCode, Visual Studio) for a
/// DSC profile and formats them for display.
/// </summary>
internal sealed class ExtensionLoader(IProfileRepository repository)
{
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

        return totalCount == 0
            ? "No paired extension files found for this profile."
            : sb.ToString();
    }

    private async Task<int> AppendExtensionsFromJsonAsync(
        StringBuilder sb,
        string fileName,
        string heading,
        string idField,
        string labelField)
    {
        try
        {
            var json = await repository.GetProfileContentAsync(fileName);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("extensions", out var arr) ||
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
        catch (Exception ex) when (ex is HttpRequestException or FileNotFoundException or DirectoryNotFoundException)
        {
            return 0;
        }
    }
}