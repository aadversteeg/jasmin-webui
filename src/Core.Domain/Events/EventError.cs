namespace Core.Domain.Events;

/// <summary>
/// Represents an error in an event.
/// </summary>
public record EventError(string Code, string Message);
