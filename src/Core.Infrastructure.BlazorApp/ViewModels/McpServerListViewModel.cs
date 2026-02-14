using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.McpServers;
using Core.Domain.Events;

namespace Core.Infrastructure.BlazorApp.ViewModels;

public partial class McpServerListViewModel : ViewModelBase
{
    private readonly IMcpServerListService _serverListService;
    private readonly IToolInvocationService _invocationService;
    private string? _currentServerUrl;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string? _refreshingServerName;

    public McpServerListViewModel(
        IMcpServerListService serverListService,
        IToolInvocationService invocationService)
    {
        _serverListService = serverListService;
        _invocationService = invocationService;
        _serverListService.ServersChanged += OnServersChanged;
    }

    /// <summary>
    /// Gets the list of MCP servers.
    /// </summary>
    public IReadOnlyList<McpServerListItem> Servers => _serverListService.Servers;

    /// <summary>
    /// Raised when the server list changes.
    /// </summary>
    public event Action? ServersChanged;

    /// <summary>
    /// Raised when a delete confirmation is requested.
    /// </summary>
    public event Func<string, Task<bool>>? DeleteConfirmationRequested;

    /// <summary>
    /// Loads the server list from the API.
    /// </summary>
    /// <param name="serverUrl">The base URL of the jasmin-server.</param>
    public async Task LoadAsync(string serverUrl)
    {
        _currentServerUrl = serverUrl;
        await _serverListService.LoadAsync(serverUrl);
    }

    /// <summary>
    /// Clears the server list.
    /// </summary>
    public void Clear()
    {
        _currentServerUrl = null;
        _serverListService.Clear();
    }

    /// <summary>
    /// Handles an SSE event to update the server list.
    /// </summary>
    /// <param name="evt">The MCP server event.</param>
    public void HandleEvent(McpServerEvent evt)
    {
        _serverListService.HandleEvent(evt);
    }

    /// <summary>
    /// Refreshes the server list from the API.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!string.IsNullOrEmpty(_currentServerUrl))
        {
            await _serverListService.LoadAsync(_currentServerUrl);
        }
    }

    /// <summary>
    /// Requests deletion of an MCP server with confirmation.
    /// </summary>
    /// <param name="serverName">The name of the server to delete.</param>
    [RelayCommand]
    private async Task DeleteServerAsync(string serverName)
    {
        if (string.IsNullOrEmpty(_currentServerUrl))
        {
            return;
        }

        // Request confirmation
        var confirmed = DeleteConfirmationRequested != null
            && await DeleteConfirmationRequested.Invoke(serverName);

        if (!confirmed)
        {
            return;
        }

        await _serverListService.DeleteAsync(_currentServerUrl, serverName);
    }

    private void OnServersChanged()
    {
        ServersChanged?.Invoke();
    }

    /// <summary>
    /// Refreshes metadata for an MCP server.
    /// If the server has running instances, uses one of them.
    /// Otherwise, starts a temporary instance, refreshes, then stops it.
    /// </summary>
    /// <param name="serverName">The name of the server to refresh metadata for.</param>
    [RelayCommand]
    private async Task RefreshMetadataAsync(string serverName)
    {
        if (string.IsNullOrEmpty(_currentServerUrl) || IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;
        RefreshingServerName = serverName;

        try
        {
            // Get running instances
            var instancesResult = await _invocationService.GetInstancesAsync(_currentServerUrl, serverName);

            string instanceId;
            bool startedTemporaryInstance = false;

            if (instancesResult.IsSuccess && instancesResult.Value!.Count > 0)
            {
                // Use the first running instance
                instanceId = instancesResult.Value[0].InstanceId;
            }
            else
            {
                // Start a temporary instance
                var startResult = await _invocationService.StartInstanceAsync(_currentServerUrl, serverName);
                if (!startResult.IsSuccess)
                {
                    // Failed to start instance - the server list will update via SSE
                    return;
                }
                instanceId = startResult.InstanceId!;
                startedTemporaryInstance = true;
            }

            // Refresh metadata
            var refreshResult = await _invocationService.RefreshMetadataAsync(
                _currentServerUrl,
                serverName,
                instanceId);

            // If we started a temporary instance, stop it
            if (startedTemporaryInstance)
            {
                await _invocationService.StopInstanceAsync(_currentServerUrl, serverName, instanceId);
            }
        }
        finally
        {
            IsRefreshing = false;
            RefreshingServerName = null;
        }
    }
}
