namespace Core.Application.McpServers;

/// <summary>
/// Service for interacting with the jasmin-server REST API.
/// </summary>
public interface IJasminApiService
{
    /// <summary>
    /// Gets the list of MCP servers from the jasmin-server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <returns>A list of MCP server information.</returns>
    Task<IReadOnlyList<McpServerInfo>> GetMcpServersAsync(string serverUrl);

    /// <summary>
    /// Gets the list of event types from the jasmin-server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <returns>A list of event type information.</returns>
    Task<IReadOnlyList<EventTypeInfo>> GetEventTypesAsync(string serverUrl);
}
