namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for creating a new MCP server via POST /v1/mcp-servers.
/// </summary>
public record McpServerCreateDto(
    string Name,
    McpServerConfigurationRequestDto? Configuration);

/// <summary>
/// DTO for configuration in create/update requests.
/// </summary>
public record McpServerConfigurationRequestDto(
    string Command,
    List<string>? Args,
    Dictionary<string, string>? Env);
