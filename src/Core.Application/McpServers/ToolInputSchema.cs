namespace Core.Application.McpServers;

/// <summary>
/// Represents the input schema for an MCP tool.
/// </summary>
public record ToolInputSchema(
    IReadOnlyList<ToolInputParameter> Parameters);

/// <summary>
/// Represents a single parameter in a tool's input schema.
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Type">The parameter type (string, number, integer, boolean, array, object).</param>
/// <param name="Description">Optional description of the parameter.</param>
/// <param name="Required">Whether the parameter is required.</param>
/// <param name="EnumValues">Allowed values for enum types.</param>
/// <param name="Default">Default value if not provided.</param>
/// <param name="NestedSchema">Schema for object types with fixed properties or array items of object type.</param>
/// <param name="ItemsType">Type of items for array types.</param>
/// <param name="AdditionalPropertiesType">Type of values for object types with dynamic keys (additionalProperties).</param>
public record ToolInputParameter(
    string Name,
    string Type,
    string? Description,
    bool Required,
    IReadOnlyList<string>? EnumValues,
    object? Default,
    ToolInputSchema? NestedSchema,
    string? ItemsType,
    string? AdditionalPropertiesType = null);
