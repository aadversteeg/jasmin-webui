namespace Core.Domain.Events;

/// <summary>
/// Types of events from MCP server lifecycle.
/// </summary>
public enum McpServerEventType
{
    Starting = 0,
    Started = 1,
    StartFailed = 2,
    Stopping = 3,
    Stopped = 4,
    StopFailed = 5,
    ConfigurationCreated = 6,
    ConfigurationUpdated = 7,
    ConfigurationDeleted = 8,
    ToolsRetrieving = 9,
    ToolsRetrieved = 10,
    ToolsRetrievalFailed = 11,
    PromptsRetrieving = 12,
    PromptsRetrieved = 13,
    PromptsRetrievalFailed = 14,
    ResourcesRetrieving = 15,
    ResourcesRetrieved = 16,
    ResourcesRetrievalFailed = 17,
    ToolInvocationAccepted = 18,
    ToolInvoking = 19,
    ToolInvoked = 20,
    ToolInvocationFailed = 21,
    ServerCreated = 22,
    ServerDeleted = 23
}
