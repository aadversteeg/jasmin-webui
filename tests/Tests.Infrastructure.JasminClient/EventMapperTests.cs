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
            ServerName: "test-server",
            EventType: "Started",
            Timestamp: "2024-01-15T10:30:00Z",
            Errors: null,
            InstanceId: "inst-123",
            RequestId: "req-456",
            OldConfiguration: null,
            Configuration: null);

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

    [Fact(DisplayName = "MAP-002: ToDomain should map errors correctly")]
    public void MAP002()
    {
        // Arrange
        var dto = new EventResponseDto(
            ServerName: "server",
            EventType: "StartFailed",
            Timestamp: "2024-01-15T10:30:00Z",
            Errors: new List<EventErrorDto>
            {
                new("ERR001", "Connection failed"),
                new("ERR002", "Timeout")
            },
            InstanceId: null,
            RequestId: null,
            OldConfiguration: null,
            Configuration: null);

        // Act
        var result = EventMapper.ToDomain(dto);

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Errors![0].Code.Should().Be("ERR001");
        result.Errors[0].Message.Should().Be("Connection failed");
        result.Errors[1].Code.Should().Be("ERR002");
    }

    [Fact(DisplayName = "MAP-003: ToDomain should map configuration correctly")]
    public void MAP003()
    {
        // Arrange
        var dto = new EventResponseDto(
            ServerName: "server",
            EventType: "ConfigurationCreated",
            Timestamp: "2024-01-15T10:30:00Z",
            Errors: null,
            InstanceId: null,
            RequestId: null,
            OldConfiguration: null,
            Configuration: new EventConfigurationDto(
                Command: "npx",
                Args: new List<string> { "-y", "@modelcontextprotocol/server-filesystem" },
                Env: new Dictionary<string, string> { ["PATH"] = "/usr/bin" }));

        // Act
        var result = EventMapper.ToDomain(dto);

        // Assert
        result.Configuration.Should().NotBeNull();
        result.Configuration!.Command.Should().Be("npx");
        result.Configuration.Args.Should().HaveCount(2);
        result.Configuration.Env.Should().ContainKey("PATH");
    }

    [Theory(DisplayName = "MAP-004: ToDomain should parse event types case-insensitively")]
    [InlineData("started", McpServerEventType.Started)]
    [InlineData("STARTED", McpServerEventType.Started)]
    [InlineData("Started", McpServerEventType.Started)]
    [InlineData("toolInvoked", McpServerEventType.ToolInvoked)]
    public void MAP004(string eventTypeString, McpServerEventType expectedType)
    {
        // Arrange
        var dto = new EventResponseDto(
            ServerName: "server",
            EventType: eventTypeString,
            Timestamp: "2024-01-15T10:30:00Z",
            Errors: null,
            InstanceId: null,
            RequestId: null,
            OldConfiguration: null,
            Configuration: null);

        // Act
        var result = EventMapper.ToDomain(dto);

        // Assert
        result.EventType.Should().Be(expectedType);
    }
}
