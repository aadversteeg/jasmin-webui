namespace Core.Application.Storage;

/// <summary>
/// Service for managing application state with persistence.
/// Application state includes connection and session continuity data.
/// </summary>
public interface IApplicationStateService
{
    /// <summary>
    /// Loads application state from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Gets whether state has been loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// The server URL for the event stream connection.
    /// </summary>
    string? ServerUrl { get; set; }

    /// <summary>
    /// The last received event ID for SSE reconnection continuity.
    /// </summary>
    string? LastEventId { get; set; }

    /// <summary>
    /// Event raised when any state changes.
    /// </summary>
    event Action? StateChanged;
}
