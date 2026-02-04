using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.McpServers;
using Core.Domain.Events;

namespace Core.Infrastructure.BlazorApp.ViewModels;

public partial class McpServerListViewModel : ViewModelBase
{
    private readonly IMcpServerListService _serverListService;
    private string? _currentServerUrl;

    public McpServerListViewModel(IMcpServerListService serverListService)
    {
        _serverListService = serverListService;
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
}
