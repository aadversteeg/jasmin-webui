using Core.Application.Storage;

namespace Core.Infrastructure.LocalStorage;

/// <summary>
/// Service for managing application state with localStorage persistence.
/// </summary>
public class ApplicationStateService : IApplicationStateService
{
    private const string StateKey = "jasmin-webui:app-state";
    private const string LegacyPreferencesKey = "jasmin-webui:preferences";
    private const string LegacyServerUrlKey = "jasmin-webui:server-url";

    private readonly ILocalStorageService _localStorage;
    private ApplicationState _state = new();
    private bool _isLoaded;

    public bool IsLoaded => _isLoaded;
    public event Action? StateChanged;

    public ApplicationStateService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        if (_isLoaded) return;

        var saved = await _localStorage.GetAsync<ApplicationState>(StateKey);
        if (saved != null)
        {
            _state = saved;
        }
        else
        {
            // Migrate from legacy storage locations
            await MigrateLegacyDataAsync();
        }

        _isLoaded = true;
    }

    public string? ServerUrl
    {
        get => _state.ServerUrl;
        set => UpdateState(s => s with { ServerUrl = value });
    }

    public string? LastEventId
    {
        get => _state.LastEventId;
        set => UpdateState(s => s with { LastEventId = value });
    }

    private void UpdateState(Func<ApplicationState, ApplicationState> update)
    {
        _state = update(_state);
        _ = _localStorage.SetAsync(StateKey, _state);
        StateChanged?.Invoke();
    }

    private async Task MigrateLegacyDataAsync()
    {
        string? serverUrl = null;
        string? lastEventId = null;

        // Try to get server URL from ConfigurationViewModel's legacy key
        serverUrl = await _localStorage.GetAsync<string>(LegacyServerUrlKey);

        // Try to get data from legacy preferences
        var legacyPrefs = await _localStorage.GetAsync<LegacyUserPreferences>(LegacyPreferencesKey);
        if (legacyPrefs != null)
        {
            // Prefer the legacy server-url key if set, otherwise use preferences
            serverUrl ??= legacyPrefs.ServerUrl;
            lastEventId = legacyPrefs.LastEventId;
        }

        // If we found any legacy data, save it to the new location
        if (serverUrl != null || lastEventId != null)
        {
            _state = new ApplicationState
            {
                ServerUrl = serverUrl,
                LastEventId = lastEventId
            };
            await _localStorage.SetAsync(StateKey, _state);
        }
    }

    /// <summary>
    /// Legacy preferences record for migration purposes.
    /// </summary>
    private record LegacyUserPreferences
    {
        public string? ServerUrl { get; init; }
        public string? LastEventId { get; init; }
    }
}
