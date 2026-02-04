using System.Text.Json;
using Core.Application.Events;
using Core.Application.Storage;
using Core.Domain.Events;
using Core.Infrastructure.JasminClient.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Service for consuming SSE events from jasmin-server using browser's EventSource API.
/// </summary>
public class EventStreamService : IEventStreamService, IAsyncDisposable
{
    private const string EventStreamPath = "/v1/events/stream";

    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private readonly IUserPreferencesService _preferences;
    private readonly ILogger<EventStreamService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private DotNetObjectReference<EventStreamService>? _dotNetRef;
    private ConnectionState _connectionState = ConnectionState.Disconnected;
    private string _connectionId = Guid.NewGuid().ToString();
    private string? _lastEventId;
    private bool _lastEventIdLoaded;

    /// <inheritdoc />
    public ConnectionState ConnectionState => _connectionState;

    /// <inheritdoc />
    public string? LastEventId => _lastEventId;

    /// <inheritdoc />
    public event EventHandler<McpServerEvent>? EventReceived;

    /// <inheritdoc />
    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    /// <inheritdoc />
    public event EventHandler<string>? ErrorOccurred;

    public EventStreamService(
        IJSRuntime jsRuntime,
        HttpClient httpClient,
        IUserPreferencesService preferences,
        ILogger<EventStreamService> logger)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
        _preferences = preferences;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task StartAsync(string streamUrl, string? lastEventId = null, CancellationToken cancellationToken = default)
    {
        await StopAsync();

        SetConnectionState(ConnectionState.Connecting);

        _dotNetRef = DotNetObjectReference.Create(this);

        try
        {
            // Load lastEventId from preferences if not already loaded
            if (!_lastEventIdLoaded)
            {
                await _preferences.LoadAsync();
                _lastEventId = _preferences.LastEventId;
                _lastEventIdLoaded = true;
                if (!string.IsNullOrEmpty(_lastEventId))
                {
                    _logger.LogInformation("Loaded last event ID from storage: {LastEventId}", _lastEventId);
                }
            }

            // Use provided lastEventId, or fall back to stored value for reconnection
            var eventIdToUse = lastEventId ?? _lastEventId;
            if (!string.IsNullOrEmpty(eventIdToUse))
            {
                _logger.LogInformation("Reconnecting with last event ID: {LastEventId}", eventIdToUse);
            }
            await _jsRuntime.InvokeVoidAsync("eventSourceHelper.connect", cancellationToken, streamUrl, _dotNetRef, eventIdToUse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start EventSource connection to {Url}", streamUrl);
            ErrorOccurred?.Invoke(this, $"Failed to connect: {ex.Message}");
            SetConnectionState(ConnectionState.Error);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (_dotNetRef != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("eventSourceHelper.disconnect", _connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting EventSource");
            }

            _dotNetRef.Dispose();
            _dotNetRef = null;
        }

        SetConnectionState(ConnectionState.Disconnected);
    }

    [JSInvokable]
    public void OnConnected()
    {
        _logger.LogInformation("EventSource connected");
        SetConnectionState(ConnectionState.Connected);
    }

    [JSInvokable]
    public void OnDisconnected()
    {
        _logger.LogInformation("EventSource disconnected");
        SetConnectionState(ConnectionState.Disconnected);
    }

    [JSInvokable]
    public void OnReconnecting()
    {
        _logger.LogInformation("EventSource reconnecting");
        SetConnectionState(ConnectionState.Reconnecting);
    }

    [JSInvokable]
    public void OnError(string error)
    {
        _logger.LogError("EventSource error: {Error}", error);
        ErrorOccurred?.Invoke(this, error);
        SetConnectionState(ConnectionState.Error);
    }

    [JSInvokable]
    public void OnEventReceived(string data, string eventId)
    {
        // Track the last event ID for reconnection
        if (!string.IsNullOrEmpty(eventId) && eventId != _lastEventId)
        {
            _lastEventId = eventId;
            // Save to preferences (fire and forget)
            _preferences.LastEventId = eventId;
        }

        try
        {
            var eventDto = JsonSerializer.Deserialize<EventResponseDto>(data, _jsonOptions);
            if (eventDto != null)
            {
                var domainEvent = EventMapper.ToDomain(eventDto, data);
                EventReceived?.Invoke(this, domainEvent);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse event: {Data}", data);
        }
    }

    private void SetConnectionState(ConnectionState state)
    {
        if (_connectionState != state)
        {
            _connectionState = state;
            ConnectionStateChanged?.Invoke(this, state);
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(string serverUrl)
    {
        try
        {
            var streamUrl = BuildStreamUrl(serverUrl);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var request = new HttpRequestMessage(HttpMethod.Get, streamUrl);
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            return (false, $"Server returned {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (TaskCanceledException)
        {
            return (false, "Connection timed out");
        }
        catch (HttpRequestException ex)
        {
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection to {Url}", serverUrl);
            return (false, ex.Message);
        }
    }

    private static string BuildStreamUrl(string serverUrl)
    {
        var baseUrl = serverUrl.TrimEnd('/');
        return baseUrl + EventStreamPath;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    public void Dispose()
    {
        _ = DisposeAsync();
    }
}
