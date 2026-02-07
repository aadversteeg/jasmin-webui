namespace Core.Application.McpServers;

/// <summary>
/// Defines how MCP server instances are managed during tool/prompt invocation.
/// </summary>
public enum InstanceLifecycleMode
{
    /// <summary>
    /// Start a new instance for each invocation, stop it immediately after.
    /// </summary>
    PerInvocation = 0,

    /// <summary>
    /// Start a new instance when first needed, keep it running while dialog is open, stop on close.
    /// </summary>
    PerDialog = 1,

    /// <summary>
    /// Start a new instance when first needed, keep it running even after dialog closes.
    /// </summary>
    Persistent = 2,

    /// <summary>
    /// Reuse an existing running instance (SelectedInstanceId specifies which one).
    /// </summary>
    ExistingInstance = 3
}
