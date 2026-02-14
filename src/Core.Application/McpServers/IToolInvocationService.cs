namespace Core.Application.McpServers;

/// <summary>
/// Service for invoking tools on MCP servers.
/// </summary>
public interface IToolInvocationService
{
    /// <summary>
    /// Starts a new instance of an MCP server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the MCP server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The instance ID on success, stderr lines and error message on failure.</returns>
    Task<StartInstanceResult> StartInstanceAsync(
        string serverUrl,
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a tool on an MCP server instance.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the MCP server.</param>
    /// <param name="instanceId">The instance ID to invoke the tool on.</param>
    /// <param name="toolName">The name of the tool to invoke.</param>
    /// <param name="input">The input arguments for the tool.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tool invocation result, or an error message on failure.</returns>
    Task<ToolInvocationServiceResult<ToolInvocationResult>> InvokeToolAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        string toolName,
        Dictionary<string, object?>? input,
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

    /// <summary>
    /// Gets the list of running instances for an MCP server.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the MCP server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of running instances, or an error message on failure.</returns>
    Task<ToolInvocationServiceResult<IReadOnlyList<McpServerInstance>>> GetInstancesAsync(
        string serverUrl,
        string serverName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes metadata (tools, prompts, resources) for an MCP server instance.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the MCP server.</param>
    /// <param name="instanceId">The instance ID to refresh metadata from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or an error message on failure.</returns>
    Task<ToolInvocationServiceResult> RefreshMetadataAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests an MCP server configuration without persisting it.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="env">The environment variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing stderr lines (always) and an error message (on failure).</returns>
    Task<TestConfigurationResult> TestConfigurationAsync(
        string serverUrl,
        string command,
        IReadOnlyList<string>? args,
        IReadOnlyDictionary<string, string>? env,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a tool invocation service operation (no value).
/// </summary>
public class ToolInvocationServiceResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected ToolInvocationServiceResult(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static ToolInvocationServiceResult Success() => new(true, null);
    public static ToolInvocationServiceResult Failure(string error) => new(false, error);
}

/// <summary>
/// Result of a tool invocation service operation with a value.
/// </summary>
public class ToolInvocationServiceResult<T> : ToolInvocationServiceResult
{
    public T? Value { get; }

    private ToolInvocationServiceResult(bool isSuccess, T? value, string? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static ToolInvocationServiceResult<T> Success(T value) => new(true, value, null);
    public new static ToolInvocationServiceResult<T> Failure(string error) => new(false, default, error);
}

/// <summary>
/// Result of a test-configuration operation. Always carries stderr lines (even on failure).
/// </summary>
public record TestConfigurationResult(
    IReadOnlyList<string> StderrLines,
    string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage == null;
}

/// <summary>
/// Result of a start-instance operation. Carries the instance ID on success,
/// or stderr lines and an error message on failure.
/// </summary>
public record StartInstanceResult(
    string? InstanceId,
    IReadOnlyList<string> StderrLines,
    string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage == null;
}
