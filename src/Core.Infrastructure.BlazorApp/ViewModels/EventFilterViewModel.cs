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
    private const string ServerExpandedKey = "jasmin-webui:server-filter-expanded";
    private const string EventTypeExpandedKey = "jasmin-webui:event-type-filter-expanded";

    private readonly ILocalStorageService _localStorage;
    private readonly HashSet<string> _knownServers = new();
    private readonly HashSet<string> _selectedServers = new();
    private readonly HashSet<McpServerEventType> _enabledEventTypes;
    private bool _isInitialized;

    [ObservableProperty]
    private bool _isServerFilterExpanded = true;

    [ObservableProperty]
    private bool _isEventTypeFilterExpanded = true;

    public IReadOnlySet<string> KnownServers => _knownServers;
    public IReadOnlySet<string> SelectedServers => _selectedServers;
    public IReadOnlySet<McpServerEventType> EnabledEventTypes => _enabledEventTypes;

    /// <summary>
    /// Event type groups for UI organization.
    /// </summary>
    public static IReadOnlyDictionary<string, McpServerEventType[]> EventTypeGroups { get; } =
        new Dictionary<string, McpServerEventType[]>
        {
            ["Lifecycle"] = new[]
            {
                McpServerEventType.Starting,
                McpServerEventType.Started,
                McpServerEventType.StartFailed,
                McpServerEventType.Stopping,
                McpServerEventType.Stopped,
                McpServerEventType.StopFailed
            },
            ["Configuration"] = new[]
            {
                McpServerEventType.ConfigurationCreated,
                McpServerEventType.ConfigurationUpdated,
                McpServerEventType.ConfigurationDeleted
            },
            ["Tools"] = new[]
            {
                McpServerEventType.ToolsRetrieving,
                McpServerEventType.ToolsRetrieved,
                McpServerEventType.ToolsRetrievalFailed
            },
            ["Prompts"] = new[]
            {
                McpServerEventType.PromptsRetrieving,
                McpServerEventType.PromptsRetrieved,
                McpServerEventType.PromptsRetrievalFailed
            },
            ["Resources"] = new[]
            {
                McpServerEventType.ResourcesRetrieving,
                McpServerEventType.ResourcesRetrieved,
                McpServerEventType.ResourcesRetrievalFailed
            },
            ["Invocations"] = new[]
            {
                McpServerEventType.ToolInvocationAccepted,
                McpServerEventType.ToolInvoking,
                McpServerEventType.ToolInvoked,
                McpServerEventType.ToolInvocationFailed
            },
            ["Server"] = new[]
            {
                McpServerEventType.ServerCreated,
                McpServerEventType.ServerDeleted
            }
        };

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

        var savedServers = await _localStorage.GetAsync<List<string>>(ServerFilterKey);
        if (savedServers != null)
        {
            foreach (var server in savedServers)
            {
                _selectedServers.Add(server);
            }
        }

        var savedEventTypes = await _localStorage.GetAsync<List<McpServerEventType>>(EventTypeFilterKey);
        if (savedEventTypes != null)
        {
            _enabledEventTypes.Clear();
            foreach (var eventType in savedEventTypes)
            {
                _enabledEventTypes.Add(eventType);
            }
        }

        var savedServerExpanded = await _localStorage.GetAsync<bool?>(ServerExpandedKey);
        if (savedServerExpanded.HasValue)
        {
            IsServerFilterExpanded = savedServerExpanded.Value;
        }

        var savedEventTypeExpanded = await _localStorage.GetAsync<bool?>(EventTypeExpandedKey);
        if (savedEventTypeExpanded.HasValue)
        {
            IsEventTypeFilterExpanded = savedEventTypeExpanded.Value;
        }

        _isInitialized = true;
        OnPropertyChanged(nameof(SelectedServers));
        OnPropertyChanged(nameof(EnabledEventTypes));
    }

    partial void OnIsServerFilterExpandedChanged(bool value)
    {
        _ = _localStorage.SetAsync(ServerExpandedKey, value);
    }

    partial void OnIsEventTypeFilterExpandedChanged(bool value)
    {
        _ = _localStorage.SetAsync(EventTypeExpandedKey, value);
    }

    public void AddKnownServer(string serverName)
    {
        if (_knownServers.Add(serverName))
        {
            // Auto-select new servers
            _selectedServers.Add(serverName);
            OnPropertyChanged(nameof(KnownServers));
            OnPropertyChanged(nameof(SelectedServers));
        }
    }

    public bool IsServerSelected(string serverName)
    {
        return _selectedServers.Contains(serverName);
    }

    public void SetServerSelected(string serverName, bool selected)
    {
        var changed = selected
            ? _selectedServers.Add(serverName)
            : _selectedServers.Remove(serverName);

        if (changed)
        {
            _ = SaveServerFilterAsync();
            OnPropertyChanged(nameof(SelectedServers));
            FilterChanged?.Invoke();
        }
    }

    [RelayCommand]
    private void SelectAllServers()
    {
        foreach (var server in _knownServers)
        {
            _selectedServers.Add(server);
        }
        _ = SaveServerFilterAsync();
        OnPropertyChanged(nameof(SelectedServers));
        FilterChanged?.Invoke();
    }

    [RelayCommand]
    private void DeselectAllServers()
    {
        _selectedServers.Clear();
        _ = SaveServerFilterAsync();
        OnPropertyChanged(nameof(SelectedServers));
        FilterChanged?.Invoke();
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

    public void SelectEventTypeGroup(string groupName)
    {
        if (EventTypeGroups.TryGetValue(groupName, out var eventTypes))
        {
            foreach (var eventType in eventTypes)
            {
                _enabledEventTypes.Add(eventType);
            }
            _ = SaveEventTypeFilterAsync();
            OnPropertyChanged(nameof(EnabledEventTypes));
            FilterChanged?.Invoke();
        }
    }

    public void DeselectEventTypeGroup(string groupName)
    {
        if (EventTypeGroups.TryGetValue(groupName, out var eventTypes))
        {
            foreach (var eventType in eventTypes)
            {
                _enabledEventTypes.Remove(eventType);
            }
            _ = SaveEventTypeFilterAsync();
            OnPropertyChanged(nameof(EnabledEventTypes));
            FilterChanged?.Invoke();
        }
    }

    public bool IsEventTypeEnabled(McpServerEventType eventType)
    {
        return _enabledEventTypes.Contains(eventType);
    }

    public bool MatchesFilter(McpServerEvent evt)
    {
        // If servers are selected, filter by them; otherwise show all
        if (_selectedServers.Count > 0 && !_selectedServers.Contains(evt.ServerName))
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
        _selectedServers.Clear();
        OnPropertyChanged(nameof(KnownServers));
        OnPropertyChanged(nameof(SelectedServers));
    }

    private async Task SaveServerFilterAsync()
    {
        await _localStorage.SetAsync(ServerFilterKey, _selectedServers.ToList());
    }

    private async Task SaveEventTypeFilterAsync()
    {
        await _localStorage.SetAsync(EventTypeFilterKey, _enabledEventTypes.ToList());
    }

    /// <summary>
    /// Gets a display-friendly name for an event type.
    /// </summary>
    public static string GetEventTypeDisplayName(McpServerEventType eventType) => eventType switch
    {
        McpServerEventType.Starting => "Starting",
        McpServerEventType.Started => "Started",
        McpServerEventType.StartFailed => "Start Failed",
        McpServerEventType.Stopping => "Stopping",
        McpServerEventType.Stopped => "Stopped",
        McpServerEventType.StopFailed => "Stop Failed",
        McpServerEventType.ConfigurationCreated => "Created",
        McpServerEventType.ConfigurationUpdated => "Updated",
        McpServerEventType.ConfigurationDeleted => "Deleted",
        McpServerEventType.ToolsRetrieving => "Retrieving",
        McpServerEventType.ToolsRetrieved => "Retrieved",
        McpServerEventType.ToolsRetrievalFailed => "Failed",
        McpServerEventType.PromptsRetrieving => "Retrieving",
        McpServerEventType.PromptsRetrieved => "Retrieved",
        McpServerEventType.PromptsRetrievalFailed => "Failed",
        McpServerEventType.ResourcesRetrieving => "Retrieving",
        McpServerEventType.ResourcesRetrieved => "Retrieved",
        McpServerEventType.ResourcesRetrievalFailed => "Failed",
        McpServerEventType.ToolInvocationAccepted => "Accepted",
        McpServerEventType.ToolInvoking => "Invoking",
        McpServerEventType.ToolInvoked => "Invoked",
        McpServerEventType.ToolInvocationFailed => "Failed",
        McpServerEventType.ServerCreated => "Created",
        McpServerEventType.ServerDeleted => "Deleted",
        _ => eventType.ToString()
    };
}
