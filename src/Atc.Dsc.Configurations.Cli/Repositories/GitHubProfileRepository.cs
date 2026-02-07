namespace Atc.Dsc.Configurations.Cli.Repositories;

/// <summary>
/// Fetches DSC profile listings and YAML content from the
/// <c>atc-net/atc-dsc-configurations</c> GitHub repository using
/// the GitHub Contents API and raw.githubusercontent.com downloads.
/// </summary>
public sealed class GitHubProfileRepository : IProfileRepository
{
    private const string DefaultOwner = "atc-net";
    private const string DefaultRepo = "atc-dsc-configurations";
    private const string DefaultBranch = "main";
    private const string ConfigurationsPath = "configurations";

    private readonly HttpClient httpClient;
    private readonly string owner;
    private readonly string repo;
    private readonly string gitRef;

    public GitHubProfileRepository(
        HttpClient httpClient,
        string? owner = null,
        string? repo = null,
        string? gitRef = null)
    {
        this.httpClient = httpClient;
        this.owner = owner ?? DefaultOwner;
        this.repo = repo ?? DefaultRepo;
        this.gitRef = gitRef ?? DefaultBranch;
    }

    public async Task<IReadOnlyList<ProfileSummary>> ListProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{ConfigurationsPath}?ref={gitRef}";
        var items = await httpClient.GetFromJsonAsync<JsonElement[]>(url, cancellationToken) ?? [];

        var profiles = new List<ProfileSummary>();

        foreach (var item in items)
        {
            var name = item
                .GetProperty("name")
                .GetString() ?? string.Empty;

            if (!name.EndsWith(".dsc.yaml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            profiles.Add(new ProfileSummary(
                name,
                ProfileFileNameExtensions.DeriveName(name),
                Description: null));
        }

        return profiles;
    }

    public Task<string> GetProfileContentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://raw.githubusercontent.com/{owner}/{repo}/{gitRef}/{ConfigurationsPath}/{fileName}";
        return httpClient.GetStringAsync(new Uri(url), cancellationToken);
    }

    public void InvalidateCache()
    {
        // No-op — GitHubProfileRepository has no local cache to invalidate.
    }
}