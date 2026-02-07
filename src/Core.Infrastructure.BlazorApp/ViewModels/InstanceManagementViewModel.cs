using System.Collections.ObjectModel;
using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.McpServers;
using Core.Application.Storage;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for the instance management dialog.
/// </summary>
public partial class InstanceManagementViewModel : ViewModelBase
{
    private readonly IToolInvocationService _invocationService;
    private readonly IApplicationStateService _appState;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _serverName = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isStartingInstance;

    [ObservableProperty]
    private string? _stoppingInstanceId;

    /// <summary>
    /// The list of running instances.
    /// </summary>
    public ObservableCollection<McpServerInstance> Instances { get; } = new();

    public InstanceManagementViewModel(
        IToolInvocationService invocationService,
        IApplicationStateService appState)
    {
        _invocationService = invocationService;
        _appState = appState;
    }

    /// <summary>
    /// Opens the dialog for managing instances of a server.
    /// </summary>
    [RelayCommand]
    private async Task OpenAsync(string serverName)
    {
        ServerName = serverName;
        ErrorMessage = null;
        Instances.Clear();
        IsOpen = true;

        await RefreshAsync();
    }

    /// <summary>
    /// Refreshes the list of instances.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await _appState.LoadAsync();
        var serverUrl = _appState.ServerUrl;
        if (string.IsNullOrEmpty(serverUrl))
        {
            ErrorMessage = "No server URL configured";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _invocationService.GetInstancesAsync(serverUrl, ServerName);
            if (result.IsSuccess)
            {
                Instances.Clear();
                foreach (var instance in result.Value!)
                {
                    Instances.Add(instance);
                }
            }
            else
            {
                ErrorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Starts a new instance of the server.
    /// </summary>
    [RelayCommand]
    private async Task StartNewInstanceAsync()
    {
        await _appState.LoadAsync();
        var serverUrl = _appState.ServerUrl;
        if (string.IsNullOrEmpty(serverUrl))
        {
            ErrorMessage = "No server URL configured";
            return;
        }

        IsStartingInstance = true;
        ErrorMessage = null;

        try
        {
            var result = await _invocationService.StartInstanceAsync(serverUrl, ServerName);
            if (result.IsSuccess)
            {
                // Refresh to get the new instance in the list
                await RefreshAsync();
            }
            else
            {
                ErrorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsStartingInstance = false;
        }
    }

    /// <summary>
    /// Stops a running instance.
    /// </summary>
    [RelayCommand]
    private async Task StopInstanceAsync(string instanceId)
    {
        await _appState.LoadAsync();
        var serverUrl = _appState.ServerUrl;
        if (string.IsNullOrEmpty(serverUrl))
        {
            ErrorMessage = "No server URL configured";
            return;
        }

        StoppingInstanceId = instanceId;
        ErrorMessage = null;

        try
        {
            var result = await _invocationService.StopInstanceAsync(serverUrl, ServerName, instanceId);
            if (result.IsSuccess)
            {
                // Remove from the list immediately for better UX
                var instance = Instances.FirstOrDefault(i => i.InstanceId == instanceId);
                if (instance != null)
                {
                    Instances.Remove(instance);
                }
            }
            else
            {
                ErrorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            StoppingInstanceId = null;
        }
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }
}
