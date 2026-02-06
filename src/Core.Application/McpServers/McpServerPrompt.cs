namespace Core.Application.McpServers;

/// <summary>
/// Represents a prompt exposed by an MCP server.
/// </summary>
public record McpServerPrompt(
    string Name,
    string? Title,
    string? Description,
    IReadOnlyList<McpServerPromptArgument> Arguments) : IExpandableItem;

/// <summary>
/// Represents an argument for an MCP server prompt.
/// </summary>
public record McpServerPromptArgument(
    string Name,
    string? Description,
    bool Required);
