namespace Atc.Dsc.Configurations.Cli.Clients;

/// <summary>
/// Queries the local WinGet installation to determine installed package
/// versions and available upgrades. Results are cached for the session.
/// Parsing logic follows the approach used by winget-tui (Rust).
/// </summary>
public sealed class WinGetClient
{
    private Dictionary<string, WinGetPackageInfo>? cache;

    /// <summary>
    /// Gets package info for a specific package ID (case-insensitive).
    /// Returns <see langword="null"/> if the package is not installed.
    /// </summary>
    /// <param name="packageId">the WinGet package identifier.</param>
    /// <param name="cancellationToken">a cancellation token.</param>
    /// <returns>The package info, or <see langword="null"/>.</returns>
    public async Task<WinGetPackageInfo?> GetPackageAsync(
        string packageId,
        CancellationToken cancellationToken = default)
    {
        var packages = await GetAllPackagesAsync(cancellationToken);
        packages.TryGetValue(packageId, out var info);
        return info;
    }

    /// <summary>
    /// Loads and caches all installed WinGet packages.
    /// </summary>
    /// <param name="cancellationToken">a cancellation token.</param>
    /// <returns>A dictionary of package ID to info.</returns>
    internal async Task<Dictionary<string, WinGetPackageInfo>> GetAllPackagesAsync(
        CancellationToken cancellationToken = default)
    {
        if (cache is not null)
        {
            return cache;
        }

        cache = await LoadInstalledPackagesAsync(cancellationToken);
        return cache;
    }

    internal static async Task<Dictionary<string, WinGetPackageInfo>> LoadInstalledPackagesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var result = await CliWrap.Cli
                .Wrap("winget")
                .WithArguments([
                    "list",
                    "--source", "winget",
                    "--accept-source-agreements",
                    "--disable-interactivity",
                ])
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cts.Token);

            return ParseWinGetListOutput(result.StandardOutput);
        }
        catch (Exception ex) when (
            ex is OperationCanceledException or Win32Exception or FileNotFoundException)
        {
            return new Dictionary<string, WinGetPackageInfo>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Parses the tabular output of <c>winget list</c>.
    /// Uses column-position detection from the header line,
    /// following the approach from winget-tui (Rust).
    /// </summary>
    /// <param name="output">the raw stdout from winget list.</param>
    /// <returns>A dictionary of package ID to info.</returns>
    internal static Dictionary<string, WinGetPackageInfo> ParseWinGetListOutput(
        string output)
    {
        var result = new Dictionary<string, WinGetPackageInfo>(StringComparer.OrdinalIgnoreCase);
        var cleaned = CleanProgressOutput(output);
        var lines = cleaned.Split('\n');

        var separatorIndex = FindSeparatorLine(lines);
        if (separatorIndex < 1)
        {
            return result;
        }

        var header = lines[separatorIndex - 1];
        var columns = DetectColumns(header);

        if (!columns.TryGetValue("Id", out var idCol) ||
            !columns.TryGetValue("Version", out var versionCol))
        {
            return result;
        }

        columns.TryGetValue("Available", out var availableCol);

        for (var i = separatorIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (IsFooterLine(line))
            {
                break;
            }

            var info = ParsePackageLine(
                line,
                idCol,
                versionCol,
                availableCol);

            if (info is not null)
            {
                result[info.Id] = info;
            }
        }

        return result;
    }

    /// <summary>
    /// Strips winget progress bar <c>\r</c> overwrites from output.
    /// Winget uses <c>\r</c> to overwrite spinner text in-place.
    /// </summary>
    /// <param name="output">the raw stdout from winget.</param>
    /// <returns>Cleaned output with progress overwrites removed.</returns>
    internal static string CleanProgressOutput(string output)
    {
        var normalized = output.Replace("\r\n", "\n", StringComparison.Ordinal);
        var sb = new StringBuilder(normalized.Length);

        foreach (var rawLine in normalized.Split('\n'))
        {
            if (rawLine.Contains('\r', StringComparison.Ordinal))
            {
                // Keep only the final segment after last \r
                var lastCr = rawLine.LastIndexOf('\r');
                sb.Append(rawLine[(lastCr + 1)..]);
            }
            else
            {
                sb.Append(rawLine);
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static int FindSeparatorLine(string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimEnd();
            if (trimmed.Length > 10 && IsSeparator(trimmed))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsSeparator(string line)
        => line.All(c => c is '-' or ' ');

    private static Dictionary<string, int> DetectColumns(string header)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var i = 0;

        while (i < header.Length)
        {
            // Skip whitespace
            while (i < header.Length && header[i] == ' ')
            {
                i++;
            }

            if (i >= header.Length)
            {
                break;
            }

            var start = i;

            // Read word
            while (i < header.Length && header[i] != ' ')
            {
                i++;
            }

            var word = header[start..i];
            columns[word] = start;
        }

        return columns;
    }

    private static WinGetPackageInfo? ParsePackageLine(
        string line,
        int idCol,
        int versionCol,
        int availableCol)
    {
        if (line.Length <= idCol)
        {
            return null;
        }

        var id = ExtractField(line, idCol, versionCol).Trim();

        // Validate ID: must contain . or \ to be a real package ID
        if (id.Length == 0 ||
            (!id.Contains('.', StringComparison.Ordinal) &&
             !id.Contains('\\', StringComparison.Ordinal)))
        {
            return null;
        }

        var versionEnd = availableCol > 0 ? availableCol : line.Length;
        var version = ExtractField(line, versionCol, versionEnd).Trim();

        string? available = null;
        if (availableCol > 0 && line.Length > availableCol)
        {
            var av = line[availableCol..].Trim();
            if (av.Length > 0)
            {
                // Strip trailing source column (e.g. "winget")
                var spaceIdx = av.IndexOf(' ', StringComparison.Ordinal);
                available = spaceIdx > 0 ? av[..spaceIdx] : av;
            }
        }

        return new WinGetPackageInfo(id, version, available);
    }

    private static string ExtractField(
        string line,
        int start,
        int end)
    {
        if (start >= line.Length)
        {
            return string.Empty;
        }

        var actualEnd = System.Math.Min(end, line.Length);
        return line[start..actualEnd];
    }

    private static bool IsFooterLine(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.Length == 0)
        {
            return false;
        }

        // Footer: "36 upgrades available." — digits only before the first space
        if (char.IsDigit(trimmed[0]))
        {
            var spaceIdx = trimmed.IndexOf(' ', StringComparison.Ordinal);
            if (spaceIdx > 0 && trimmed[..spaceIdx].All(char.IsDigit))
            {
                return true;
            }
        }

        return trimmed.StartsWith("The following", StringComparison.OrdinalIgnoreCase);
    }
}