namespace Atc.Dsc.Configurations.Cli.Clients;

/// <summary>
/// Runs DSC operations (test/apply) against a profile YAML file using the DSC v3 CLI.
/// </summary>
public interface IDscClient
{
    Task<ExecutionResult> TestAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    Task<ExecutionResult> ApplyAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}