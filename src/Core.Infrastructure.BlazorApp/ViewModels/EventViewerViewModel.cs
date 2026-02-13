using System.ComponentModel;
using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.Events;
using Core.Application.McpServers;
using Core.Application.Storage;
using Core.Domain.Events;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for the main event viewer page.
/// </summary>
public partial class EventViewerViewModel : ViewModelBase, IDisposable
{
    private const string EventStreamPath = "/v1/events/stream";
    private const int MaxEvents = 1000;
    private const string DefaultServerUrl = "http://localhost:5000";

    private readonly IEventStreamService _eventStreamService;
    private readonly IApplicationStateService _appState;
    private readonly IMcpServerDetailService _serverDetailService;
    private readonly List<McpServerEvent> _events = new();
    private bool _isInitialized;

    [ObservableProperty]
    private string _serverUrl = DefaultServerUrl;

    [ObservableProperty]
    private string? _lastError;

    /// <summary>
    /// Child ViewModel for filter state.
    /// </summary>
    public EventFilterViewModel FilterViewModel { get; }

    /// <summary>
    /// Child ViewModel for MCP server list.
    /// </summary>
    public McpServerListViewModel ServerListViewModel { get; }

    /// <summary>
    /// Child ViewModel for managing expanded state of events.
    /// </summary>
    public ExpandableItemsViewModel EventExpandState { get; } = new();

    /// <summary>
    /// All received events.
    /// </summary>
    public IReadOnlyList<McpServerEvent> Events => _events;

    /// <summary>
    /// Events filtered by current filter settings.
    /// </summary>
    public IEnumerable<McpServerEvent> FilteredEvents => FilterViewModel.FilterEvents(_events);

    /// <summary>
    /// Current connection state.
    /// </summary>
    public ConnectionState ConnectionState => _eventStreamService.ConnectionState;

    /// <summary>
    /// Event raised when a new event is added (for scroll-to-bottom behavior).
    /// </summary>
    public event Action? EventAdded;

    public EventViewerViewModel(
        IEventStreamService eventStreamService,
        IApplicationStateService appState,
        IMcpServerDetailService serverDetailService,
        EventFilterViewModel filterViewModel,
        McpServerListViewModel serverListViewModel)
    {
        _eventStreamService = eventStreamService;
        _appState = appState;
        _serverDetailService = serverDetailService;
        FilterViewModel = filterViewModel;
        ServerListViewModel = serverListViewModel;

        _eventStreamService.EventReceived += HandleEventReceived;
        _eventStreamService.ConnectionStateChanged += HandleConnectionStateChanged;
        _eventStreamService.ErrorOccurred += HandleError;
        FilterViewModel.FilterChanged += HandleFilterChanged;
    }

    public override async Task OnInitializedAsync()
    {
        if (_isInitialized) return;

        await _appState.LoadAsync();

        if (!string.IsNullOrWhiteSpace(_appState.ServerUrl))
        {
            ServerUrl = _appState.ServerUrl;
        }
        else if (!string.IsNullOrWhiteSpace(ServerUrl))
        {
            _appState.ServerUrl = ServerUrl;
        }

        await FilterViewModel.InitializeAsync();

        _isInitialized = true;

        // Auto-connect if URL is available
        if (!string.IsNullOrWhiteSpace(ServerUrl))
        {
            await ConnectAsync();
        }
    }

    /// <summary>
    /// Handles URL saved from the configuration dialog.
    /// Disconnects from current server if connected, then connects with new URL.
    /// </summary>
    public async Task HandleUrlSavedAsync(string url)
    {
        // Auto-disconnect from current server before connecting to new one
        if (ConnectionState == ConnectionState.Connected)
        {
            await DisconnectAsync();
        }
        ServerUrl = url;
        await ConnectAsync();
    }

