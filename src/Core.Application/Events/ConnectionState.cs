namespace Core.Application.Events;

/// <summary>
/// Represents the connection state to the event stream.
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Error
}
