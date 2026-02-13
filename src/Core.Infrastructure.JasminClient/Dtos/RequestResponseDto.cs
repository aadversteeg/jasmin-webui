using System.Text.Json;

namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for a request response.
/// </summary>
public record RequestResponseDto(
    string Id,
    string Action,
    string Target,
    string Status,
    string CreatedAt,
    string? CompletedAt,
    JsonElement? Parameters,
    JsonElement? Output,
    IReadOnlyList<RequestErrorDto>? Errors);

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

/// <summary>
/// DTO for prompt invocation output.
/// </summary>
public record PromptOutputDto(
    IReadOnlyList<PromptMessageDto> Messages,
    string? Description);

/// <summary>
/// DTO for a prompt message.
/// </summary>
public record PromptMessageDto(
    string Role,
    PromptMessageContentDto Content);

/// <summary>
/// DTO for prompt message content.
/// </summary>
public record PromptMessageContentDto(
    string Type,
    string? Text,
    string? MimeType,
    string? Data,
    string? Uri);

/// <summary>
/// DTO for resource read output.
/// </summary>
public record ResourceReadOutputDto(
    IReadOnlyList<ResourceContentDto> Contents);

/// <summary>
/// DTO for resource content.
/// </summary>
public record ResourceContentDto(
    string Uri,
    string? MimeType,
    string? Text,
    string? Blob);
