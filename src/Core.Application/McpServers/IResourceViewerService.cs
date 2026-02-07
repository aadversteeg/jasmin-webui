namespace Core.Application.McpServers;

/// <summary>
/// Service for reading resources from MCP servers.
/// </summary>
public interface IResourceViewerService
{
    /// <summary>
    /// Reads a resource from an MCP server instance.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    /// <param name="serverName">The name of the MCP server.</param>
    /// <param name="instanceId">The instance ID to read the resource from.</param>
    /// <param name="resourceUri">The URI of the resource to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resource content, or an error message on failure.</returns>
    Task<ResourceViewerServiceResult<McpResourceReadResult>> ReadResourceAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        string resourceUri,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a resource viewer service operation (no value).
/// </summary>
public class ResourceViewerServiceResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected ResourceViewerServiceResult(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static ResourceViewerServiceResult Success() => new(true, null);
    public static ResourceViewerServiceResult Failure(string error) => new(false, error);
}

/// <summary>
/// Result of a resource viewer service operation with a value.
/// </summary>
public class ResourceViewerServiceResult<T> : ResourceViewerServiceResult
{
    public T? Value { get; }

    private ResourceViewerServiceResult(bool isSuccess, T? value, string? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static ResourceViewerServiceResult<T> Success(T value) => new(true, value, null);
    public new static ResourceViewerServiceResult<T> Failure(string error) => new(false, default, error);
}
