using Core.Application.Storage;
using Core.Domain.Events;

namespace Core.Infrastructure.BlazorApp.Services;

/// <summary>
/// Manages filter state for events.
/// </summary>
public class EventFilterState
{
    private const string ServerFilterKey = "jasmin-webui:server-filter";
    private const string EventTypeFilterKey = "jasmin-webui:event-type-filter";

    private readonly ILocalStorageService _localStorage;
    private readonly HashSet<string> _knownServers = new();
    private readonly HashSet<McpServerEventType> _enabledEventTypes;
    private string? _selectedServer;
    private bool _isInitialized;

    public IReadOnlySet<string> KnownServers => _knownServers;
    public IReadOnlySet<McpServerEventType> EnabledEventTypes => _enabledEventTypes;

    public string? SelectedServer
    {
        get => _selectedServer;
        set
        {
            if (_selectedServer != value)
            {
                _selectedServer = value;
                _ = SaveServerFilterAsync();
                NotifyStateChanged();
            }
        }
    }

    public event Action? OnChange;

    public EventFilterState(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
        _enabledEventTypes = new HashSet<McpServerEventType>(
            Enum.GetValues<McpServerEventType>());
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        var savedServer = await _localStorage.GetAsync<string>(ServerFilterKey);
        _selectedServer = savedServer;

        var savedEventTypes = await _localStorage.GetAsync<List<McpServerEventType>>(EventTypeFilterKey);
        if (savedEventTypes != null)
        {
            _enabledEventTypes.Clear();
            foreach (var eventType in savedEventTypes)
            {
                _enabledEventTypes.Add(eventType);
            }
        }

        _isInitialized = true;
        NotifyStateChanged();
    }

    public void AddKnownServer(string serverName)
    {
        if (_knownServers.Add(serverName))
        {
            NotifyStateChanged();
        }
    }

    public void SetEventTypeEnabled(McpServerEventType eventType, bool enabled)
    {
        var changed = enabled
            ? _enabledEventTypes.Add(eventType)
            : _enabledEventTypes.Remove(eventType);

        if (changed)
        {
            _ = SaveEventTypeFilterAsync();
            NotifyStateChanged();
        }
    }

    public void EnableAllEventTypes()
    {
        _enabledEventTypes.Clear();
        foreach (var eventType in Enum.GetValues<McpServerEventType>())
        {
            _enabledEventTypes.Add(eventType);
        }
        _ = SaveEventTypeFilterAsync();
        NotifyStateChanged();
    }

    public void DisableAllEventTypes()
    {
        _enabledEventTypes.Clear();
        _ = SaveEventTypeFilterAsync();
        NotifyStateChanged();
    }

    public bool IsEventTypeEnabled(McpServerEventType eventType)
    {
        return _enabledEventTypes.Contains(eventType);
    }

    public bool MatchesFilter(McpServerEvent evt)
    {
        if (_selectedServer != null && evt.ServerName != _selectedServer)
        {
            return false;
        }

        if (!_enabledEventTypes.Contains(evt.EventType))
        {
            return false;
        }

        return true;
    }

    public IEnumerable<McpServerEvent> FilterEvents(IEnumerable<McpServerEvent> events)
    {
        return events.Where(MatchesFilter);
    }

    public void ClearKnownServers()
    {
        _knownServers.Clear();
        _selectedServer = null;
        _ = SaveServerFilterAsync();
        NotifyStateChanged();
    }

    private async Task SaveServerFilterAsync()
    {
        if (_selectedServer != null)
        {
            await _localStorage.SetAsync(ServerFilterKey, _selectedServer);
        }
        else
        {
            await _localStorage.RemoveAsync(ServerFilterKey);
        }
    }

    private async Task SaveEventTypeFilterAsync()
    {
        await _localStorage.SetAsync(EventTypeFilterKey, _enabledEventTypes.ToList());
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
