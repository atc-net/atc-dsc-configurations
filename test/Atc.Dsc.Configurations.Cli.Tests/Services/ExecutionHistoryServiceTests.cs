namespace Atc.Dsc.Configurations.Cli.Tests.Services;

public sealed class ExecutionHistoryServiceTests : IDisposable
{
    private readonly string tempDir;
    private readonly JsonSerializerOptions jsonOptions;

    public ExecutionHistoryServiceTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "atc-dsc-test-" + Guid.NewGuid().ToString("N")[..8]);
        jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    [Fact]
    public void GetAll_EmptyHistory_ReturnsEmpty()
    {
        using var svc = CreateService();
        Assert.Empty(svc.GetAll());
    }

    [Fact]
    public async Task RecordAsync_AddsEntry()
    {
        using var svc = CreateService();

        await svc.RecordAsync(CreateResult(true), "test.dsc.yaml");

        var all = svc.GetAll();
        Assert.Single(all);
        Assert.Equal("test.dsc.yaml", all[0].FileName);
        Assert.True(all[0].Success);
    }

    [Fact]
    public async Task GetLatest_ReturnsNewest()
    {
        using var svc = CreateService();

        await svc.RecordAsync(CreateResult(false), "a.dsc.yaml");
        await svc.RecordAsync(CreateResult(true), "a.dsc.yaml");

        var latest = svc.GetLatest("a.dsc.yaml");
        Assert.NotNull(latest);
        Assert.True(latest.Success);
    }

    [Fact]
    public void GetLatest_NoMatch_ReturnsNull()
    {
        using var svc = CreateService();
        Assert.Null(svc.GetLatest("nonexistent.dsc.yaml"));
    }

    [Fact]
    public async Task RecordAsync_TrimsAtMaxEntries()
    {
        using var svc = CreateService();

        for (var i = 0; i < ExecutionHistoryService.MaxEntries + 10; i++)
        {
            await svc.RecordAsync(CreateResult(true), $"p{i}.dsc.yaml");
        }

        Assert.Equal(ExecutionHistoryService.MaxEntries, svc.GetAll().Count);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private ExecutionHistoryService CreateService()
        => new(jsonOptions, tempDir);

    private static ExecutionResult CreateResult(bool success)
        => new(
            "test profile",
            ExecutionMode.Test,
            success,
            [new ResourceResult("r1", "Package", success ? ResourceState.Compliant : ResourceState.Failed, null)],
            TimeSpan.FromSeconds(1.5));
}