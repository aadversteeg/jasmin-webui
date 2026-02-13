using System.Text.Json;
using Core.Application.Events;
using Core.Application.McpServers;
using Core.Infrastructure.JasminClient.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Service for streaming stderr logs from MCP server instances via SSE.
/// Uses the browser's EventSource API through JavaScript interop.
/// </summary>
public class InstanceLogService : IInstanceLogService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<InstanceLogService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private DotNetObjectReference<InstanceLogService>? _dotNetRef;
    private int _connectionId;
    private ConnectionState _connectionState = ConnectionState.Disconnected;

    /// <inheritdoc />
    public ConnectionState ConnectionState => _connectionState;

    /// <inheritdoc />
    public event EventHandler<InstanceLogEntry>? LogEntryReceived;

    /// <inheritdoc />
    public event EventHandler<string>? ErrorOccurred;

    public InstanceLogService(IJSRuntime jsRuntime, ILogger<InstanceLogService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task StartStreamAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        long afterLine = 0,
        CancellationToken cancellationToken = default)
    {
        await StopStreamAsync();

        SetConnectionState(ConnectionState.Connecting);

        _dotNetRef = DotNetObjectReference.Create(this);

        try
        {
            var url = $"{serverUrl.TrimEnd('/')}/v1/mcp-servers/{Uri.EscapeDataString(serverName)}/instances/{Uri.EscapeDataString(instanceId)}/logs/stream?afterLine={afterLine}";

            _logger.LogInformation("Starting instance log stream: {Url}", url);

            _connectionId = await _jsRuntime.InvokeAsync<int>(
                "eventSourceHelper.connect",
                cancellationToken,
                url,
                _dotNetRef,
                null,
                "instance-log",
                "OnLogEntryReceived");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start instance log stream");
            ErrorOccurred?.Invoke(this, $"Failed to connect: {ex.Message}");
            SetConnectionState(ConnectionState.Error);
        }
    }

    /// <inheritdoc />
    public async Task StopStreamAsync()
    {
        if (_dotNetRef != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("eventSourceHelper.disconnect", _connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting instance log stream");
            }

            _dotNetRef.Dispose();
            _dotNetRef = null;
        }

        SetConnectionState(ConnectionState.Disconnected);
    }

    [JSInvokable]
    public void OnConnected()
    {
        _logger.LogInformation("Instance log stream connected");
        SetConnectionState(ConnectionState.Connected);
    }

    [JSInvokable]
    public void OnDisconnected()
    {
        _logger.LogInformation("Instance log stream disconnected");
        SetConnectionState(ConnectionState.Disconnected);
    }

    [JSInvokable]
    public void OnReconnecting()
    {
        _logger.LogInformation("Instance log stream reconnecting");
        SetConnectionState(ConnectionState.Reconnecting);
    }

    [JSInvokable]
    public void OnError(string error)
    {
        _logger.LogError("Instance log stream error: {Error}", error);
        ErrorOccurred?.Invoke(this, error);
        SetConnectionState(ConnectionState.Error);
    }

    [JSInvokable]
    public void OnLogEntryReceived(string data, string eventId)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<InstanceLogEntryDto>(data, _jsonOptions);
            if (dto != null)
            {
                var timestamp = DateTimeOffset.TryParse(dto.Timestamp, out var ts)
                    ? ts
                    : DateTimeOffset.UtcNow;

                var entry = new InstanceLogEntry(dto.LineNumber, timestamp, dto.Text);
                LogEntryReceived?.Invoke(this, entry);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse instance log entry: {Data}", data);
        }
    }

    private void SetConnectionState(ConnectionState state)
    {
        if (_connectionState != state)
        {
            _connectionState = state;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopStreamAsync();
    }
}
