namespace Atc.Dsc.Configurations.Cli.Extensions;

/// <summary>
/// Shared helpers for resolving DSC profile file names and deriving
/// human-readable names from file paths.
/// </summary>
internal static class ProfileFileNameExtensions
{
    /// <summary>
    /// If the input already ends with <c>.dsc.yaml</c> returns it as-is;
    /// otherwise appends <c>-configuration.dsc.yaml</c>.
    /// </summary>
    /// <param name="profile">the profile name or file name.</param>
    /// <returns>The resolved file name ending in <c>.dsc.yaml</c>.</returns>
    internal static string ResolveFileName(string profile)
        => profile.EndsWith(".dsc.yaml", StringComparison.OrdinalIgnoreCase)
            ? profile
            : $"{profile}-configuration.dsc.yaml";

    /// <summary>
    /// Derives a human-readable profile name from a DSC YAML file name
    /// by stripping the extension and replacing dashes/underscores with spaces.
    /// </summary>
    /// <param name="fileName">the DSC YAML file name.</param>
    /// <returns>A human-readable name derived from the file name.</returns>
    internal static string DeriveName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);

        name = name.Replace(".dsc", string.Empty, StringComparison.OrdinalIgnoreCase);
        name = name.Replace('-', ' ');

        return name.Replace('_', ' ');
    }
}