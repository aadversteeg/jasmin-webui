using Core.Domain.Events;
using Core.Infrastructure.JasminClient.Dtos;

namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Maps DTOs to domain models.
/// </summary>
public static class EventMapper
{
    /// <summary>
    /// Maps an EventResponseDto to a domain McpServerEvent.
    /// </summary>
    public static McpServerEvent ToDomain(EventResponseDto dto, string? rawJson = null)
    {
        var eventType = Enum.Parse<McpServerEventType>(dto.EventType, ignoreCase: true);
        var timestamp = DateTimeOffset.Parse(dto.Timestamp);

        return new McpServerEvent(
            dto.ServerName,
            eventType,
            timestamp,
            dto.Errors?.Select(e => new EventError(e.Code, e.Message)).ToList(),
            dto.InstanceId,
            dto.RequestId,
            dto.OldConfiguration != null ? ToDomain(dto.OldConfiguration) : null,
            dto.Configuration != null ? ToDomain(dto.Configuration) : null,
            rawJson);
    }

    private static EventConfiguration ToDomain(EventConfigurationDto dto)
    {
        return new EventConfiguration(dto.Command, dto.Args, dto.Env);
    }
}
