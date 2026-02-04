using Core.Domain.Events;

namespace Core.Application.McpServers;

/// <summary>
/// Service for managing the MCP server list with real-time updates.
/// </summary>
public interface IMcpServerListService
{
    /// <summary>
    /// Gets the current list of MCP servers.
    /// </summary>
    IReadOnlyList<McpServerListItem> Servers { get; }

    /// <summary>
    /// Raised when the server list changes.
    /// </summary>
    event Action? ServersChanged;

    /// <summary>
    /// Loads the server list from the API.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    Task LoadAsync(string serverUrl);

    /// <summary>
    /// Clears the server list.
    /// </summary>
    void Clear();

    /// <summary>
    /// Handles an SSE event to update the server list.
    /// </summary>
    /// <param name="evt">The MCP server event.</param>
    void HandleEvent(McpServerEvent evt);

    /// <summary>
    /// Deletes an MCP server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the server to delete.</param>
    Task DeleteAsync(string serverUrl, string serverName);
}
