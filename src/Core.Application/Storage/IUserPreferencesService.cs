namespace Core.Application.Storage;

/// <summary>
/// Service for managing user preferences with persistence.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Loads all preferences from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Gets whether preferences have been loaded.
    /// </summary>
    bool IsLoaded { get; }

    // Side Panel
    bool IsPanelOpen { get; set; }
    int PanelWidth { get; set; }

    // Filters
    IReadOnlyList<string> KnownServers { get; }
    void SetKnownServers(IEnumerable<string> servers);

    IReadOnlyList<string> SelectedServers { get; }
    void SetSelectedServers(IEnumerable<string> servers);

    IReadOnlySet<int> EnabledEventTypes { get; }
    void SetEnabledEventTypes(IEnumerable<int> eventTypes);

    bool IsServerFilterExpanded { get; set; }
    bool IsEventTypeFilterExpanded { get; set; }

    // Connection
    string? ServerUrl { get; set; }
    string? LastEventId { get; set; }

    /// <summary>
    /// Event raised when any preference changes.
    /// </summary>
    event Action? PreferencesChanged;
}
