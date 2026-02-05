using CommunityToolkit.Mvvm.ComponentModel;
using Core.Application.McpServers;
using Core.Application.Storage;
using Core.Infrastructure.BlazorApp.Components;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for the MCP server detail page.
/// </summary>
public partial class ServerDetailViewModel : NavigableViewModelBase, IDisposable
{
    private readonly IMcpServerDetailService _detailService;
    private readonly IApplicationStateService _appState;

    [ObservableProperty]
    private string _serverName = string.Empty;

    [ObservableProperty]
    private string _activeTab = "configuration";

    [ObservableProperty]
    private bool _hasTools;

    [ObservableProperty]
    private bool _hasPrompts;

    [ObservableProperty]
    private bool _hasResources;

    [ObservableProperty]
    private McpServerConfiguration? _configuration;

    [ObservableProperty]
    private McpServerMetadataResult<McpServerTool>? _toolsResult;

    [ObservableProperty]
    private McpServerMetadataResult<McpServerPrompt>? _promptsResult;

    [ObservableProperty]
    private McpServerMetadataResult<McpServerResource>? _resourcesResult;

    public ServerDetailViewModel(
        IMcpServerDetailService detailService,
        IApplicationStateService appState)
    {
        _detailService = detailService;
        _appState = appState;
        _detailService.DataChanged += HandleDataChanged;
    }

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
    protected override async Task LoadDataAsync()
    {
        await _appState.LoadAsync();

        var serverUrl = _appState.ServerUrl;
        if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(ServerName))
        {
            return;
        }

        // Load all data in parallel
        var configTask = _detailService.GetConfigurationAsync(serverUrl, ServerName);
        var toolsTask = _detailService.GetToolsAsync(serverUrl, ServerName);
        var promptsTask = _detailService.GetPromptsAsync(serverUrl, ServerName);
        var resourcesTask = _detailService.GetResourcesAsync(serverUrl, ServerName);

        await Task.WhenAll(configTask, toolsTask, promptsTask, resourcesTask);

        Configuration = await configTask;
        ToolsResult = await toolsTask;
        PromptsResult = await promptsTask;
        ResourcesResult = await resourcesTask;

        // Update tab visibility based on data availability
        HasTools = ToolsResult.Items.Count > 0;
        HasPrompts = PromptsResult.Items.Count > 0;
        HasResources = ResourcesResult.Items.Count > 0;

        // Notify that Tabs property changed (since it depends on HasX properties)
        OnPropertyChanged(nameof(Tabs));
    }

    private void HandleDataChanged(string serverName)
    {
        if (serverName == ServerName)
        {
            // Reload data when SSE event indicates changes
            _ = InitializeDataAsync();
        }
    }

    public void Dispose()
    {
        _detailService.DataChanged -= HandleDataChanged;
    }
}
