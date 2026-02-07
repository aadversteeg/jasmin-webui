namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for the instance list response from GET /v1/mcp-servers/{name}/instances.
/// </summary>
public record InstanceListResponseDto(IReadOnlyList<InstanceResponseDto> Items);

/// <summary>
/// DTO for an instance in the list response.
/// </summary>
public record InstanceResponseDto(
    string InstanceId,
    string ServerName,
    string StartedAt);
