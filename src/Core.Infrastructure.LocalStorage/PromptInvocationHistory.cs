using Core.Application.Storage;

namespace Core.Infrastructure.LocalStorage;

/// <summary>
/// Storage for prompt invocation history and drafts.
/// </summary>
public record PromptInvocationHistory
{
    /// <summary>
    /// History of invoked arguments per prompt.
    /// Key: "{serverName}:{promptName}"
    /// </summary>
    public Dictionary<string, List<PromptHistoryEntry>> History { get; init; } = new();

    /// <summary>
    /// Unsaved draft arguments per prompt.
    /// Key: "{serverName}:{promptName}"
    /// </summary>
    public Dictionary<string, Dictionary<string, string?>> Drafts { get; init; } = new();
}
