using Core.Application.McpServers;

namespace Core.Application.Storage;

/// <summary>
/// A single history entry for a tool invocation.
/// </summary>
public record ToolHistoryEntry
{
    /// <summary>
    /// The input values that were used for the invocation.
    /// </summary>
    public Dictionary<string, object?> InputValues { get; init; } = new();

    /// <summary>
    /// The output from the invocation.
    /// </summary>
    public ToolInvocationResult? Output { get; init; }

    /// <summary>
    /// When the invocation occurred.
    /// </summary>
    public DateTime InvokedAt { get; init; }
}

/// <summary>
/// Service for managing tool invocation history and drafts.
/// </summary>
public interface IToolHistoryService
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
    /// Gets the maximum number of history entries per tool.
    /// </summary>
    int MaxHistoryItems { get; set; }

    // History (invoked inputs)

    /// <summary>
    /// Gets the history for a specific tool.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="toolName">The tool name.</param>
    /// <returns>A list of history entries, oldest first.</returns>
    IReadOnlyList<ToolHistoryEntry> GetHistory(string serverName, string toolName);

    /// <summary>
    /// Adds a history entry for a tool invocation.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="toolName">The tool name.</param>
    /// <param name="inputValues">The input values used.</param>
    /// <param name="output">The output from the invocation.</param>
    Task AddEntryAsync(string serverName, string toolName, Dictionary<string, object?> inputValues, ToolInvocationResult? output);

    /// <summary>
    /// Clears all history for a specific tool.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="toolName">The tool name.</param>
    Task ClearHistoryAsync(string serverName, string toolName);

    // Drafts (unsaved inputs)

    /// <summary>
    /// Gets the draft for a specific tool.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="toolName">The tool name.</param>
    /// <returns>The draft input values, or null if no draft exists.</returns>
    Dictionary<string, object?>? GetDraft(string serverName, string toolName);

    /// <summary>
    /// Saves the draft for a specific tool.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="toolName">The tool name.</param>
    /// <param name="inputValues">The input values to save.</param>
    Task SaveDraftAsync(string serverName, string toolName, Dictionary<string, object?> inputValues);

    /// <summary>
    /// Clears the draft for a specific tool.
    /// </summary>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="toolName">The tool name.</param>
    Task ClearDraftAsync(string serverName, string toolName);
}
