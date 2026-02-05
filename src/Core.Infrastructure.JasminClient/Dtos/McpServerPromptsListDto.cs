namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for the prompts list response from GET /v1/mcp-servers/{name}/prompts.
/// </summary>
public record McpServerPromptsListDto(
    IReadOnlyList<McpServerPromptDto>? Items,
    string? RetrievedAt,
    IReadOnlyList<McpServerMetadataErrorDto>? Errors);

/// <summary>
/// DTO for a single prompt in the prompts list.
/// </summary>
public record McpServerPromptDto(
    string Name,
    string? Title,
    string? Description,
    IReadOnlyList<McpServerPromptArgumentDto>? Arguments);

/// <summary>
/// DTO for a prompt argument.
/// </summary>
public record McpServerPromptArgumentDto(
    string Name,
    string? Description,
    bool Required);

/// <summary>
/// DTO for metadata retrieval errors.
/// </summary>
public record McpServerMetadataErrorDto(
    string Code,
    string Message);
