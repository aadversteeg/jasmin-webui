using Core.Application.Events;
using Core.Application.Storage;
using Core.Domain.Events;

namespace Core.Infrastructure.BlazorApp.Services;

/// <summary>
/// Manages the state of the event viewer application.
/// </summary>
public class EventViewerState : IDisposable
{
    private const string ServerUrlKey = "jasmin-webui:server-url";
    private const string EventStreamPath = "/v1/events/stream";
    private const int MaxEvents = 1000;
    private const string DefaultServerUrl = "http://localhost:5000";

    private readonly IEventStreamService _eventStreamService;
    private readonly ILocalStorageService _localStorage;
    private readonly EventFilterState _filterState;
    private readonly List<McpServerEvent> _events = new();
    private string _serverUrl = DefaultServerUrl;
    private string? _lastError;
    private bool _isInitialized;

    public IReadOnlyList<McpServerEvent> Events => _events;
    public IEnumerable<McpServerEvent> FilteredEvents => _filterState.FilterEvents(_events);
    public ConnectionState ConnectionState => _eventStreamService.ConnectionState;
    public EventFilterState FilterState => _filterState;
    public string? LastError => _lastError;

    public string ServerUrl
    {
        get => _serverUrl;
        set
        {
            if (_serverUrl != value)
            {
                _serverUrl = value;
                _ = SaveUrlAsync();
                NotifyStateChanged();
            }
        }
    }

    public event Action? OnChange;
    public event Action? OnEventAdded;

    public EventViewerState(
        IEventStreamService eventStreamService,
        ILocalStorageService localStorage,
        EventFilterState filterState)
    {
        _eventStreamService = eventStreamService;
        _localStorage = localStorage;
        _filterState = filterState;

        _eventStreamService.EventReceived += HandleEventReceived;
        _eventStreamService.ConnectionStateChanged += HandleConnectionStateChanged;
        _eventStreamService.ErrorOccurred += HandleError;
        _filterState.OnChange += HandleFilterChanged;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        var savedUrl = await _localStorage.GetAsync<string>(ServerUrlKey);
        if (!string.IsNullOrWhiteSpace(savedUrl))
        {
            _serverUrl = savedUrl;
        }

        await _filterState.InitializeAsync();

        _isInitialized = true;
        NotifyStateChanged();
    }

    public async Task ConnectAsync()
    {
        await InitializeAsync();
        _lastError = null;
        var streamUrl = BuildStreamUrl(_serverUrl);
        await _eventStreamService.StartAsync(streamUrl);
    }

    private static string BuildStreamUrl(string serverUrl)
    {
        var baseUrl = serverUrl.TrimEnd('/');
        return baseUrl + EventStreamPath;
    }

    public async Task DisconnectAsync()
    {
        await _eventStreamService.StopAsync();
    }

    public void ClearEvents()
    {
        _events.Clear();
        _filterState.ClearKnownServers();
        NotifyStateChanged();
    }

    private void HandleEventReceived(object? sender, McpServerEvent evt)
    {
        _events.Add(evt);
        _filterState.AddKnownServer(evt.ServerName);

        // Trim old events to prevent memory issues
        while (_events.Count > MaxEvents)
        {
            _events.RemoveAt(0);
        }

        OnEventAdded?.Invoke();
        NotifyStateChanged();
    }

    private void HandleConnectionStateChanged(object? sender, ConnectionState state)
    {
        if (state == ConnectionState.Connected)
        {
            _lastError = null;
        }
        NotifyStateChanged();
    }

    private void HandleError(object? sender, string error)
    {
        _lastError = error;
        NotifyStateChanged();
    }

    private void HandleFilterChanged()
    {
        NotifyStateChanged();
    }

    private async Task SaveUrlAsync()
    {
        await _localStorage.SetAsync(ServerUrlKey, _serverUrl);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void Dispose()
    {
        _eventStreamService.EventReceived -= HandleEventReceived;
        _eventStreamService.ConnectionStateChanged -= HandleConnectionStateChanged;
        _eventStreamService.ErrorOccurred -= HandleError;
        _filterState.OnChange -= HandleFilterChanged;
    }
}
