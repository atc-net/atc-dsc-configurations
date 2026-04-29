namespace Atc.Dsc.Configurations.Cli.UpdateCheck.Models;

/// <summary>
/// Represents the response shape of the NuGet v3 flat-container index endpoint
/// (e.g. <c>https://api.nuget.org/v3-flatcontainer/{id}/index.json</c>).
/// </summary>
internal sealed class NuGetVersionIndex
{
    [JsonPropertyName("versions")]
    public required IReadOnlyList<string> Versions { get; set; }
}