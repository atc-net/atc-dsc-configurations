namespace Atc.Dsc.Configurations.Cli.Extensions;

internal static class ProfileRepositoryExtensions
{
    internal static async Task<string> DownloadToTempAsync(
        this IProfileRepository repository,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(safeFileName))
        {
            throw new ArgumentException("Invalid file name.", nameof(fileName));
        }

        var content = await repository.GetProfileContentAsync(fileName, cancellationToken);
        var tempDir = Path.Combine(Path.GetTempPath(), "atc-dsc", "downloads");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, safeFileName);
        await File.WriteAllTextAsync(tempPath, content, cancellationToken);
        return tempPath;
    }
}