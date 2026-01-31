namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO matching the jasmin-server EventResponse JSON structure.
/// </summary>
public record EventResponseDto(
    string ServerName,
    string EventType,
    string Timestamp,
    IReadOnlyList<EventErrorDto>? Errors,
    string? InstanceId,
    string? RequestId,
    EventConfigurationDto? OldConfiguration,
    EventConfigurationDto? Configuration);

/// <summary>
/// DTO for event errors.
/// </summary>
public record EventErrorDto(string Code, string Message);

/// <summary>
/// DTO for event configuration.
/// </summary>
public record EventConfigurationDto(
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);
