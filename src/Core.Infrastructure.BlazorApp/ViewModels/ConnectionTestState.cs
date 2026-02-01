namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// Represents the state of a connection test in the configuration dialog.
/// </summary>
public enum ConnectionTestState
{
    /// <summary>
    /// No test has been performed yet.
    /// </summary>
    None,

    /// <summary>
    /// A connection test is in progress.
    /// </summary>
    Testing,

    /// <summary>
    /// The connection test succeeded.
    /// </summary>
    Success,

    /// <summary>
    /// The connection test failed.
    /// </summary>
    Failed
}
