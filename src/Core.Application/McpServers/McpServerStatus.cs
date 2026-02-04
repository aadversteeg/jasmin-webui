namespace Core.Application.McpServers;

/// <summary>
/// Represents the connection status of an MCP server.
/// </summary>
public enum McpServerStatus
{
    /// <summary>
    /// Server status is unknown (not verified yet).
    /// </summary>
    Unknown,

    /// <summary>
    /// Server has been verified and can connect successfully.
    /// </summary>
    Verified,

    /// <summary>
    /// Server failed to start or has invalid configuration.
    /// </summary>
    Failed
}
