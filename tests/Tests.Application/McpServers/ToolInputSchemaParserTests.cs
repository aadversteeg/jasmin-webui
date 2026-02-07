using Core.Application.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Application.McpServers;

public class ToolInputSchemaParserTests
{
    [Fact(DisplayName = "TIP-001: Parse simple string parameter")]
    public void TIP001()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""name"": {
                    ""type"": ""string"",
                    ""description"": ""The name of the item""
                }
            },
            ""required"": [""name""]
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();
        result!.Parameters.Should().HaveCount(1);
        var param = result.Parameters[0];
        param.Name.Should().Be("name");
        param.Type.Should().Be("string");
        param.Description.Should().Be("The name of the item");
        param.Required.Should().BeTrue();
        param.NestedSchema.Should().BeNull();
        param.ItemsType.Should().BeNull();
    }

    [Fact(DisplayName = "TIP-002: Parse array of strings should set ItemsType")]
    public void TIP002()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""tags"": {
                    ""type"": ""array"",
                    ""items"": {
                        ""type"": ""string""
                    },
                    ""description"": ""List of tags""
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();
        result!.Parameters.Should().HaveCount(1);
        var param = result.Parameters[0];
        param.Name.Should().Be("tags");
        param.Type.Should().Be("array");
        param.ItemsType.Should().Be("string");
        param.NestedSchema.Should().BeNull();
    }

    [Fact(DisplayName = "TIP-003: Parse array of objects should populate NestedSchema")]
    public void TIP003()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""iterations"": {
                    ""type"": ""array"",
                    ""items"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""name"": { ""type"": ""string"" },
                            ""startDate"": { ""type"": ""string"" }
                        },
                        ""required"": [""name""]
                    }
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();
        var param = result!.Parameters[0];
        param.Name.Should().Be("iterations");
        param.Type.Should().Be("array");
        param.ItemsType.Should().Be("object");
        param.NestedSchema.Should().NotBeNull();
        param.NestedSchema!.Parameters.Should().HaveCount(2);

        var nameParam = param.NestedSchema.Parameters.First(p => p.Name == "name");
        nameParam.Type.Should().Be("string");
        nameParam.Required.Should().BeTrue();

        var dateParam = param.NestedSchema.Parameters.First(p => p.Name == "startDate");
        dateParam.Type.Should().Be("string");
        dateParam.Required.Should().BeFalse();
    }

    [Fact(DisplayName = "TIP-004: Parse object with properties should populate NestedSchema")]
    public void TIP004()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""config"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""host"": { ""type"": ""string"" },
                        ""port"": { ""type"": ""number"" }
                    }
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();
        var param = result!.Parameters[0];
        param.Name.Should().Be("config");
        param.Type.Should().Be("object");
        param.NestedSchema.Should().NotBeNull();
        param.NestedSchema!.Parameters.Should().HaveCount(2);

        var hostParam = param.NestedSchema.Parameters.First(p => p.Name == "host");
        hostParam.Type.Should().Be("string");

        var portParam = param.NestedSchema.Parameters.First(p => p.Name == "port");
        portParam.Type.Should().Be("number");
    }

    [Fact(DisplayName = "TIP-005: Parse nested array within object (multi-level)")]
    public void TIP005()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""team"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""name"": { ""type"": ""string"" },
                        ""members"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""email"": { ""type"": ""string"" },
                                    ""role"": { ""type"": ""string"" }
                                }
                            }
                        }
                    }
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();
        var teamParam = result!.Parameters[0];
        teamParam.Name.Should().Be("team");
        teamParam.Type.Should().Be("object");
        teamParam.NestedSchema.Should().NotBeNull();

        var membersParam = teamParam.NestedSchema!.Parameters.First(p => p.Name == "members");
        membersParam.Type.Should().Be("array");
        membersParam.ItemsType.Should().Be("object");
        membersParam.NestedSchema.Should().NotBeNull();
        membersParam.NestedSchema!.Parameters.Should().HaveCount(2);
    }

    [Fact(DisplayName = "TIP-006: Parse deeply nested structure (3+ levels)")]
    public void TIP006()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""level1"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""level2"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""level3"": {
                                    ""type"": ""array"",
                                    ""items"": {
                                        ""type"": ""object"",
                                        ""properties"": {
                                            ""value"": { ""type"": ""string"" }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();

        var level1 = result!.Parameters[0];
        level1.Name.Should().Be("level1");
        level1.NestedSchema.Should().NotBeNull();

        var level2 = level1.NestedSchema!.Parameters[0];
        level2.Name.Should().Be("level2");
        level2.NestedSchema.Should().NotBeNull();

        var level3 = level2.NestedSchema!.Parameters[0];
        level3.Name.Should().Be("level3");
        level3.Type.Should().Be("array");
        level3.ItemsType.Should().Be("object");
        level3.NestedSchema.Should().NotBeNull();

        var valueParam = level3.NestedSchema!.Parameters[0];
        valueParam.Name.Should().Be("value");
        valueParam.Type.Should().Be("string");
    }

    [Fact(DisplayName = "TIP-007: Handle missing properties gracefully")]
    public void TIP007()
    {
        // Arrange - schema with no properties
        var schema = @"{ ""type"": ""object"" }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "TIP-008: Parse null or empty schema returns null")]
    public void TIP008()
    {
        // Act & Assert
        ToolInputSchemaParser.Parse(null).Should().BeNull();
        ToolInputSchemaParser.Parse("").Should().BeNull();
        ToolInputSchemaParser.Parse("   ").Should().BeNull();
    }

    [Fact(DisplayName = "TIP-009: Parse invalid JSON returns null")]
    public void TIP009()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = ToolInputSchemaParser.Parse(invalidJson);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "TIP-010: Parse enum values")]
    public void TIP010()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""status"": {
                    ""type"": ""string"",
                    ""enum"": [""active"", ""inactive"", ""pending""]
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();
        var param = result!.Parameters[0];
        param.EnumValues.Should().NotBeNull();
        param.EnumValues.Should().BeEquivalentTo(["active", "inactive", "pending"]);
    }

    [Fact(DisplayName = "TIP-011: Parse default values")]
    public void TIP011()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""count"": {
                    ""type"": ""number"",
                    ""default"": 10
                },
                ""enabled"": {
                    ""type"": ""boolean"",
                    ""default"": true
                },
                ""name"": {
                    ""type"": ""string"",
                    ""default"": ""default-name""
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();

        var countParam = result!.Parameters.First(p => p.Name == "count");
        countParam.Default.Should().Be(10L);

        var enabledParam = result.Parameters.First(p => p.Name == "enabled");
        enabledParam.Default.Should().Be(true);

        var nameParam = result.Parameters.First(p => p.Name == "name");
        nameParam.Default.Should().Be("default-name");
    }

    [Fact(DisplayName = "TIP-012: Parse additionalProperties with type string")]
    public void TIP012()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""headers"": {
                    ""type"": ""object"",
                    ""additionalProperties"": {
                        ""type"": ""string""
                    },
                    ""description"": ""HTTP headers as key-value pairs""
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();
        var param = result!.Parameters[0];
        param.Name.Should().Be("headers");
        param.Type.Should().Be("object");
        param.AdditionalPropertiesType.Should().Be("string");
        param.NestedSchema.Should().BeNull();
        param.Description.Should().Be("HTTP headers as key-value pairs");
    }

    [Fact(DisplayName = "TIP-013: Parse additionalProperties with type number")]
    public void TIP013()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""scores"": {
                    ""type"": ""object"",
                    ""additionalProperties"": {
                        ""type"": ""number""
                    }
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();
        var param = result!.Parameters[0];
        param.Name.Should().Be("scores");
        param.Type.Should().Be("object");
        param.AdditionalPropertiesType.Should().Be("number");
    }

    [Fact(DisplayName = "TIP-014: Parse additionalProperties: true allows any type")]
    public void TIP014()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""metadata"": {
                    ""type"": ""object"",
                    ""additionalProperties"": true
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();
        var param = result!.Parameters[0];
        param.Name.Should().Be("metadata");
        param.Type.Should().Be("object");
        param.AdditionalPropertiesType.Should().Be("any");
    }

    [Fact(DisplayName = "TIP-015: Object with both properties and additionalProperties")]
    public void TIP015()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""env"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""PATH"": { ""type"": ""string"" }
                    },
                    ""additionalProperties"": {
                        ""type"": ""string""
                    }
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();
        var param = result!.Parameters[0];
        param.Name.Should().Be("env");
        param.Type.Should().Be("object");
        // Has both NestedSchema (fixed properties) and additionalProperties
        param.NestedSchema.Should().NotBeNull();
        param.NestedSchema!.Parameters.Should().Contain(p => p.Name == "PATH");
        param.AdditionalPropertiesType.Should().Be("string");
    }

    [Fact(DisplayName = "TIP-016: Object without additionalProperties has null AdditionalPropertiesType")]
    public void TIP016()
    {
        // Arrange
        var schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""config"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""host"": { ""type"": ""string"" }
                    }
                }
            }
        }";

        // Act
        var result = ToolInputSchemaParser.Parse(schema);

        // Assert
        result.Should().NotBeNull();
        var param = result!.Parameters[0];
        param.Name.Should().Be("config");
        param.Type.Should().Be("object");
        param.AdditionalPropertiesType.Should().BeNull();
        param.NestedSchema.Should().NotBeNull();
    }
}
