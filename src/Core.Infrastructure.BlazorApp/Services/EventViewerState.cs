using Core.Application.Events;
using Core.Application.Storage;
using Core.Domain.Events;

namespace Core.Infrastructure.BlazorApp.Services;

/// <summary>
/// Manages the state of the event viewer application.
/// </summary>
public class EventViewerState : IDisposable
{
    private const string StreamUrlKey = "jasmin-webui:stream-url";
    private const int MaxEvents = 1000;
    private const string DefaultStreamUrl = "http://localhost:5000/v1/events/stream";

    private readonly IEventStreamService _eventStreamService;
    private readonly ILocalStorageService _localStorage;
    private readonly EventFilterState _filterState;
    private readonly List<McpServerEvent> _events = new();
    private string _streamUrl = DefaultStreamUrl;
    private string? _lastError;
    private bool _isInitialized;

    public IReadOnlyList<McpServerEvent> Events => _events;
    public IEnumerable<McpServerEvent> FilteredEvents => _filterState.FilterEvents(_events);
    public ConnectionState ConnectionState => _eventStreamService.ConnectionState;
    public EventFilterState FilterState => _filterState;
    public string? LastError => _lastError;

    public string StreamUrl
    {
        get => _streamUrl;
        set
        {
            if (_streamUrl != value)
            {
                _streamUrl = value;
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

        var savedUrl = await _localStorage.GetAsync<string>(StreamUrlKey);
        if (!string.IsNullOrWhiteSpace(savedUrl))
        {
            _streamUrl = savedUrl;
        }

        await _filterState.InitializeAsync();

        _isInitialized = true;
        NotifyStateChanged();
    }

    public async Task ConnectAsync()
    {
        await InitializeAsync();
        _lastError = null;
        await _eventStreamService.StartAsync(_streamUrl);
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
        await _localStorage.SetAsync(StreamUrlKey, _streamUrl);
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
