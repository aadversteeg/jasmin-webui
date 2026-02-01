using System.ComponentModel;
using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.Storage;
using Core.Domain.Events;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for event filtering functionality.
/// </summary>
public partial class EventFilterViewModel : ViewModelBase
{
    private const string ServerFilterKey = "jasmin-webui:server-filter";
    private const string EventTypeFilterKey = "jasmin-webui:event-type-filter";

    private readonly ILocalStorageService _localStorage;
    private readonly HashSet<string> _knownServers = new();
    private readonly HashSet<McpServerEventType> _enabledEventTypes;
    private bool _isInitialized;

    [ObservableProperty]
    private string? _selectedServer;

    public IReadOnlySet<string> KnownServers => _knownServers;
    public IReadOnlySet<McpServerEventType> EnabledEventTypes => _enabledEventTypes;

    /// <summary>
    /// Event raised when filter state changes in a way that affects filtered results.
    /// </summary>
    public event Action? FilterChanged;

    public EventFilterViewModel(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
        _enabledEventTypes = new HashSet<McpServerEventType>(
            Enum.GetValues<McpServerEventType>());
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        var savedServer = await _localStorage.GetAsync<string>(ServerFilterKey);
        SetProperty(ref _selectedServer, savedServer, nameof(SelectedServer));

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
        OnPropertyChanged(nameof(SelectedServer));
        OnPropertyChanged(nameof(EnabledEventTypes));
    }

    partial void OnSelectedServerChanged(string? value)
    {
        _ = SaveServerFilterAsync();
        FilterChanged?.Invoke();
    }

    public void AddKnownServer(string serverName)
    {
        if (_knownServers.Add(serverName))
        {
            OnPropertyChanged(nameof(KnownServers));
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
            OnPropertyChanged(nameof(EnabledEventTypes));
            FilterChanged?.Invoke();
        }
    }

    [RelayCommand]
    private void EnableAllEventTypes()
    {
        _enabledEventTypes.Clear();
        foreach (var eventType in Enum.GetValues<McpServerEventType>())
        {
            _enabledEventTypes.Add(eventType);
        }
        _ = SaveEventTypeFilterAsync();
        OnPropertyChanged(nameof(EnabledEventTypes));
        FilterChanged?.Invoke();
    }

    [RelayCommand]
    private void DisableAllEventTypes()
    {
        _enabledEventTypes.Clear();
        _ = SaveEventTypeFilterAsync();
        OnPropertyChanged(nameof(EnabledEventTypes));
        FilterChanged?.Invoke();
    }

    public bool IsEventTypeEnabled(McpServerEventType eventType)
    {
        return _enabledEventTypes.Contains(eventType);
    }

    public bool MatchesFilter(McpServerEvent evt)
    {
        if (SelectedServer != null && evt.ServerName != SelectedServer)
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
        SelectedServer = null;
        OnPropertyChanged(nameof(KnownServers));
    }

    private async Task SaveServerFilterAsync()
    {
        if (SelectedServer != null)
        {
            await _localStorage.SetAsync(ServerFilterKey, SelectedServer);
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
}