    /// <summary>
    /// Handles disconnect request from the configuration dialog.
    /// Disconnects and clears the stored URL.
    /// </summary>
    public async Task HandleDisconnectAsync()
    {
        await DisconnectAsync();
        ServerUrl = "";
        _appState.ServerUrl = null;
    }

    partial void OnServerUrlChanged(string value)
    {
        _ = SaveUrlAsync();
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        LastError = null;
        var streamUrl = BuildStreamUrl(ServerUrl);
        await _eventStreamService.StartAsync(streamUrl);
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _eventStreamService.StopAsync();
    }

    [RelayCommand]
    private void ClearEvents()
    {
        _events.Clear();
        FilterViewModel.ClearKnownServers();
        EventExpandState.CollapseAll();
        OnPropertyChanged(nameof(Events));
        OnPropertyChanged(nameof(FilteredEvents));
    }

    /// <summary>
    /// Gets a unique identifier for an event.
    /// </summary>
    public static string GetEventId(McpServerEvent evt)
    {
        return $"{evt.Timestamp.Ticks}_{evt.ServerName}_{evt.EventType}";
    }

    /// <summary>
    /// Expands all currently filtered events.
    /// </summary>
    public void ExpandAllEvents()
    {
        var eventIds = FilteredEvents.Select(GetEventId);
        EventExpandState.ExpandAll(eventIds);
    }

    /// <summary>
    /// Collapses all events.
    /// </summary>
    public void CollapseAllEvents()
    {
        EventExpandState.CollapseAll();
    }

    private static string BuildStreamUrl(string serverUrl)
    {
        var baseUrl = serverUrl.TrimEnd('/');
        return baseUrl + EventStreamPath;
    }

    private void HandleEventReceived(object? sender, McpServerEvent evt)
    {
        _events.Add(evt);
        FilterViewModel.AddKnownServer(evt.ServerName);

        // Handle ServerCreated/ServerDeleted events for filter updates
        if (evt.EventType == McpServerEventType.ServerCreated ||
            evt.EventType == McpServerEventType.ServerDeleted)
        {
            FilterViewModel.HandleServerEvent(evt);
        }

        // Forward all events to the server list for real-time updates
        ServerListViewModel.HandleEvent(evt);

        // Forward relevant events to the server detail service for real-time updates
        _serverDetailService.HandleEvent(evt);

        // Trim old events to prevent memory issues
        while (_events.Count > MaxEvents)
        {
            _events.RemoveAt(0);
        }

        OnPropertyChanged(nameof(Events));
        OnPropertyChanged(nameof(FilteredEvents));
        EventAdded?.Invoke();
    }

    private void HandleConnectionStateChanged(object? sender, ConnectionState state)
    {
        if (state == ConnectionState.Connected)
        {
            LastError = null;
            // Load filters and server list from API when connected
            _ = LoadFiltersFromApiAsync();
            _ = ServerListViewModel.LoadAsync(ServerUrl);
        }
        else if (state == ConnectionState.Disconnected)
        {
            // Clear server list when disconnected
            ServerListViewModel.Clear();
        }
        OnPropertyChanged(nameof(ConnectionState));
    }

    private async Task LoadFiltersFromApiAsync()
    {
        // Load servers and event types from API in parallel
        await Task.WhenAll(
            FilterViewModel.LoadServersFromApiAsync(ServerUrl),
            FilterViewModel.LoadEventTypesFromApiAsync(ServerUrl));
    }

    private void HandleError(object? sender, string error)
    {
        LastError = error;
    }

    private void HandleFilterChanged()
    {
        OnPropertyChanged(nameof(FilteredEvents));
    }

    private Task SaveUrlAsync()
    {
        _appState.ServerUrl = ServerUrl;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _eventStreamService.EventReceived -= HandleEventReceived;
        _eventStreamService.ConnectionStateChanged -= HandleConnectionStateChanged;
        _eventStreamService.ErrorOccurred -= HandleError;
        FilterViewModel.FilterChanged -= HandleFilterChanged;
    }
}
