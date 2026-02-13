namespace Core.Application.McpServers;

/// <summary>
/// Represents a single stderr log entry from an MCP server instance.
/// </summary>
public record InstanceLogEntry(
    long LineNumber,
    DateTimeOffset Timestamp,
    string Text);
