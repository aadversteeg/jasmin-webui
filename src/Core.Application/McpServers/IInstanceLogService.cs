using Core.Application.Events;

namespace Core.Application.McpServers;

/// <summary>
/// Service for streaming stderr logs from MCP server instances via SSE.
/// </summary>
public interface IInstanceLogService
{
    /// <summary>
    /// Starts streaming logs for the specified instance.
    /// Uses SSE with afterLine=0 for full catch-up plus live tail.
    /// </summary>
    Task StartStreamAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        long afterLine = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the current log stream.
    /// </summary>
    Task StopStreamAsync();

    /// <summary>
    /// Event raised when a new log entry is received from the stream.
    /// </summary>
    event EventHandler<InstanceLogEntry>? LogEntryReceived;

    /// <summary>
    /// Event raised when a stream error occurs.
    /// </summary>
    event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Gets the current connection state of the log stream.
    /// </summary>
    ConnectionState ConnectionState { get; }
}
