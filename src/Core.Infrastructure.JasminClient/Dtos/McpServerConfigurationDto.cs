namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for the configuration response from GET /v1/mcp-servers/{name}/configuration.
/// </summary>
public record McpServerConfigurationDto(
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);
