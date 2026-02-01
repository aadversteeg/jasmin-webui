using Core.Domain.Events;

namespace Core.Application.Events;

/// <summary>
/// Service for streaming events from jasmin-server.
/// </summary>
public interface IEventStreamService
{
    /// <summary>
    /// Starts streaming events from the specified endpoint.
    /// </summary>
    Task StartAsync(string streamUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the current event stream.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    ConnectionState ConnectionState { get; }

    /// <summary>
    /// Event raised when a new event is received.
    /// </summary>
    event EventHandler<McpServerEvent>? EventReceived;

    /// <summary>
    /// Event raised when connection state changes.
    /// </summary>
    event EventHandler<ConnectionState>? ConnectionStateChanged;

    /// <summary>
    /// Event raised when an error occurs.
    /// </summary>
    event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Tests the connection to the specified server URL.
    /// </summary>
    /// <param name="serverUrl">The base server URL to test.</param>
    /// <returns>A tuple indicating success and an optional error message.</returns>
    Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(string serverUrl);
}
