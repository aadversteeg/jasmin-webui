using System.Text.Json;
using Core.Application.Events;
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
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<EventStreamService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private DotNetObjectReference<EventStreamService>? _dotNetRef;
    private ConnectionState _connectionState = ConnectionState.Disconnected;
    private string _connectionId = Guid.NewGuid().ToString();

    /// <inheritdoc />
    public ConnectionState ConnectionState => _connectionState;

    /// <inheritdoc />
    public event EventHandler<McpServerEvent>? EventReceived;

    /// <inheritdoc />
    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    /// <inheritdoc />
    public event EventHandler<string>? ErrorOccurred;

    public EventStreamService(IJSRuntime jsRuntime, ILogger<EventStreamService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task StartAsync(string streamUrl, CancellationToken cancellationToken = default)
    {
        await StopAsync();

        SetConnectionState(ConnectionState.Connecting);

        _dotNetRef = DotNetObjectReference.Create(this);

        try
        {
            await _jsRuntime.InvokeVoidAsync("eventSourceHelper.connect", cancellationToken, streamUrl, _dotNetRef);
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
    public void OnError(string error)
    {
        _logger.LogError("EventSource error: {Error}", error);
        ErrorOccurred?.Invoke(this, error);
        SetConnectionState(ConnectionState.Error);
    }

    [JSInvokable]
    public void OnEventReceived(string data)
    {
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
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    public void Dispose()
    {
        _ = DisposeAsync();
    }
}
