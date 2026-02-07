namespace Core.Application.McpServers;

/// <summary>
/// Service for invoking prompts on MCP servers.
/// </summary>
public interface IPromptInvocationService
{
    /// <summary>
    /// Starts a new instance of an MCP server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the MCP server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The instance ID on success, or an error message on failure.</returns>
    Task<ToolInvocationServiceResult<string>> StartInstanceAsync(
        string serverUrl,
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a prompt from an MCP server instance.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the MCP server.</param>
    /// <param name="instanceId">The instance ID to invoke the prompt on.</param>
    /// <param name="promptName">The name of the prompt to get.</param>
    /// <param name="arguments">The arguments for the prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt invocation result, or an error message on failure.</returns>
    Task<ToolInvocationServiceResult<PromptInvocationResult>> GetPromptAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        string promptName,
        Dictionary<string, string?>? arguments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops an MCP server instance.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the MCP server.</param>
    /// <param name="instanceId">The instance ID to stop.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or an error message on failure.</returns>
    Task<ToolInvocationServiceResult> StopInstanceAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        CancellationToken cancellationToken = default);
}
