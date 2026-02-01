namespace Core.Application.McpServers;

/// <summary>
/// Represents basic information about an MCP server.
/// </summary>
public record McpServerInfo(string Name, string Status, DateTimeOffset? UpdatedAt);
