namespace Atc.Dsc.Configurations.Cli.Tests.Tui;

public sealed class MainWindowTests
{
    [Theory]
    [InlineData("Microsoft.WinGet/Package", "Package")]
    [InlineData("Microsoft.Windows/WindowsPowerShell", "PowerShell")]
    [InlineData("Microsoft.DSC.Transitional/RunCommandOnSet", "Script")]
    [InlineData("Microsoft.VisualStudio.DSC/VSComponents", "VS Config")]
    [InlineData("Microsoft.DSC/Assertion", "Assertion")]
    [InlineData("Foo/Bar", "Bar")]
    [InlineData("Some.Vendor/CustomResource", "CustomResource")]
    [InlineData("SomeType", "SomeType")]
    public void AbbreviateType_VariousInputs_ReturnsExpected(
        string input,
        string expected)
    {
        // Act & Assert
        Assert.Equal(expected, MainWindow.AbbreviateType(input));
    }

    [Theory]
    [MemberData(nameof(ResolveDependencyIndexData))]
    public void ResolveDependencyIndex_VariousInputs_ReturnsExpected(
        string dependency,
        IReadOnlyList<Resource> resources,
        int? expected)
    {
        // Act & Assert
        Assert.Equal(expected, MainWindow.ResolveDependencyIndex(dependency, resources));
    }

    [Fact]
    public void GroupResourcesByType_GroupsAndSortsDescending()
    {
        // Arrange
        var resources = new List<Resource>
        {
            new("git", "Microsoft.WinGet/Package", [], null),
            new("node", "Microsoft.WinGet/Package", [], null),
            new("setup", "Microsoft.DSC.Transitional/RunCommandOnSet", [], null),
            new("vs", "Microsoft.VisualStudio.DSC/VSComponents", [], null),
            new("dotnet", "Microsoft.WinGet/Package", [], null),
        };

        // Act
        var result = MainWindow.GroupResourcesByType(resources);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(("Package", 3), result[0]);
        Assert.Equal(("Script", 1), result[1]);
        Assert.Equal(("VS Config", 1), result[2]);
    }

    [Fact]
    public void GroupResourcesByType_EmptyList_ReturnsEmpty()
    {
        var result = MainWindow.GroupResourcesByType([]);
        Assert.Empty(result);
    }

    public static TheoryData<string, IReadOnlyList<Resource>, int?>
        ResolveDependencyIndexData
        => new()
        {
            {
                "[resourceId('Microsoft.WinGet/Package', 'Install Git')]",
                new List<Resource>
                {
                    new("Install Git", "Microsoft.WinGet/Package", [], null),
                    new("Install VS Code", "Microsoft.WinGet/Package", [], null),
                },
                1
            },
            {
                "[resourceId('Microsoft.WinGet/Package', 'Install VS Code')]",
                new List<Resource>
                {
                    new("Install Git", "Microsoft.WinGet/Package", [], null),
                    new("Install VS Code", "Microsoft.WinGet/Package", [], null),
                },
                2
            },
            {
                "[resourceId('Microsoft.WinGet/Package', 'Install Node')]",
                new List<Resource>
                {
                    new("Install Git", "Microsoft.WinGet/Package", [], null),
                },
                null
            },
            {
                "not-a-valid-dep",
                new List<Resource>
                {
                    new("Install Git", "Microsoft.WinGet/Package", [], null),
                },
                null
            },
            {
                "[resourceId('Microsoft.WinGet/Package',  'Install VS Code')]",
                new List<Resource>
                {
                    new("Install VS Code", "Microsoft.WinGet/Package", [], null),
                },
                1
            },
        };
}