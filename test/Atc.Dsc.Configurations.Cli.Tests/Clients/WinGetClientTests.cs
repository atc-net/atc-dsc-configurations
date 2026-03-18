namespace Atc.Dsc.Configurations.Cli.Tests.Clients;

public sealed class WinGetClientTests
{
    [Fact]
    public void ParseWinGetListOutput_CleanTable_ParsesCorrectly()
    {
        // Arrange — columns aligned exactly like real winget output
        //                         0         1         2         3         4         5
        //                         0123456789012345678901234567890123456789012345678901234567
        var output =
            "Name                     Id                  Version      Available\n" +
            "--------------------------------------------------------------------\n" +
            "7-Zip 25.01 (x64)        7zip.7zip           25.01        26.00\n" +
            "Git                      Git.Git             2.53.0.2\n" +
            "Fork                     Fork.Fork           2.17.1\n";

        // Act
        var result = WinGetClient.ParseWinGetListOutput(output);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result.TryGetValue("7zip.7zip", out var zip));
        Assert.Equal("25.01", zip!.InstalledVersion);
        Assert.Equal("26.00", zip.AvailableVersion);

        Assert.True(result.TryGetValue("Git.Git", out var git));
        Assert.Equal("2.53.0.2", git!.InstalledVersion);
        Assert.Null(git.AvailableVersion);
    }

    [Fact]
    public void ParseWinGetListOutput_WithProgressChars_ParsesCorrectly()
    {
        // Arrange — simulated progress + header on same line
        var output =
            "\r   - \r   \\ \r\rName                Id                Version    Available\r\n" +
            "-----------------------------------------------------------\r\n" +
            "Git                 Git.Git            2.53.0.2\r\n";

        // Act
        var result = WinGetClient.ParseWinGetListOutput(output);

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey("Git.Git"));
    }

    [Fact]
    public void ParseWinGetListOutput_EmptyOutput_ReturnsEmpty()
    {
        var result = WinGetClient.ParseWinGetListOutput(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void CleanProgressOutput_StripsCarriageReturns()
    {
        var input = "\r   - \r   \\ \r   | \rName  Id  Version\r\n---\r\n";

        var cleaned = WinGetClient.CleanProgressOutput(input);

        Assert.Contains("Name", cleaned, StringComparison.Ordinal);
        Assert.DoesNotContain("\\", cleaned, StringComparison.Ordinal);
    }
}