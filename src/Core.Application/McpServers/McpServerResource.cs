namespace Core.Application.McpServers;

/// <summary>
/// Represents a resource exposed by an MCP server.
/// </summary>
public record McpServerResource(
    string Name,
    string Uri,
    string? Title,
    string? Description,
    string? MimeType) : IExpandableItem;
