using Core.Application.McpServers;

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

    // Right Panel (Filters)
    bool IsPanelOpen { get; set; }
    int PanelWidth { get; set; }

    // Left Panel
    bool IsLeftPanelOpen { get; set; }
    int LeftPanelWidth { get; set; }

    // Filters
    IReadOnlyList<string> KnownServers { get; }
    void SetKnownServers(IEnumerable<string> servers);

    IReadOnlyList<string> SelectedServers { get; }
    void SetSelectedServers(IEnumerable<string> servers);

    IReadOnlySet<int> EnabledEventTypes { get; }
    void SetEnabledEventTypes(IEnumerable<int> eventTypes);

    bool IsServerFilterExpanded { get; set; }
    bool IsEventTypeFilterExpanded { get; set; }

    // Configuration view
    bool ShowConfigAsJson { get; set; }

    // MCP Server dialog preferences
    bool AutoRefreshMetadataOnAdd { get; set; }

    // Tool invocation dialog
    int ToolInvocationInputPanelWidthPercent { get; set; }
    int ToolInvocationHistoryMaxItems { get; set; }

    // Instance management dialog
    int InstanceManagementPanelWidthPercent { get; set; }

    // Instance lifecycle preferences (per server)

    /// <summary>
    /// Gets the instance lifecycle mode for a server.
    /// </summary>
    InstanceLifecycleMode GetInstanceLifecycleMode(string serverName);

    /// <summary>
    /// Sets the instance lifecycle mode for a server.
    /// </summary>
    void SetInstanceLifecycleMode(string serverName, InstanceLifecycleMode mode);

    /// <summary>
    /// Gets the selected instance ID for a server (used when mode is ExistingInstance).
    /// </summary>
    string? GetSelectedInstanceId(string serverName);

    /// <summary>
    /// Sets the selected instance ID for a server.
    /// </summary>
    void SetSelectedInstanceId(string serverName, string? instanceId);

    /// <summary>
    /// Event raised when any preference changes.
    /// </summary>
    event Action? PreferencesChanged;
}
