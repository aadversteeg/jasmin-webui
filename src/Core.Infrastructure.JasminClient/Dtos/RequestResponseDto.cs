using System.Text.Json;

namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for an MCP server request response.
/// </summary>
public record RequestResponseDto(
    string RequestId,
    string ServerName,
    string Action,
    string Status,
    string CreatedAt,
    string? CompletedAt,
    string? TargetInstanceId,
    string? ResultInstanceId,
    IReadOnlyList<RequestErrorDto>? Errors,
    string? ToolName = null,
    JsonElement? Input = null,
    ToolInvocationOutputDto? Output = null);

/// <summary>
/// DTO for request error information.
/// </summary>
public record RequestErrorDto(string Code, string Message);

/// <summary>
/// DTO for tool invocation output.
/// </summary>
public record ToolInvocationOutputDto(
    IReadOnlyList<ToolContentBlockDto> Content,
    JsonElement? StructuredContent,
    bool IsError);

/// <summary>
/// DTO for a tool content block.
/// </summary>
public record ToolContentBlockDto(
    string Type,
    string? Text,
    string? MimeType,
    string? Data,
    string? Uri);
