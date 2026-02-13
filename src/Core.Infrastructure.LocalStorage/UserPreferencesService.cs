using Core.Application.McpServers;
using Core.Application.Storage;

namespace Core.Infrastructure.LocalStorage;

/// <summary>
/// Service for managing user preferences with localStorage persistence.
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private const string PreferencesKey = "jasmin-webui:preferences";
    private const int MinPanelWidth = 200;
    private const int MaxPanelWidth = 800;
    private const int DefaultPanelWidth = 400;
    private const int DefaultLeftPanelWidth = 300;

    private readonly ILocalStorageService _localStorage;
    private UserPreferences _preferences = new();
    private bool _isLoaded;

    public bool IsLoaded => _isLoaded;
    public event Action? PreferencesChanged;

    public UserPreferencesService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        if (_isLoaded) return;

        var saved = await _localStorage.GetAsync<UserPreferences>(PreferencesKey);
        if (saved != null)
        {
            _preferences = saved with
            {
                PanelWidth = Math.Clamp(saved.PanelWidth, MinPanelWidth, MaxPanelWidth),
                LeftPanelWidth = Math.Clamp(saved.LeftPanelWidth, MinPanelWidth, MaxPanelWidth),
                ToolInvocationInputPanelWidthPercent = Math.Clamp(saved.ToolInvocationInputPanelWidthPercent, 20, 80),
                InstanceManagementPanelWidthPercent = Math.Clamp(saved.InstanceManagementPanelWidthPercent, 20, 80)
            };
        }
        _isLoaded = true;
    }

    // Right Panel (Filters)
    public bool IsPanelOpen
    {
        get => _preferences.IsPanelOpen;
        set => UpdatePreference(p => p with { IsPanelOpen = value });
    }

    public int PanelWidth
    {
        get => _preferences.PanelWidth;
        set => UpdatePreference(p => p with { PanelWidth = Math.Clamp(value, MinPanelWidth, MaxPanelWidth) });
    }

    // Left Panel
    public bool IsLeftPanelOpen
    {
        get => _preferences.IsLeftPanelOpen;
        set => UpdatePreference(p => p with { IsLeftPanelOpen = value });
    }

    public int LeftPanelWidth
    {
        get => _preferences.LeftPanelWidth;
        set => UpdatePreference(p => p with { LeftPanelWidth = Math.Clamp(value, MinPanelWidth, MaxPanelWidth) });
    }

    // Filters
    public IReadOnlyList<string> KnownServers => _preferences.KnownServers;

    public void SetKnownServers(IEnumerable<string> servers)
    {
        UpdatePreference(p => p with { KnownServers = servers.ToList() });
    }

    public IReadOnlyList<string> SelectedServers => _preferences.SelectedServers;

    public void SetSelectedServers(IEnumerable<string> servers)
    {
        UpdatePreference(p => p with { SelectedServers = servers.ToList() });
    }

    public IReadOnlySet<int> EnabledEventTypes => _preferences.EnabledEventTypes.ToHashSet();

    public void SetEnabledEventTypes(IEnumerable<int> eventTypes)
    {
        UpdatePreference(p => p with { EnabledEventTypes = eventTypes.ToList() });
    }

    public bool IsServerFilterExpanded
    {
        get => _preferences.IsServerFilterExpanded;
        set => UpdatePreference(p => p with { IsServerFilterExpanded = value });
    }

    public bool IsEventTypeFilterExpanded
    {
        get => _preferences.IsEventTypeFilterExpanded;
        set => UpdatePreference(p => p with { IsEventTypeFilterExpanded = value });
    }

    public bool ShowConfigAsJson
    {
        get => _preferences.ShowConfigAsJson;
        set => UpdatePreference(p => p with { ShowConfigAsJson = value });
    }

    // MCP Server dialog preferences
    public bool AutoRefreshMetadataOnAdd
    {
        get => _preferences.AutoRefreshMetadataOnAdd;
        set => UpdatePreference(p => p with { AutoRefreshMetadataOnAdd = value });
    }

    // Tool invocation dialog
    public int ToolInvocationInputPanelWidthPercent
    {
        get => _preferences.ToolInvocationInputPanelWidthPercent;
        set => UpdatePreference(p => p with { ToolInvocationInputPanelWidthPercent = Math.Clamp(value, 20, 80) });
    }

    public int ToolInvocationHistoryMaxItems
    {
        get => _preferences.ToolInvocationHistoryMaxItems;
        set => UpdatePreference(p => p with { ToolInvocationHistoryMaxItems = Math.Clamp(value, 1, 100) });
    }

    // Instance management dialog
    public int InstanceManagementPanelWidthPercent
    {
        get => _preferences.InstanceManagementPanelWidthPercent;
        set => UpdatePreference(p => p with { InstanceManagementPanelWidthPercent = Math.Clamp(value, 20, 80) });
    }

    // Instance lifecycle preferences (per server)

    public InstanceLifecycleMode GetInstanceLifecycleMode(string serverName)
    {
        if (_preferences.ServerInstanceLifecycleMode.TryGetValue(serverName, out var mode))
        {
            return (InstanceLifecycleMode)mode;
        }
        return InstanceLifecycleMode.PerDialog; // Default
    }

    public void SetInstanceLifecycleMode(string serverName, InstanceLifecycleMode mode)
    {
        var dict = new Dictionary<string, int>(_preferences.ServerInstanceLifecycleMode)
        {
            [serverName] = (int)mode
        };
        UpdatePreference(p => p with { ServerInstanceLifecycleMode = dict });
    }

    public string? GetSelectedInstanceId(string serverName)
    {
        _preferences.ServerSelectedInstanceId.TryGetValue(serverName, out var instanceId);
        return instanceId;
    }

    public void SetSelectedInstanceId(string serverName, string? instanceId)
    {
        var dict = new Dictionary<string, string>(_preferences.ServerSelectedInstanceId);
        if (instanceId != null)
        {
            dict[serverName] = instanceId;
        }
        else
        {
            dict.Remove(serverName);
        }
        UpdatePreference(p => p with { ServerSelectedInstanceId = dict });
    }

    private void UpdatePreference(Func<UserPreferences, UserPreferences> update)
    {
        _preferences = update(_preferences);
        _ = _localStorage.SetAsync(PreferencesKey, _preferences);
        PreferencesChanged?.Invoke();
    }
}
