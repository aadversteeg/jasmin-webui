using Core.Domain.Events;

namespace Core.Application.McpServers;

/// <summary>
/// Service for fetching MCP server details (configuration, tools, prompts, resources).
/// </summary>
public interface IMcpServerDetailService
{
    /// <summary>
    /// Raised when data for a specific server changes.
    /// The string parameter is the server name.
    /// </summary>
    event Action<string>? DataChanged;

    /// <summary>
    /// Gets the configuration for an MCP server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the server.</param>
    /// <returns>The server configuration, or null if not found.</returns>
    Task<McpServerConfiguration?> GetConfigurationAsync(string serverUrl, string serverName);

    /// <summary>
    /// Gets the tools for an MCP server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the server.</param>
    /// <returns>The tools list result.</returns>
    Task<McpServerMetadataResult<McpServerTool>> GetToolsAsync(string serverUrl, string serverName);

    /// <summary>
    /// Gets the prompts for an MCP server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the server.</param>
    /// <returns>The prompts list result.</returns>
    Task<McpServerMetadataResult<McpServerPrompt>> GetPromptsAsync(string serverUrl, string serverName);

    /// <summary>
    /// Gets the resources for an MCP server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the server.</param>
    /// <returns>The resources list result.</returns>
    Task<McpServerMetadataResult<McpServerResource>> GetResourcesAsync(string serverUrl, string serverName);

    /// <summary>
    /// Handles an SSE event to trigger data refresh if applicable.
    /// </summary>
    /// <param name="evt">The MCP server event.</param>
    void HandleEvent(McpServerEvent evt);
}

/// <summary>
/// Result of fetching metadata (tools, prompts, resources) from an MCP server.
/// </summary>
public record McpServerMetadataResult<T>(
    IReadOnlyList<T> Items,
    DateTimeOffset? RetrievedAt,
    IReadOnlyList<McpServerMetadataError>? Errors);

/// <summary>
/// Represents an error that occurred during metadata retrieval.
/// </summary>
public record McpServerMetadataError(string Code, string Message);
