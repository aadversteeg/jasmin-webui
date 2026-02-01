namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for the MCP server list response from GET /v1/mcp-servers.
/// </summary>
public record McpServerListDto(
    string Name,
    string Status,
    string? UpdatedAt);
