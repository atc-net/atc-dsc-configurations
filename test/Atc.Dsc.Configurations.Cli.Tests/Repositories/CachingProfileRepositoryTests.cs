namespace Atc.Dsc.Configurations.Cli.Tests.Repositories;

public sealed class CachingProfileRepositoryTests : IDisposable
{
    private readonly string cacheDir = Path.Combine(Path.GetTempPath(), "atc-dsc-tests", Guid.NewGuid().ToString("N"));
    private readonly IProfileRepository inner = Substitute.For<IProfileRepository>();
    private readonly JsonSerializerOptions jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    public void Dispose()
    {
        if (Directory.Exists(cacheDir))
        {
            Directory.Delete(cacheDir, recursive: true);
        }
    }

    [Fact]
    public async Task ListProfilesAsync_CacheMiss_CallsInnerAndCaches()
    {
        // Arrange
        var expected = new List<ProfileSummary>
        {
            new("test.dsc.yaml", "test", "A test profile"),
        };

        inner
            .ListProfilesAsync(Arg.Any<CancellationToken>())
            .Returns(expected);

        using var cache = new CachingProfileRepository(inner, jsonOptions, cacheDir);

        // Act
        var result = await cache.ListProfilesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("test", result[0].Name);

        await inner
            .Received(1)
            .ListProfilesAsync(Arg.Any<CancellationToken>());

        // Act - second call should hit cache, not inner
        var result2 = await cache.ListProfilesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result2);

        await inner
            .Received(1)
            .ListProfilesAsync(Arg.Any<CancellationToken>()); // still 1
    }

    [Fact]
    public async Task GetProfileContentAsync_CacheMiss_CallsInnerAndCaches()
    {
        // Arrange
        inner
            .GetProfileContentAsync("test.dsc.yaml", Arg.Any<CancellationToken>())
            .Returns("yaml content here");

        using var cache = new CachingProfileRepository(inner, jsonOptions, cacheDir);

        // Act
        var content = await cache.GetProfileContentAsync("test.dsc.yaml", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("yaml content here", content);

        await inner
            .Received(1)
            .GetProfileContentAsync("test.dsc.yaml", Arg.Any<CancellationToken>());

        // Act - second call hits cache
        var content2 = await cache.GetProfileContentAsync("test.dsc.yaml", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("yaml content here", content2);

        await inner
            .Received(1)
            .GetProfileContentAsync("test.dsc.yaml", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProfileContentAsync_ExpiredCache_RefetchesFromInner()
    {
        // Arrange
        inner
            .GetProfileContentAsync("test.dsc.yaml", Arg.Any<CancellationToken>())
            .Returns("original", "updated");

        using var cache = new CachingProfileRepository(inner, jsonOptions, cacheDir, ttl: TimeSpan.Zero);

        // Act
        var first = await cache.GetProfileContentAsync("test.dsc.yaml", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("original", first);

        // Act - TTL is zero so it's always expired, next call re-fetches
        var second = await cache.GetProfileContentAsync("test.dsc.yaml", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("updated", second);

        await inner
            .Received(2)
            .GetProfileContentAsync("test.dsc.yaml", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateCache_ClearsAllCachedFiles()
    {
        // Arrange
        inner
            .GetProfileContentAsync("test.dsc.yaml", Arg.Any<CancellationToken>())
            .Returns("content");

        using var cache = new CachingProfileRepository(inner, jsonOptions, cacheDir);

        await cache.GetProfileContentAsync("test.dsc.yaml", TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(cacheDir, "test.dsc.yaml")));

        // Act
        cache.InvalidateCache();

        // Assert
        Assert.Empty(Directory.GetFiles(cacheDir));
    }

    [Fact]
    public async Task DownloadToTempAsync_WritesToTempDirectory()
    {
        // Arrange
        inner
            .GetProfileContentAsync("test.dsc.yaml", Arg.Any<CancellationToken>())
            .Returns("yaml content");

        using var cache = new CachingProfileRepository(inner, jsonOptions, cacheDir);

        // Act
        var tempPath = await cache.DownloadToTempAsync("test.dsc.yaml", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(File.Exists(tempPath));
        Assert.Equal("yaml content", await File.ReadAllTextAsync(tempPath, TestContext.Current.CancellationToken));

        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ListProfilesAsync_ExpiredCache_RefetchesFromInner()
    {
        // Arrange
        var first = new List<ProfileSummary> { new("a.dsc.yaml", "a", "First") };
        var second = new List<ProfileSummary> { new("b.dsc.yaml", "b", "Second") };

        inner
            .ListProfilesAsync(Arg.Any<CancellationToken>())
            .Returns(first, second);

        using var cache = new CachingProfileRepository(inner, jsonOptions, cacheDir, ttl: TimeSpan.Zero);

        // Act
        var result1 = await cache.ListProfilesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result1);
        Assert.Equal("a", result1[0].Name);

        // Act - TTL is zero so it's always expired, next call re-fetches
        var result2 = await cache.ListProfilesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result2);
        Assert.Equal("b", result2[0].Name);
        await inner
            .Received(2)
            .ListProfilesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListProfilesAsync_NullCacheContent_FallsBackToInner()
    {
        // Arrange — pre-seed cache with JSON "null" (deserializes to null, exercising the null-guard)
        Directory.CreateDirectory(cacheDir);
        var indexPath = Path.Combine(cacheDir, "profiles-index.json");
        await File.WriteAllTextAsync(indexPath, "null", TestContext.Current.CancellationToken);

        var expected = new List<ProfileSummary> { new("test.dsc.yaml", "test", "A test profile") };
        inner
            .ListProfilesAsync(Arg.Any<CancellationToken>())
            .Returns(expected);

        using var cache = new CachingProfileRepository(inner, jsonOptions, cacheDir);

        // Act
        var result = await cache.ListProfilesAsync(TestContext.Current.CancellationToken);

        // Assert — should fall through to inner despite cache file existing
        Assert.Single(result);
        Assert.Equal("test", result[0].Name);
        await inner
            .Received(1)
            .ListProfilesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("..\\..\\etc\\passwd")]
    [InlineData("C:\\Windows\\System32\\evil.yaml")]
    [InlineData("/etc/passwd")]
    [InlineData("sub/dir/file.yaml")]
    public async Task GetProfileContentAsync_PathTraversal_UsesFileNameOnly(
        string maliciousInput)
    {
        // Arrange
        var expectedFileName = Path.GetFileName(maliciousInput);
        inner
            .GetProfileContentAsync(maliciousInput, Arg.Any<CancellationToken>())
            .Returns("safe content");

        using var cache = new CachingProfileRepository(inner, jsonOptions, cacheDir);

        // Act
        var content = await cache.GetProfileContentAsync(maliciousInput, TestContext.Current.CancellationToken);

        // Assert — file should be written inside the cache dir with only the file name portion
        Assert.Equal("safe content", content);
        Assert.True(File.Exists(Path.Combine(cacheDir, expectedFileName)));
    }

    [Fact]
    public void Constructor_CreatesCacheDirectory()
    {
        // Arrange
        var dir = Path.Combine(cacheDir, "sub");
        Assert.False(Directory.Exists(dir));

        // Act
        using var cache = new CachingProfileRepository(inner, jsonOptions, dir);

        // Assert
        Assert.True(Directory.Exists(dir));
    }
}