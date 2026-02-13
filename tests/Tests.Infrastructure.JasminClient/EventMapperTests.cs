using System.Text.Json;
using Core.Domain.Events;
using Core.Infrastructure.JasminClient;
using Core.Infrastructure.JasminClient.Dtos;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.JasminClient;

public class EventMapperTests
{
    [Fact(DisplayName = "MAP-001: ToDomain should map all basic properties correctly")]
    public void MAP001()
    {
        // Arrange
        var dto = new EventResponseDto(
            EventType: "mcp-server.instance.started",
            Target: "mcp-servers/test-server/instances/inst-123",
            Timestamp: "2024-01-15T10:30:00Z",
            Payload: null,
            RequestId: "req-456");

        // Act
        var result = EventMapper.ToDomain(dto);

        // Assert
        result.ServerName.Should().Be("test-server");
        result.EventType.Should().Be(McpServerEventType.Started);
        result.Timestamp.Should().Be(DateTimeOffset.Parse("2024-01-15T10:30:00Z"));
        result.InstanceId.Should().Be("inst-123");
        result.RequestId.Should().Be("req-456");
        result.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "MAP-002: ToDomain should map errors from payload correctly")]
    public void MAP002()
    {
        // Arrange
        var payload = JsonSerializer.SerializeToElement(new
        {
            errors = new[]
            {
                new { code = "ERR001", message = "Connection failed" },
                new { code = "ERR002", message = "Timeout" }
            }
        });

        var dto = new EventResponseDto(
            EventType: "mcp-server.instance.start-failed",
            Target: "mcp-servers/server",
            Timestamp: "2024-01-15T10:30:00Z",
            Payload: payload,
            RequestId: null);

        // Act
        var result = EventMapper.ToDomain(dto);

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Errors![0].Code.Should().Be("ERR001");
        result.Errors[0].Message.Should().Be("Connection failed");
        result.Errors[1].Code.Should().Be("ERR002");
    }

    [Fact(DisplayName = "MAP-003: ToDomain should map configuration from payload correctly")]
    public void MAP003()
    {
        // Arrange
        var payload = JsonSerializer.SerializeToElement(new
        {
            configuration = new
            {
                command = "npx",
                args = new[] { "-y", "@modelcontextprotocol/server-filesystem" },
                env = new Dictionary<string, string> { ["PATH"] = "/usr/bin" }
            }
        });

        var dto = new EventResponseDto(
            EventType: "mcp-server.configuration.created",
            Target: "mcp-servers/server",
            Timestamp: "2024-01-15T10:30:00Z",
            Payload: payload,
            RequestId: null);

        // Act
        var result = EventMapper.ToDomain(dto);

        // Assert
        result.Configuration.Should().NotBeNull();
        result.Configuration!.Command.Should().Be("npx");
        result.Configuration.Args.Should().HaveCount(2);
        result.Configuration.Env.Should().ContainKey("PATH");
    }

    [Fact(DisplayName = "MAP-004: ToDomain should throw for unknown event type")]
    public void MAP004()
    {
        // Arrange
        var dto = new EventResponseDto(
            EventType: "unknown.event.type",
            Target: "mcp-servers/server",
            Timestamp: "2024-01-15T10:30:00Z",
            Payload: null,
            RequestId: null);

        // Act
        var act = () => EventMapper.ToDomain(dto);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unknown event type*");
    }

    [Fact(DisplayName = "MAP-005: ToDomain should extract serverName from server-only target")]
    public void MAP005()
    {
        // Arrange
        var dto = new EventResponseDto(
            EventType: "mcp-server.created",
            Target: "mcp-servers/my-server",
            Timestamp: "2024-01-15T10:30:00Z",
            Payload: null,
            RequestId: null);

        // Act
        var result = EventMapper.ToDomain(dto);

        // Assert
        result.ServerName.Should().Be("my-server");
        result.InstanceId.Should().BeNull();
    }

    [Fact(DisplayName = "MAP-006: ToDomain should map newConfiguration and oldConfiguration from payload")]
    public void MAP006()
    {
        // Arrange
        var payload = JsonSerializer.SerializeToElement(new
        {
            oldConfiguration = new
            {
                command = "old-cmd",
                args = new[] { "old-arg" },
                env = new Dictionary<string, string>()
            },
            newConfiguration = new
            {
                command = "new-cmd",
                args = new[] { "new-arg" },
                env = new Dictionary<string, string> { ["KEY"] = "val" }
            }
        });

        var dto = new EventResponseDto(
            EventType: "mcp-server.configuration.updated",
            Target: "mcp-servers/server",
            Timestamp: "2024-01-15T10:30:00Z",
            Payload: payload,
            RequestId: null);

        // Act
        var result = EventMapper.ToDomain(dto);

        // Assert
        result.OldConfiguration.Should().NotBeNull();
        result.OldConfiguration!.Command.Should().Be("old-cmd");
        result.Configuration.Should().NotBeNull();
        result.Configuration!.Command.Should().Be("new-cmd");
    }

    [Fact(DisplayName = "MAP-007: ToDomain should handle null payload gracefully")]
    public void MAP007()
    {
        // Arrange
        var dto = new EventResponseDto(
            EventType: "mcp-server.instance.stopping",
            Target: "mcp-servers/server/instances/inst-1",
            Timestamp: "2024-01-15T10:30:00Z",
            Payload: null,
            RequestId: null);

        // Act
        var result = EventMapper.ToDomain(dto);

        // Assert
        result.Errors.Should().BeNull();
        result.Configuration.Should().BeNull();
        result.OldConfiguration.Should().BeNull();
    }
}
