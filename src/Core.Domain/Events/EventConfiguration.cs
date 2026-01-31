namespace Core.Domain.Events;

/// <summary>
/// Configuration data captured in an event.
/// </summary>
public record EventConfiguration(
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);
