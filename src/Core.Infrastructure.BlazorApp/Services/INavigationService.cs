namespace Core.Infrastructure.BlazorApp.Services;

/// <summary>
/// Service for managing navigation and page context.
/// Wraps Blazor's NavigationManager in an MVVM-friendly abstraction.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the home page.
    /// </summary>
    void NavigateToHome();

    /// <summary>
    /// Navigates to the specified path.
    /// </summary>
    /// <param name="path">The path to navigate to (e.g., "/mcp-servers/chronos").</param>
    void NavigateTo(string path);

    /// <summary>
    /// Gets the current side panel content type.
    /// </summary>
    SidePanelContentType CurrentSidePanelContent { get; }

    /// <summary>
    /// Gets the current page title.
    /// </summary>
    string CurrentPageTitle { get; }

    /// <summary>
    /// Sets the page context for the current page. Called by pages on initialization
    /// to inform the layout what side panel content and title to display.
    /// </summary>
    /// <param name="sidePanelContent">The side panel content type for this page.</param>
    /// <param name="pageTitle">The page title.</param>
    void SetPageContext(SidePanelContentType sidePanelContent, string pageTitle);

    /// <summary>
    /// Raised when the page context changes.
    /// </summary>
    event Action? PageContextChanged;
}
