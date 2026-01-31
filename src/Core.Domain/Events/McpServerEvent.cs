namespace Core.Domain.Events;

/// <summary>
/// Represents an MCP server event received from jasmin-server.
/// </summary>
public record McpServerEvent(
    string ServerName,
    McpServerEventType EventType,
    DateTimeOffset Timestamp,
    IReadOnlyList<EventError>? Errors = null,
    string? InstanceId = null,
    string? RequestId = null,
    EventConfiguration? OldConfiguration = null,
    EventConfiguration? Configuration = null);
