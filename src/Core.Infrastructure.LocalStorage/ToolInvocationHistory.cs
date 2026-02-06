using Core.Application.Storage;

namespace Core.Infrastructure.LocalStorage;

/// <summary>
/// Storage for tool invocation history and drafts.
/// </summary>
public record ToolInvocationHistory
{
    /// <summary>
    /// History of invoked inputs per tool.
    /// Key: "{serverName}:{toolName}"
    /// </summary>
    public Dictionary<string, List<ToolHistoryEntry>> History { get; init; } = new();

    /// <summary>
    /// Unsaved draft inputs per tool.
    /// Key: "{serverName}:{toolName}"
    /// </summary>
    public Dictionary<string, Dictionary<string, object?>> Drafts { get; init; } = new();
}
