namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for the event types response from GET /v1/events/types.
/// </summary>
public record EventTypesResponseDto(IReadOnlyList<EventTypeDto> Items);

/// <summary>
/// DTO for an individual event type.
/// </summary>
public record EventTypeDto(
    string Name,
    int Value,
    string Category,
    string Description);
