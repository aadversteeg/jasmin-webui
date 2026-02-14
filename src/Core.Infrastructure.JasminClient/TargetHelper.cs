namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Helper for building and parsing target URI strings used by the jasmin-server API.
/// </summary>
public static class TargetHelper
{
    /// <summary>
    /// Builds a target URI for a server (e.g., "mcp-servers/my-server").
    /// </summary>
    public static string BuildServerTarget(string serverName)
        => $"mcp-servers/{serverName}";

    /// <summary>
    /// Builds a target URI for an instance (e.g., "mcp-servers/my-server/instances/abc-123").
    /// </summary>
    public static string BuildInstanceTarget(string serverName, string instanceId)
        => $"mcp-servers/{serverName}/instances/{instanceId}";

    /// <summary>
    /// Parses a target URI to extract the server name and optional instance ID.
    /// </summary>
    public static (string? ServerName, string? InstanceId) ParseTarget(string? target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return (null, null);
        }

        var segments = target.Split('/');
        string? serverName = segments.Length >= 2 ? segments[1] : null;
        string? instanceId = segments.Length >= 4 ? segments[3] : null;
        return (serverName, instanceId);
    }
}
