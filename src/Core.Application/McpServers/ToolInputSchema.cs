namespace Core.Application.McpServers;

/// <summary>
/// Represents the input schema for an MCP tool.
/// </summary>
public record ToolInputSchema(
    IReadOnlyList<ToolInputParameter> Parameters);

/// <summary>
/// Represents a single parameter in a tool's input schema.
/// </summary>
public record ToolInputParameter(
    string Name,
    string Type,
    string? Description,
    bool Required,
    IReadOnlyList<string>? EnumValues,
    object? Default,
    ToolInputSchema? NestedSchema);
