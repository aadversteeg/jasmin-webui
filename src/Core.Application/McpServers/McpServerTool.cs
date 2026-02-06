namespace Core.Application.McpServers;

/// <summary>
/// Represents a tool exposed by an MCP server.
/// </summary>
public record McpServerTool(
    string Name,
    string? Title,
    string? Description,
    ToolInputSchema? InputSchema) : IExpandableItem;
