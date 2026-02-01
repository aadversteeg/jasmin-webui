using System.ComponentModel;
using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.Events;
using Core.Application.Storage;
using Core.Domain.Events;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for the main event viewer page.
/// </summary>
public partial class EventViewerViewModel : ViewModelBase, IDisposable
{
    private const string ServerUrlKey = "jasmin-webui:server-url";
    private const string EventStreamPath = "/v1/events/stream";
    private const int MaxEvents = 1000;
    private const string DefaultServerUrl = "http://localhost:5000";

    private readonly IEventStreamService _eventStreamService;
    private readonly ILocalStorageService _localStorage;
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
        ILocalStorageService localStorage,
        EventFilterViewModel filterViewModel)
    {
        _eventStreamService = eventStreamService;
        _localStorage = localStorage;
        FilterViewModel = filterViewModel;

        _eventStreamService.EventReceived += HandleEventReceived;
        _eventStreamService.ConnectionStateChanged += HandleConnectionStateChanged;
        _eventStreamService.ErrorOccurred += HandleError;
        FilterViewModel.FilterChanged += HandleFilterChanged;
    }

    public override async Task OnInitializedAsync()
    {
        if (_isInitialized) return;

        var savedUrl = await _localStorage.GetAsync<string>(ServerUrlKey);
        if (!string.IsNullOrWhiteSpace(savedUrl))
        {
            ServerUrl = savedUrl;
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
        await _localStorage.RemoveAsync(ServerUrlKey);
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
        OnPropertyChanged(nameof(Events));
        OnPropertyChanged(nameof(FilteredEvents));
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
        }
        OnPropertyChanged(nameof(ConnectionState));
    }

    private void HandleError(object? sender, string error)
    {
        LastError = error;
    }

    private void HandleFilterChanged()
    {
        OnPropertyChanged(nameof(FilteredEvents));
    }

    private async Task SaveUrlAsync()
    {
        await _localStorage.SetAsync(ServerUrlKey, ServerUrl);
    }

    public void Dispose()
    {
        _eventStreamService.EventReceived -= HandleEventReceived;
        _eventStreamService.ConnectionStateChanged -= HandleConnectionStateChanged;
        _eventStreamService.ErrorOccurred -= HandleError;
        FilterViewModel.FilterChanged -= HandleFilterChanged;
    }
}
