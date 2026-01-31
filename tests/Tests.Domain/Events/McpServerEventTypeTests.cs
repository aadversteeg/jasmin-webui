using Core.Domain.Events;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Events;

public class McpServerEventTypeTests
{
    [Fact(DisplayName = "TYP-001: McpServerEventType should have 24 values")]
    public void TYP001()
    {
        // Arrange & Act
        var values = Enum.GetValues<McpServerEventType>();

        // Assert
        values.Should().HaveCount(24);
    }

    [Theory(DisplayName = "TYP-002: McpServerEventType values should have correct numeric values")]
    [InlineData(McpServerEventType.Starting, 0)]
    [InlineData(McpServerEventType.Started, 1)]
    [InlineData(McpServerEventType.StartFailed, 2)]
    [InlineData(McpServerEventType.ServerDeleted, 23)]
    public void TYP002(McpServerEventType eventType, int expectedValue)
    {
        // Assert
        ((int)eventType).Should().Be(expectedValue);
    }

    [Fact(DisplayName = "TYP-003: McpServerEventType should parse from string")]
    public void TYP003()
    {
        // Arrange & Act
        var result = Enum.Parse<McpServerEventType>("Started", ignoreCase: true);

        // Assert
        result.Should().Be(McpServerEventType.Started);
    }
}
