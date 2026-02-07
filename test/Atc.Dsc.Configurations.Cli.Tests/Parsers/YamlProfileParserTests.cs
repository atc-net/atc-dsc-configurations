namespace Atc.Dsc.Configurations.Cli.Tests.Parsers;

public sealed class YamlProfileParserTests
{
    private readonly YamlProfileParser parser = new();

    [Fact]
    public void Parse_ValidProfile_ReturnsProfileWithResources()
    {
        // Arrange
        const string yaml = """
                            $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
                            metadata:
                              description: Test profile
                            resources:
                              - name: Install Git
                                type: Microsoft.WinGet.DSC/WinGetPackage
                                properties:
                                  id: Git.Git
                              - name: Enable Developer Mode
                                type: Microsoft.Windows.Developer/DeveloperMode
                                dependsOn:
                                  - "[resourceId('Microsoft.WinGet.DSC/WinGetPackage', 'Install Git')]"
                            """;

        // Act
        var profile = parser.Parse(yaml, "test-configuration.dsc.yaml");

        // Assert
        Assert.Equal("test configuration", profile.Name);
        Assert.Equal("test-configuration.dsc.yaml", profile.FileName);
        Assert.Equal("Test profile", profile.Description);
        Assert.Equal(2, profile.Resources.Count);
        Assert.Equal("Install Git", profile.Resources[0].Name);
        Assert.Equal("Microsoft.WinGet.DSC/WinGetPackage", profile.Resources[0].Type);
        Assert.Empty(profile.Resources[0].DependsOn);
        Assert.NotNull(profile.Resources[0].Properties);
        Assert.Equal("Git.Git", profile.Resources[0].Properties!["id"]);
        Assert.Single(profile.Resources[1].DependsOn);
    }

    [Fact]
    public void Parse_EmptyYaml_ReturnsEmptyProfile()
    {
        // Act
        var profile = parser.Parse(string.Empty, "empty.dsc.yaml");

        // Assert
        Assert.Equal("empty", profile.Name);
        Assert.Empty(profile.Resources);
    }

    [Fact]
    public void Parse_NoResources_ReturnsEmptyResourceList()
    {
        // Arrange
        var yaml = """
            $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
            metadata:
              description: A profile with no resources
            """;

        // Act
        var profile = parser.Parse(yaml, "no-resources.dsc.yaml");

        // Assert
        Assert.Equal("A profile with no resources", profile.Description);
        Assert.Empty(profile.Resources);
    }

    [Fact]
    public void Parse_DerivesNameFromFileName()
    {
        // Arrange
        const string yaml = """
                            $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
                            resources: []
                            """;

        // Act
        var profile = parser.Parse(yaml, "azure-configuration.dsc.yaml");

        // Assert
        Assert.Equal("azure configuration", profile.Name);
    }

    [Fact]
    public void Parse_MultipleDependencies_ParsesAll()
    {
        // Arrange
        const string yaml = """
                            $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
                            resources:
                              - name: Install Git
                                type: Microsoft.WinGet/Package
                                properties:
                                  id: Git.Git
                              - name: Install Node
                                type: Microsoft.WinGet/Package
                                properties:
                                  id: OpenJS.NodeJS
                              - name: Install Tools
                                type: Microsoft.DSC.Transitional/RunCommandOnSet
                                dependsOn:
                                  - "[resourceId('Microsoft.WinGet/Package', 'Install Git')]"
                                  - "[resourceId('Microsoft.WinGet/Package', 'Install Node')]"
                                properties:
                                  executable: pwsh
                            """;

        // Act
        var profile = parser.Parse(yaml, "multi.dsc.yaml");

        // Assert
        Assert.Equal(3, profile.Resources.Count);
        Assert.Equal(2, profile.Resources[2].DependsOn.Count);
        Assert.Contains("Install Git", profile.Resources[2].DependsOn[0], StringComparison.Ordinal);
        Assert.Contains("Install Node", profile.Resources[2].DependsOn[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_NestedProperties_PreservesStructure()
    {
        // Arrange
        const string yaml = """
                            $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
                            resources:
                              - name: Install VS Code
                                type: Microsoft.WinGet/Package
                                properties:
                                  id: Microsoft.VisualStudioCode
                                  source: winget
                            """;

        // Act
        var profile = parser.Parse(yaml, "nested.dsc.yaml");

        // Assert
        Assert.Single(profile.Resources);
        Assert.NotNull(profile.Resources[0].Properties);
        Assert.Equal("Microsoft.VisualStudioCode", profile.Resources[0].Properties!["id"]);
        Assert.Equal("winget", profile.Resources[0].Properties!["source"]);
    }

    [Fact]
    public void Parse_NullYaml_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            parser.Parse(null!, "null.dsc.yaml"));
    }

    [Fact]
    public void Parse_MalformedYaml_Throws()
    {
        // Act & Assert
        Assert.ThrowsAny<Exception>(() =>
            parser.Parse("{{{{not yaml", "bad.dsc.yaml"));
    }

    [Fact]
    public void Parse_V3Format_NoDescription_DescriptionIsNull()
    {
        // Arrange
        const string yaml = """
                            $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
                            resources:
                              - name: Test
                                type: Microsoft.WinGet/Package
                                properties:
                                  id: Test.Package
                            """;

        // Act
        var profile = parser.Parse(yaml, "nodesc.dsc.yaml");

        // Assert
        Assert.Null(profile.Description);
        Assert.Single(profile.Resources);
    }

    [Fact]
    public void Parse_DscV3WithMetadata_ParsesResources()
    {
        // Arrange
        const string yaml = """
                            $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
                            metadata:
                              winget:
                                processor:
                                  identifier: dscv3
                            resources:
                              - name: Install Git
                                type: Microsoft.WinGet/Package
                                properties:
                                  id: Git.Git
                            """;

        // Act
        var profile = parser.Parse(yaml, "meta.dsc.yaml");

        // Assert
        Assert.Single(profile.Resources);
        Assert.Equal("Install Git", profile.Resources[0].Name);
    }

    [Theory]
    [InlineData("os-configuration.dsc.yaml", "os configuration")]
    [InlineData("dotnet-configuration.dsc.yaml", "dotnet configuration")]
    [InlineData("3d-printing-configuration.dsc.yaml", "3d printing configuration")]
    [InlineData("c-cplusplus-configuration.dsc.yaml", "c cplusplus configuration")]
    public void Parse_VariousFileNames_DerivesCorrectName(
        string fileName,
        string expectedName)
    {
        // Arrange
        const string yaml = """
                            $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
                            resources: []
                            """;

        // Act
        var profile = parser.Parse(yaml, fileName);

        // Assert
        Assert.Equal(expectedName, profile.Name);
    }

    [Fact]
    public void Parse_ResourceMissingNameAndType_DefaultsToUnnamedAndUnknown()
    {
        // Arrange
        const string yaml = """
                            $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
                            resources:
                              - properties:
                                  id: Some.Package
                            """;

        // Act
        var profile = parser.Parse(yaml, "defaults.dsc.yaml");

        // Assert
        Assert.Single(profile.Resources);
        Assert.Equal("unnamed", profile.Resources[0].Name);
        Assert.Equal("unknown", profile.Resources[0].Type);
    }
}