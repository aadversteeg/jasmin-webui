namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for the tools list response from GET /v1/mcp-servers/{name}/tools.
/// </summary>
public record McpServerToolsListDto(
    IReadOnlyList<McpServerToolDto>? Items,
    string? RetrievedAt,
    IReadOnlyList<McpServerMetadataErrorDto>? Errors);

/// <summary>
/// DTO for a single tool in the tools list.
/// </summary>
public record McpServerToolDto(
    string Name,
    string? Title,
    string? Description,
    object? InputSchema);
