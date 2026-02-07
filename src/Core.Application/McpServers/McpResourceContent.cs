namespace Core.Application.McpServers;

/// <summary>
/// Represents the content of a resource read from an MCP server.
/// </summary>
public record McpResourceContent(
    string Uri,
    string? MimeType,
    string? Text,
    string? Blob);

/// <summary>
/// Result of reading a resource from an MCP server.
/// </summary>
public record McpResourceReadResult(
    IReadOnlyList<McpResourceContent> Contents);
