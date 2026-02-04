namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for the MCP server details response from GET /v1/mcp-servers/{name}.
/// </summary>
public record McpServerDetailsDto(
    string Name,
    string Status,
    string? UpdatedAt,
    IReadOnlyList<McpServerInstanceDto>? Instances);

/// <summary>
/// DTO for an MCP server instance.
/// </summary>
public record McpServerInstanceDto(
    string InstanceId,
    string ServerName,
    string StartedAt);
