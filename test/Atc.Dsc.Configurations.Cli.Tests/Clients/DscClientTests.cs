namespace Atc.Dsc.Configurations.Cli.Tests.Clients;

public sealed class DscClientTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void ParseOutput_EmptyOrWhitespace_ReturnsEmptyResults(string json)
    {
        // Act
        var results = DscClient.ParseOutput(json, ExecutionMode.Test);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void ParseOutput_InvalidJson_ReturnsParseError()
    {
        // Act
        var results = DscClient.ParseOutput("not json at all", ExecutionMode.Test);

        // Assert
        Assert.Single(results);
        Assert.Equal(ResourceState.Failed, results[0].State);
        Assert.Contains("parse", results[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseOutput_MissingResultsKey_ReturnsEmpty()
    {
        // Arrange
        const string json = """
                            {
                              "metadata": {
                                "Microsoft.DSC": {
                                  "version": "3.2.0"
                                }
                              }
                            }
                            """;

        // Act
        var results = DscClient.ParseOutput(json, ExecutionMode.Test);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [MemberData(nameof(TestModeStateData))]
    public void ParseOutput_TestMode_ResourceState(
        string json,
        ResourceState expected)
    {
        // Act
        var results = DscClient.ParseOutput(json, ExecutionMode.Test);

        // Assert
        Assert.Single(results);
        Assert.Equal(expected, results[0].State);
    }

    public static TheoryData<string, ResourceState> TestModeStateData
        => new()
        {
            {
                """
                {
                  "results": [
                    {
                      "name": "Install Git",
                      "type": "Microsoft.WinGet/Package",
                      "result": { "inDesiredState": true }
                    }
                  ]
                }
                """,
                ResourceState.Compliant
            },
            {
                """
                {
                  "results": [
                    {
                      "name": "Install Git",
                      "type": "Microsoft.WinGet/Package",
                      "result": { "inDesiredState": false }
                    }
                  ]
                }
                """,
                ResourceState.NonCompliant
            },
        };

    [Fact]
    public void ParseOutput_TestMode_RunCommandOnSet_Skipped()
    {
        // Arrange
        const string json = """
                            {
                              "results": [
                                {
                                  "name": "Install Extensions",
                                  "type": "Microsoft.DSC.Transitional/RunCommandOnSet",
                                  "result": {}
                                }
                              ]
                            }
                            """;

        // Act
        var results = DscClient.ParseOutput(json, ExecutionMode.Test);

        // Assert
        Assert.Single(results);
        Assert.Equal(ResourceState.Skipped, results[0].State);
    }

    [Fact]
    public void ParseOutput_ApplyMode_ChangedProperties()
    {
        // Arrange
        const string json = """
                            {
                              "results": [
                                {
                                  "name": "Enable Dark Mode",
                                  "type": "Microsoft.Windows.Developer/DarkMode",
                                  "result": {
                                    "changedProperties": ["AppsTheme", "SystemTheme"]
                                  }
                                }
                              ]
                            }
                            """;

        // Act
        var results = DscClient.ParseOutput(json, ExecutionMode.Apply);

        // Assert
        Assert.Single(results);
        Assert.Equal(ResourceState.Changed, results[0].State);
    }

    [Fact]
    public void ParseOutput_ApplyMode_ScriptExecuted()
    {
        // Arrange
        const string json = """
                            {
                              "results": [
                                {
                                  "name": "Install Tools",
                                  "type": "Microsoft.DSC.Transitional/RunCommandOnSet",
                                  "result": {}
                                }
                              ]
                            }
                            """;

        // Act
        var results = DscClient.ParseOutput(json, ExecutionMode.Apply);

        // Assert
        Assert.Single(results);
        Assert.Equal(ResourceState.Executed, results[0].State);
    }

    [Fact]
    public void ParseOutput_ApplyMode_NoChanges_Compliant()
    {
        // Arrange
        const string json = """
                            {
                              "results": [
                                {
                                  "name": "Install Git",
                                  "type": "Microsoft.WinGet/Package",
                                  "result": {}
                                }
                              ]
                            }
                            """;

        // Act
        var results = DscClient.ParseOutput(json, ExecutionMode.Apply);

        // Assert
        Assert.Single(results);
        Assert.Equal(ResourceState.Compliant, results[0].State);
    }

    [Fact]
    public void ParseOutput_ResourceWithNoResult_Failed()
    {
        // Arrange
        const string json = """
                            {
                              "results": [
                                {
                                  "name": "Bad Resource",
                                  "type": "Unknown/Type"
                                }
                              ]
                            }
                            """;

        // Act
        var results = DscClient.ParseOutput(json, ExecutionMode.Test);

        // Assert
        Assert.Single(results);
        Assert.Equal(ResourceState.Failed, results[0].State);
    }

    [Fact]
    public void ParseOutput_ResourceMissingNameAndType_DefaultsToUnknown()
    {
        // Arrange
        const string json = """
                            {
                              "results": [
                                {
                                  "result": { "inDesiredState": true }
                                }
                              ]
                            }
                            """;

        // Act
        var results = DscClient.ParseOutput(json, ExecutionMode.Test);

        // Assert
        Assert.Single(results);
        Assert.Equal("unknown", results[0].Name);
        Assert.Equal("unknown", results[0].Type);
    }

    [Fact]
    public void ParseOutput_MultipleResources_ParsesAll()
    {
        // Arrange
        const string json = """
                            {
                              "results": [
                                {
                                  "name": "Resource A",
                                  "type": "Microsoft.WinGet/Package",
                                  "result": { "inDesiredState": true }
                                },
                                {
                                  "name": "Resource B",
                                  "type": "Microsoft.WinGet/Package",
                                  "result": { "inDesiredState": false }
                                }
                              ]
                            }
                            """;

        // Act
        var results = DscClient.ParseOutput(json, ExecutionMode.Test);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(ResourceState.Compliant, results[0].State);
        Assert.Equal(ResourceState.NonCompliant, results[1].State);
    }

    [Theory]
    [InlineData("PT0.1S", 100)]
    [InlineData("PT0.5S", 500)]
    [InlineData("PT1S", 1_000)]
    [InlineData("PT1M30S", 90_000)]
    [InlineData("PT5M", 300_000)]
    [InlineData("PT1H", 3_600_000)]
    [InlineData("PT2H", 7_200_000)]
    public void ParseIsoDuration_ValidDuration_ReturnsExpected(
        string iso,
        int expectedMs)
    {
        // Act
        var result = DscClient.ParseIsoDuration(iso);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedMs, result.Value.TotalMilliseconds, precision: 1);
    }

    [Fact]
    public void ParseIsoDuration_InvalidString_ReturnsNull()
        => Assert.Null(DscClient.ParseIsoDuration("not-a-duration"));

    [Fact]
    public void ParseOutput_ResourceDuration_Parsed()
    {
        // Arrange
        const string json = """
                            {
                              "results": [
                                {
                                  "name": "Install Git",
                                  "type": "Microsoft.WinGet/Package",
                                  "metadata": {
                                    "Microsoft.DSC": {
                                      "duration": "PT1.234S"
                                    }
                                  },
                                  "result": { "inDesiredState": true }
                                }
                              ]
                            }
                            """;

        // Act
        var results = DscClient.ParseOutput(json, ExecutionMode.Test);

        // Assert
        Assert.Single(results);
        Assert.NotNull(results[0].Duration);
        Assert.Equal(1234, results[0].Duration!.Value.TotalMilliseconds, precision: 1);
    }

    [Theory]
    [InlineData("\e[31mError: something failed\e[0m", "Error: something failed")]
    [InlineData("\e[1;32mSuccess\e[0m", "Success")]
    [InlineData("\e[91mred\e[0m and \e[92mgreen\e[0m", "red and green")]
    [InlineData("no escapes here", "no escapes here")]
    [InlineData("", "")]
    public void AnsiEscapeRegex_StripsAnsiSequences(
        string input,
        string expected)
    {
        // Act
        var result = DscClient.AnsiEscapeRegex().Replace(input, string.Empty);

        // Assert
        Assert.Equal(expected, result);
    }
}