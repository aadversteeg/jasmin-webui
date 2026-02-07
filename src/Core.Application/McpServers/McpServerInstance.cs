namespace Core.Application.McpServers;

/// <summary>
/// Represents a running instance of an MCP server.
/// </summary>
public record McpServerInstance(
    string InstanceId,
    string ServerName,
    DateTimeOffset StartedAt);
