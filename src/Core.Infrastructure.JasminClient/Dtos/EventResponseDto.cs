using System.Text.Json;

namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO matching the jasmin-server EventResponse JSON structure.
/// </summary>
public record EventResponseDto(
    string EventType,
    string? Target,
    string Timestamp,
    JsonElement? Payload,
    string? RequestId);

/// <summary>
/// DTO for event errors within a payload.
/// </summary>
public record EventErrorDto(string Code, string Message);

/// <summary>
/// DTO for event configuration within a payload.
/// </summary>
public record EventConfigurationDto(
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);
