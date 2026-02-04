namespace Core.Infrastructure.LocalStorage;

/// <summary>
/// Application state for connection and session continuity.
/// </summary>
public record ApplicationState
{
    /// <summary>
    /// The server URL for the event stream connection.
    /// </summary>
    public string? ServerUrl { get; init; }

    /// <summary>
    /// The last received event ID for SSE reconnection continuity.
    /// </summary>
    public string? LastEventId { get; init; }
}
