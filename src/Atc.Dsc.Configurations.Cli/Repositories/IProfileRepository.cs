namespace Atc.Dsc.Configurations.Cli.Repositories;

/// <summary>
/// Provides access to DSC profile YAML files — implementations fetch from
/// GitHub, a local directory, or a file-based cache. Used by all commands
/// to discover and retrieve configuration profiles.
/// </summary>
public interface IProfileRepository
{
    Task<IReadOnlyList<ProfileSummary>> ListProfilesAsync(
        CancellationToken cancellationToken = default);

    Task<string> GetProfileContentAsync(
        string fileName,
        CancellationToken cancellationToken = default);

    void InvalidateCache();
}