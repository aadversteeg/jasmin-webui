using Core.Domain.Events;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Events;

public class McpServerEventTests
{
    [Fact(DisplayName = "EVT-001: McpServerEvent should store all properties correctly")]
    public void EVT001()
    {
        // Arrange
        var serverName = "test-server";
        var eventType = McpServerEventType.Started;
        var timestamp = DateTimeOffset.UtcNow;
        var errors = new List<EventError> { new("ERR001", "Test error") };
        var instanceId = "instance-123";
        var requestId = "request-456";

        // Act
        var evt = new McpServerEvent(
            serverName,
            eventType,
            timestamp,
            errors,
            instanceId,
            requestId);

        // Assert
        evt.ServerName.Should().Be(serverName);
        evt.EventType.Should().Be(eventType);
        evt.Timestamp.Should().Be(timestamp);
        evt.Errors.Should().HaveCount(1);
        evt.InstanceId.Should().Be(instanceId);
        evt.RequestId.Should().Be(requestId);
    }

    [Fact(DisplayName = "EVT-002: McpServerEvent should allow null optional properties")]
    public void EVT002()
    {
        // Arrange & Act
        var evt = new McpServerEvent(
            "server",
            McpServerEventType.Starting,
            DateTimeOffset.UtcNow);

        // Assert
        evt.Errors.Should().BeNull();
        evt.InstanceId.Should().BeNull();
        evt.RequestId.Should().BeNull();
        evt.OldConfiguration.Should().BeNull();
        evt.Configuration.Should().BeNull();
    }

    [Fact(DisplayName = "EVT-003: McpServerEvent should be immutable (record)")]
    public void EVT003()
    {
        // Arrange
        var evt = new McpServerEvent(
            "server",
            McpServerEventType.Started,
            DateTimeOffset.UtcNow);

        // Act
        var modified = evt with { ServerName = "new-server" };

        // Assert
        evt.ServerName.Should().Be("server");
        modified.ServerName.Should().Be("new-server");
    }
}
