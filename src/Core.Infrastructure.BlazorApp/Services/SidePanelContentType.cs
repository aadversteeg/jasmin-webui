namespace Core.Infrastructure.BlazorApp.Services;

/// <summary>
/// Identifies the type of content to display in the right side panel.
/// </summary>
public enum SidePanelContentType
{
    /// <summary>No right panel content.</summary>
    None,

    /// <summary>Event filter panel (home page).</summary>
    EventFilters,

    /// <summary>Server detail panel.</summary>
    ServerDetail
}
