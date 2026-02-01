namespace Core.Application.McpServers;

/// <summary>
/// Represents information about an event type from the jasmin-server API.
/// </summary>
public record EventTypeInfo(string Name, int Value, string Category, string Description);
