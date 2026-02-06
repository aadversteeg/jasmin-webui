using System.Text.Json;

namespace Core.Application.McpServers;

/// <summary>
/// Result of a tool invocation.
/// </summary>
public record ToolInvocationResult(
    IReadOnlyList<ToolContentBlock> Content,
    JsonElement? StructuredContent,
    bool IsError);

/// <summary>
/// A content block in a tool invocation result.
/// </summary>
public record ToolContentBlock(
    string Type,
    string? Text,
    string? MimeType,
    string? Data,
    string? Uri);
