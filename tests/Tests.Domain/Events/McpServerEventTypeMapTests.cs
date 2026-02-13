using Core.Domain.Events;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Events;

public class McpServerEventTypeMapTests
{
    [Fact(DisplayName = "ETM-001: AllApiNames should contain 24 entries")]
    public void ETM001()
    {
        // Act
        var names = McpServerEventTypeMap.AllApiNames;

        // Assert
        names.Should().HaveCount(24);
    }

    [Theory(DisplayName = "ETM-002: FromApiName should map known API name to enum")]
    [InlineData("mcp-server.instance.starting", McpServerEventType.Starting)]
    [InlineData("mcp-server.instance.started", McpServerEventType.Started)]
    [InlineData("mcp-server.instance.start-failed", McpServerEventType.StartFailed)]
    [InlineData("mcp-server.configuration.created", McpServerEventType.ConfigurationCreated)]
    [InlineData("mcp-server.metadata.tools.retrieved", McpServerEventType.ToolsRetrieved)]
    [InlineData("mcp-server.tool-invocation.invoked", McpServerEventType.ToolInvoked)]
    [InlineData("mcp-server.created", McpServerEventType.ServerCreated)]
    [InlineData("mcp-server.deleted", McpServerEventType.ServerDeleted)]
    public void ETM002(string apiName, McpServerEventType expectedType)
    {
        // Act
        var result = McpServerEventTypeMap.FromApiName(apiName);

        // Assert
        result.Should().Be(expectedType);
    }

    [Theory(DisplayName = "ETM-003: ToApiName should map enum to API name")]
    [InlineData(McpServerEventType.Starting, "mcp-server.instance.starting")]
    [InlineData(McpServerEventType.Started, "mcp-server.instance.started")]
    [InlineData(McpServerEventType.ConfigurationCreated, "mcp-server.configuration.created")]
    [InlineData(McpServerEventType.ToolInvoked, "mcp-server.tool-invocation.invoked")]
    [InlineData(McpServerEventType.ServerCreated, "mcp-server.created")]
    public void ETM003(McpServerEventType eventType, string expectedApiName)
    {
        // Act
        var result = McpServerEventTypeMap.ToApiName(eventType);

        // Assert
        result.Should().Be(expectedApiName);
    }

    [Fact(DisplayName = "ETM-004: TryFromApiName with unknown name should return false")]
    public void ETM004()
    {
        // Act
        var result = McpServerEventTypeMap.TryFromApiName("unknown.event.type", out _);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "ETM-005: Roundtrip ToApiName then FromApiName should yield original")]
    public void ETM005()
    {
        // Act & Assert
        foreach (var eventType in Enum.GetValues<McpServerEventType>())
        {
            var apiName = McpServerEventTypeMap.ToApiName(eventType);
            var roundTripped = McpServerEventTypeMap.FromApiName(apiName);
            roundTripped.Should().Be(eventType, $"roundtrip failed for {eventType}");
        }
    }
}
