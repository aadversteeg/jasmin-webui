using System.ComponentModel;
using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.McpServers;
using Core.Application.Storage;
using Core.Domain.Events;
using Microsoft.Extensions.Logging;

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
    private readonly IJasminApiService _apiService;
    private readonly ILogger<EventFilterViewModel> _logger;
    private readonly HashSet<string> _knownServers = new();
    private readonly HashSet<string> _selectedServers = new();
    private readonly HashSet<McpServerEventType> _enabledEventTypes;
    private Dictionary<string, McpServerEventType[]>? _dynamicEventTypeGroups;
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
    /// Returns dynamic groups from API if loaded, otherwise falls back to default.
    /// </summary>
    public IReadOnlyDictionary<string, McpServerEventType[]> EventTypeGroups =>
        _dynamicEventTypeGroups ?? DefaultEventTypeGroups;

    /// <summary>
    /// Default event type groups used when API data is not available.
    /// </summary>
    public static IReadOnlyDictionary<string, McpServerEventType[]> DefaultEventTypeGroups { get; } =
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

    public EventFilterViewModel(
        ILocalStorageService localStorage,
        IJasminApiService apiService,
        ILogger<EventFilterViewModel> logger)
    {
        _localStorage = localStorage;
        _apiService = apiService;
        _logger = logger;
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

    /// <summary>
    /// Loads MCP servers from the jasmin-server REST API.
    /// </summary>
    public async Task LoadServersFromApiAsync(string serverUrl)
    {
        try
        {
            var servers = await _apiService.GetMcpServersAsync(serverUrl);
            foreach (var server in servers)
            {
                AddKnownServer(server.Name);
            }
            _logger.LogInformation("Loaded {Count} servers from API", servers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load servers from API, continuing with event-based discovery");
        }
    }

    /// <summary>
    /// Loads event types from the jasmin-server REST API.
    /// </summary>
    public async Task LoadEventTypesFromApiAsync(string serverUrl)
    {
        try
        {
            var types = await _apiService.GetEventTypesAsync(serverUrl);
            if (types.Count > 0)
            {
                UpdateEventTypeGroups(types);
                _logger.LogInformation("Loaded {Count} event types from API", types.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load event types from API, using defaults");
        }
    }

    /// <summary>
    /// Handles ServerCreated and ServerDeleted events to update the filter.
    /// </summary>
    public void HandleServerEvent(McpServerEvent evt)
    {
        if (evt.EventType == McpServerEventType.ServerCreated)
        {
            AddKnownServer(evt.ServerName);
            _logger.LogDebug("Server {ServerName} added to filter via event", evt.ServerName);
        }
        else if (evt.EventType == McpServerEventType.ServerDeleted)
        {
            RemoveServer(evt.ServerName);
            _logger.LogDebug("Server {ServerName} removed from filter via event", evt.ServerName);
        }
    }

    /// <summary>
    /// Removes a server from the known and selected servers lists.
    /// </summary>
    public void RemoveServer(string serverName)
    {
        var removed = _knownServers.Remove(serverName);
        var deselected = _selectedServers.Remove(serverName);

        if (removed || deselected)
        {
            if (deselected)
            {
                _ = SaveServerFilterAsync();
            }
            OnPropertyChanged(nameof(KnownServers));
            OnPropertyChanged(nameof(SelectedServers));
            FilterChanged?.Invoke();
        }
    }

    private void UpdateEventTypeGroups(IReadOnlyList<EventTypeInfo> types)
    {
        var groups = new Dictionary<string, List<McpServerEventType>>();

        foreach (var type in types)
        {
            if (Enum.TryParse<McpServerEventType>(type.Name, out var eventType))
            {
                if (!groups.TryGetValue(type.Category, out var list))
                {
                    list = new List<McpServerEventType>();
                    groups[type.Category] = list;
                }
                list.Add(eventType);
            }
        }

        _dynamicEventTypeGroups = groups.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());

        OnPropertyChanged(nameof(EventTypeGroups));
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
