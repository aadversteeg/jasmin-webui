using Core.Application.McpServers;

namespace Core.Application.Storage;

/// <summary>
/// A single history entry for a prompt invocation.
/// </summary>
public record PromptHistoryEntry
{
    /// <summary>
    /// The argument values that were used for the invocation.
    /// </summary>
    public Dictionary<string, string?> ArgumentValues { get; init; } = new();

    /// <summary>
    /// The output from the invocation.
    /// </summary>
    public PromptInvocationResult? Output { get; init; }

    /// <summary>
    /// When the invocation occurred.
    /// </summary>
    public DateTime InvokedAt { get; init; }
}

/// <summary>
/// Service for managing prompt invocation history and drafts.
/// </summary>
public interface IPromptHistoryService
{
    /// <summary>
    /// Loads history and drafts from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Gets whether the service has been loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the maximum number of history entries per prompt.
    /// </summary>
    int MaxHistoryItems { get; set; }

    // History (invoked arguments)

    /// <summary>
    /// Gets the history for a specific prompt.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="promptName">The prompt name.</param>
    /// <returns>A list of history entries, oldest first.</returns>
    IReadOnlyList<PromptHistoryEntry> GetHistory(string serverName, string promptName);

    /// <summary>
    /// Adds a history entry for a prompt invocation.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="promptName">The prompt name.</param>
    /// <param name="argumentValues">The argument values used.</param>
    /// <param name="output">The output from the invocation.</param>
    Task AddEntryAsync(string serverName, string promptName, Dictionary<string, string?> argumentValues, PromptInvocationResult? output);

    /// <summary>
    /// Clears all history for a specific prompt.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="promptName">The prompt name.</param>
    Task ClearHistoryAsync(string serverName, string promptName);

    // Drafts (unsaved arguments)

    /// <summary>
    /// Gets the draft for a specific prompt.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="promptName">The prompt name.</param>
    /// <returns>The draft argument values, or null if no draft exists.</returns>
    Dictionary<string, string?>? GetDraft(string serverName, string promptName);

    /// <summary>
    /// Saves the draft for a specific prompt.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="promptName">The prompt name.</param>
    /// <param name="argumentValues">The argument values to save.</param>
    Task SaveDraftAsync(string serverName, string promptName, Dictionary<string, string?> argumentValues);

    /// <summary>
    /// Clears the draft for a specific prompt.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="promptName">The prompt name.</param>
    Task ClearDraftAsync(string serverName, string promptName);
}
