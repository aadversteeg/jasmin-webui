namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for the event types response from GET /v1/events/types.
/// </summary>
public record EventTypesResponseDto(IReadOnlyList<EventTypeDto> EventTypes);

/// <summary>
/// DTO for an individual event type.
/// </summary>
public record EventTypeDto(
    string Name,
    string Category,
    string Description);
