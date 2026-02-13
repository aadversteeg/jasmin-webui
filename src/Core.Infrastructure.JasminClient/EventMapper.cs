using System.Text.Json;
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
        if (!McpServerEventTypeMap.TryFromApiName(dto.EventType, out var eventType))
        {
            throw new ArgumentException($"Unknown event type: {dto.EventType}");
        }

        var (serverName, instanceId) = TargetHelper.ParseTarget(dto.Target);
        var timestamp = DateTimeOffset.Parse(dto.Timestamp);

        List<EventError>? errors = null;
        EventConfiguration? configuration = null;
        EventConfiguration? oldConfiguration = null;

        if (dto.Payload.HasValue && dto.Payload.Value.ValueKind != JsonValueKind.Null)
        {
            var payload = dto.Payload.Value;

            if (payload.TryGetProperty("errors", out var errorsElement) &&
                errorsElement.ValueKind == JsonValueKind.Array)
            {
                errors = new List<EventError>();
                foreach (var errorEl in errorsElement.EnumerateArray())
                {
                    var code = errorEl.TryGetProperty("code", out var codeEl) ? codeEl.GetString() ?? "" : "";
                    var message = errorEl.TryGetProperty("message", out var msgEl) ? msgEl.GetString() ?? "" : "";
                    errors.Add(new EventError(code, message));
                }
            }

            if (payload.TryGetProperty("configuration", out var configElement) &&
                configElement.ValueKind == JsonValueKind.Object)
            {
                configuration = DeserializeConfiguration(configElement);
            }

            if (payload.TryGetProperty("newConfiguration", out var newConfigElement) &&
                newConfigElement.ValueKind == JsonValueKind.Object)
            {
                configuration = DeserializeConfiguration(newConfigElement);
            }

            if (payload.TryGetProperty("oldConfiguration", out var oldConfigElement) &&
                oldConfigElement.ValueKind == JsonValueKind.Object)
            {
                oldConfiguration = DeserializeConfiguration(oldConfigElement);
            }
        }

        return new McpServerEvent(
            serverName ?? "",
            eventType,
            timestamp,
            errors,
            instanceId,
            dto.RequestId,
            oldConfiguration,
            configuration,
            rawJson);
    }

    private static EventConfiguration DeserializeConfiguration(JsonElement element)
    {
        var command = element.TryGetProperty("command", out var cmdEl)
            ? cmdEl.GetString() ?? ""
            : "";

        var args = new List<string>();
        if (element.TryGetProperty("args", out var argsEl) && argsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var arg in argsEl.EnumerateArray())
            {
                args.Add(arg.GetString() ?? "");
            }
        }

        var env = new Dictionary<string, string>();
        if (element.TryGetProperty("env", out var envEl) && envEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in envEl.EnumerateObject())
            {
                env[prop.Name] = prop.Value.GetString() ?? "";
            }
        }

        return new EventConfiguration(command, args, env);
    }
}
