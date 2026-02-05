using Microsoft.AspNetCore.Components;

namespace Core.Infrastructure.BlazorApp.Services;

/// <summary>
/// Service for managing navigation and page context.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly NavigationManager _navigationManager;
    private SidePanelContentType _currentSidePanelContent = SidePanelContentType.EventFilters;
    private string _currentPageTitle = "Jasmin Event Viewer";

    public NavigationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    /// <inheritdoc />
    public void NavigateToHome()
    {
        NavigateTo("/");
    }

    /// <inheritdoc />
    public void NavigateTo(string path)
    {
        _navigationManager.NavigateTo(path);
    }

    /// <inheritdoc />
    public SidePanelContentType CurrentSidePanelContent => _currentSidePanelContent;

    /// <inheritdoc />
    public string CurrentPageTitle => _currentPageTitle;

    /// <inheritdoc />
    public void SetPageContext(SidePanelContentType sidePanelContent, string pageTitle)
    {
        var changed = _currentSidePanelContent != sidePanelContent || _currentPageTitle != pageTitle;
        _currentSidePanelContent = sidePanelContent;
        _currentPageTitle = pageTitle;

        if (changed)
        {
            PageContextChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public event Action? PageContextChanged;
}
