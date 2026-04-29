namespace Atc.Dsc.Configurations.Cli.UpdateCheck.Models;

/// <summary>
/// Represents the cached result of a NuGet version check,
/// stored in the local application data directory.
/// </summary>
internal sealed class UpdateCheckCache
{
    [JsonPropertyName("lastCheck")]
    public required DateTimeOffset LastCheck { get; set; }

    [JsonPropertyName("latestVersion")]
    public required string LatestVersion { get; set; }

    [JsonPropertyName("updatePerformed")]
    public bool UpdatePerformed { get; set; }
}