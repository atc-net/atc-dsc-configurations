namespace Atc.Dsc.Configurations.Cli.Tests.Diagnostics;

public sealed class EnvironmentDetectorTests
{
    [Theory]
    [InlineData("3.2.2\n", "3.2.2")]
    [InlineData("3.2.0-preview.3\r\n", "3.2.0-preview.3")]
    [InlineData("  3.2.2  ", "3.2.2")]
    [InlineData("3.2.0", "3.2.0")]
    [InlineData("dsc 3.2.0-preview.11", "3.2.0-preview.11")]
    [InlineData("dsc  3.2.0", "3.2.0")]
    [InlineData("tool 1.0 extra", "1.0 extra")]
    public void ExtractVersion_DscOutput_ReturnsCleanVersion(
        string output,
        string expected)
    {
        // Act
        var result = EnvironmentDetector.ExtractVersion(output);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n")]
    public void ExtractVersion_EmptyOrWhitespace_ReturnsNull(string output)
    {
        // Act
        var result = EnvironmentDetector.ExtractVersion(output);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(true, "3.2.0", true)]
    [InlineData(false, null, false)]
    public void EnvironmentInfo_DscCliAvailable_ReturnsExpected(
        bool dscCliAvailable,
        string? version,
        bool expected)
    {
        // Arrange
        var info = new EnvironmentInfo(
            IsAdmin: false,
            DscCliAvailable: dscCliAvailable,
            DscCliVersion: version);

        // Act & Assert
        Assert.Equal(expected, info.DscCliAvailable);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public void CheckIsAdmin_DoesNotThrow()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        var result = EnvironmentDetector.CheckIsAdmin();

        // Assert
        Assert.IsType<bool>(result);
    }
}