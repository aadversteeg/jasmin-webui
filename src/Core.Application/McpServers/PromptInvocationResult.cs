namespace Core.Application.McpServers;

/// <summary>
/// Result of a prompt invocation (getPrompt).
/// </summary>
public record PromptInvocationResult(
    IReadOnlyList<PromptMessage> Messages,
    string? Description);

/// <summary>
/// A message in a prompt invocation result.
/// </summary>
public record PromptMessage(
    string Role,
    PromptMessageContent Content);

/// <summary>
/// Content of a prompt message.
/// </summary>
public record PromptMessageContent(
    string Type,
    string? Text,
    string? MimeType,
    string? Data,
    string? Uri);
