namespace Atc.Dsc.Configurations.Cli.Tests.Extensions;

public sealed class ProfileFileNameExtensionsTests
{
    [Theory]
    [InlineData("dotnet-configuration.dsc.yaml", "dotnet-configuration.dsc.yaml")]
    [InlineData("dotnet", "dotnet-configuration.dsc.yaml")]
    [InlineData("os", "os-configuration.dsc.yaml")]
    [InlineData("test.DSC.YAML", "test.DSC.YAML")]
    public void ResolveFileName_VariousInputs_ReturnsExpected(
        string input,
        string expected)
    {
        // Act & Assert
        Assert.Equal(expected, ProfileFileNameExtensions.ResolveFileName(input));
    }

    [Theory]
    [InlineData("os-configuration.dsc.yaml", "os configuration")]
    [InlineData("dotnet-configuration.dsc.yaml", "dotnet configuration")]
    [InlineData("3d-printing-configuration.dsc.yaml", "3d printing configuration")]
    [InlineData("c-cplusplus-configuration.dsc.yaml", "c cplusplus configuration")]
    [InlineData("my_profile-configuration.dsc.yaml", "my profile configuration")]
    [InlineData("simple.yaml", "simple")]
    [InlineData(".dsc.yaml", "")]
    [InlineData("a.dsc.yaml", "a")]
    [InlineData("no-extension", "no extension")]
    [InlineData("double--dash.dsc.yaml", "double  dash")]
    public void DeriveName_VariousInputs_ReturnsExpected(
        string fileName,
        string expected)
    {
        // Act & Assert
        Assert.Equal(expected, ProfileFileNameExtensions.DeriveName(fileName));
    }
}