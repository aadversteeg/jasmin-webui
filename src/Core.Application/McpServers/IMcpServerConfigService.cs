namespace Core.Application.McpServers;

/// <summary>
/// Service for managing MCP server configurations.
/// </summary>
public interface IMcpServerConfigService
{
    /// <summary>
    /// Creates a new MCP server with the specified configuration.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="name">The name of the MCP server.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="env">The environment variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or an error message on failure.</returns>
    Task<McpServerConfigServiceResult> CreateServerAsync(
        string serverUrl,
        string name,
        string command,
        IReadOnlyList<string>? args,
        IReadOnlyDictionary<string, string>? env,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the configuration of an existing MCP server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="name">The name of the MCP server.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="env">The environment variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or an error message on failure.</returns>
    Task<McpServerConfigServiceResult> UpdateConfigurationAsync(
        string serverUrl,
        string name,
        string command,
        IReadOnlyList<string>? args,
        IReadOnlyDictionary<string, string>? env,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configuration of an MCP server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="name">The name of the MCP server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The configuration on success, or an error message on failure.</returns>
    Task<McpServerConfigServiceResult<McpServerConfiguration>> GetConfigurationAsync(
        string serverUrl,
        string name,
        CancellationToken cancellationToken = default);

}

/// <summary>
/// Result of a config service operation (no value).
/// </summary>
public class McpServerConfigServiceResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected McpServerConfigServiceResult(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static McpServerConfigServiceResult Success() => new(true, null);
    public static McpServerConfigServiceResult Failure(string error) => new(false, error);
}

/// <summary>
/// Result of a config service operation with a value.
/// </summary>
public class McpServerConfigServiceResult<T> : McpServerConfigServiceResult
{
    public T? Value { get; }

    private McpServerConfigServiceResult(bool isSuccess, T? value, string? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static McpServerConfigServiceResult<T> Success(T value) => new(true, value, null);
    public new static McpServerConfigServiceResult<T> Failure(string error) => new(false, default, error);
}
