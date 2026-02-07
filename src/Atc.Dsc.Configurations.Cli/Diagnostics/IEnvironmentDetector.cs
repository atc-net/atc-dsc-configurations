namespace Atc.Dsc.Configurations.Cli.Diagnostics;

/// <summary>
/// Probes the system once at startup to build an <see cref="EnvironmentInfo"/> snapshot.
/// </summary>
public interface IEnvironmentDetector
{
    Task<EnvironmentInfo> CheckAsync(
        CancellationToken cancellationToken = default);
}