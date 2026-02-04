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
                PanelWidth = Math.Clamp(saved.PanelWidth, MinPanelWidth, MaxPanelWidth)
            };
        }
        _isLoaded = true;
    }

    // Side Panel
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

    // Connection
    public string? ServerUrl
    {
        get => _preferences.ServerUrl;
        set => UpdatePreference(p => p with { ServerUrl = value });
    }

    public string? LastEventId
    {
        get => _preferences.LastEventId;
        set => UpdatePreference(p => p with { LastEventId = value });
    }

    private void UpdatePreference(Func<UserPreferences, UserPreferences> update)
    {
        _preferences = update(_preferences);
        _ = _localStorage.SetAsync(PreferencesKey, _preferences);
        PreferencesChanged?.Invoke();
    }
}
