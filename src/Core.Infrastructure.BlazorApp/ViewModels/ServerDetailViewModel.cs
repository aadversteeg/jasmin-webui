using CommunityToolkit.Mvvm.ComponentModel;
using Core.Infrastructure.BlazorApp.Components;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for the MCP server detail page.
/// </summary>
public partial class ServerDetailViewModel : NavigableViewModelBase
{
    [ObservableProperty]
    private string _serverName = string.Empty;

    [ObservableProperty]
    private string _activeTab = "configuration";

    [ObservableProperty]
    private bool _hasTools = true; // Placeholder: will be set based on actual data

    [ObservableProperty]
    private bool _hasPrompts = true; // Placeholder: will be set based on actual data

    [ObservableProperty]
    private bool _hasResources = true; // Placeholder: will be set based on actual data

    public IReadOnlyList<TabItem> Tabs => new List<TabItem>
    {
        new("configuration", "Configuration", $"/mcp-servers/{ServerName}/configuration", true),
        new("tools", "Tools", $"/mcp-servers/{ServerName}/tools", HasTools),
        new("prompts", "Prompts", $"/mcp-servers/{ServerName}/prompts", HasPrompts),
        new("resources", "Resources", $"/mcp-servers/{ServerName}/resources", HasResources)
    };

    public void SetActiveTab(string? tabName)
    {
        ActiveTab = string.IsNullOrEmpty(tabName) ? "configuration" : tabName.ToLowerInvariant();
    }

    /// <inheritdoc />
    protected override Task LoadDataAsync()
    {
        // Placeholder: will load server details and tools from API in the future
        return Task.CompletedTask;
    }
}
