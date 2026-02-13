namespace Core.Domain.Events;

/// <summary>
/// Bidirectional mapping between dot-separated API event type names and McpServerEventType enum values.
/// </summary>
public static class McpServerEventTypeMap
{
    private static readonly Dictionary<string, McpServerEventType> _fromApi = new()
    {
        ["mcp-server.instance.starting"] = McpServerEventType.Starting,
        ["mcp-server.instance.started"] = McpServerEventType.Started,
        ["mcp-server.instance.start-failed"] = McpServerEventType.StartFailed,
        ["mcp-server.instance.stopping"] = McpServerEventType.Stopping,
        ["mcp-server.instance.stopped"] = McpServerEventType.Stopped,
        ["mcp-server.instance.stop-failed"] = McpServerEventType.StopFailed,
        ["mcp-server.configuration.created"] = McpServerEventType.ConfigurationCreated,
        ["mcp-server.configuration.updated"] = McpServerEventType.ConfigurationUpdated,
        ["mcp-server.configuration.deleted"] = McpServerEventType.ConfigurationDeleted,
        ["mcp-server.metadata.tools.retrieving"] = McpServerEventType.ToolsRetrieving,
        ["mcp-server.metadata.tools.retrieved"] = McpServerEventType.ToolsRetrieved,
        ["mcp-server.metadata.tools.retrieval-failed"] = McpServerEventType.ToolsRetrievalFailed,
        ["mcp-server.metadata.prompts.retrieving"] = McpServerEventType.PromptsRetrieving,
        ["mcp-server.metadata.prompts.retrieved"] = McpServerEventType.PromptsRetrieved,
        ["mcp-server.metadata.prompts.retrieval-failed"] = McpServerEventType.PromptsRetrievalFailed,
        ["mcp-server.metadata.resources.retrieving"] = McpServerEventType.ResourcesRetrieving,
        ["mcp-server.metadata.resources.retrieved"] = McpServerEventType.ResourcesRetrieved,
        ["mcp-server.metadata.resources.retrieval-failed"] = McpServerEventType.ResourcesRetrievalFailed,
        ["mcp-server.tool-invocation.accepted"] = McpServerEventType.ToolInvocationAccepted,
        ["mcp-server.tool-invocation.invoking"] = McpServerEventType.ToolInvoking,
        ["mcp-server.tool-invocation.invoked"] = McpServerEventType.ToolInvoked,
        ["mcp-server.tool-invocation.failed"] = McpServerEventType.ToolInvocationFailed,
        ["mcp-server.created"] = McpServerEventType.ServerCreated,
        ["mcp-server.deleted"] = McpServerEventType.ServerDeleted,
    };

    private static readonly Dictionary<McpServerEventType, string> _toApi =
        _fromApi.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    /// <summary>
    /// All known API event type names.
    /// </summary>
    public static IReadOnlyCollection<string> AllApiNames => _fromApi.Keys;

    /// <summary>
    /// Maps an API event type name to the corresponding enum value.
    /// </summary>
    public static McpServerEventType FromApiName(string apiName)
    {
        if (_fromApi.TryGetValue(apiName, out var type))
            return type;
        throw new ArgumentException($"Unknown API event type: {apiName}", nameof(apiName));
    }

    /// <summary>
    /// Tries to map an API event type name to the corresponding enum value.
    /// </summary>
    public static bool TryFromApiName(string apiName, out McpServerEventType type)
    {
        return _fromApi.TryGetValue(apiName, out type);
    }

    /// <summary>
    /// Maps an enum value to the corresponding API event type name.
    /// </summary>
    public static string ToApiName(McpServerEventType type)
    {
        if (_toApi.TryGetValue(type, out var name))
            return name;
        throw new ArgumentException($"Unknown event type: {type}", nameof(type));
    }
}
