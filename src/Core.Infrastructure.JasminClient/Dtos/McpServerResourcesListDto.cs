namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for the resources list response from GET /v1/mcp-servers/{name}/resources.
/// </summary>
public record McpServerResourcesListDto(
    IReadOnlyList<McpServerResourceDto>? Items,
    string? RetrievedAt,
    IReadOnlyList<McpServerMetadataErrorDto>? Errors);

/// <summary>
/// DTO for a single resource in the resources list.
/// </summary>
public record McpServerResourceDto(
    string Name,
    string Uri,
    string? Title,
    string? Description,
    string? MimeType);
