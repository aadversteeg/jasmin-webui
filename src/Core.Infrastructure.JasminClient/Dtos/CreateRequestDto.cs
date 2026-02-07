using System.Text.Json;

namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for creating an async server request.
/// POST /v1/mcp-servers/{serverId}/requests
/// </summary>
public record CreateRequestDto(
    string Action,
    string? InstanceId = null,
    string? ToolName = null,
    JsonElement? Input = null,
    string? PromptName = null,
    JsonElement? Arguments = null,
    string? ResourceUri = null);
