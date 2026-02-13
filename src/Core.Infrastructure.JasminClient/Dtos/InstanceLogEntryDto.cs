namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for a single log entry from the instance log SSE stream.
/// Event name: instance-log
/// </summary>
public record InstanceLogEntryDto(
    long LineNumber,
    string Timestamp,
    string Text);
