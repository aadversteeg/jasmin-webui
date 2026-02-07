using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.McpServers;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for the resource viewer dialog.
/// </summary>
public partial class ResourceViewerViewModel : ViewModelBase
{
    private readonly IResourceViewerService _resourceViewerService;
    private readonly IToolInvocationService _toolInvocationService;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private McpServerResource? _resource;

    [ObservableProperty]
    private string _serverName = string.Empty;

    [ObservableProperty]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    private string? _instanceId;

    [ObservableProperty]
    private bool _isStartingInstance;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private McpResourceReadResult? _result;

    private CancellationTokenSource? _cancellationTokenSource;

    public ResourceViewerViewModel(
        IResourceViewerService resourceViewerService,
        IToolInvocationService toolInvocationService)
    {
        _resourceViewerService = resourceViewerService;
        _toolInvocationService = toolInvocationService;
    }

    /// <summary>
    /// Gets the first content block from the result (most common case).
    /// </summary>
    public McpResourceContent? Content => Result?.Contents.FirstOrDefault();

    /// <summary>
    /// Opens the resource viewer for the specified resource and automatically loads it.
    /// </summary>
    public async Task OpenAsync(
        McpServerResource resource,
        string serverName,
        string serverUrl)
    {
        Resource = resource;
        ServerName = serverName;
        ServerUrl = serverUrl;
        InstanceId = null;
        Result = null;
        ErrorMessage = null;
        IsOpen = true;

        // Start instance and load resource automatically
        await StartInstanceAndLoadAsync();
    }

    /// <summary>
    /// Closes the resource viewer and stops the instance.
    /// </summary>
    [RelayCommand]
    public async Task CloseAsync()
    {
        _cancellationTokenSource?.Cancel();
        await StopInstanceAsync();

        IsOpen = false;
        Resource = null;
        Result = null;
        ErrorMessage = null;
        InstanceId = null;
    }

    private async Task StartInstanceAndLoadAsync()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        IsStartingInstance = true;
        ErrorMessage = null;

        try
        {
            // Start the instance
            var startResult = await _toolInvocationService.StartInstanceAsync(
                ServerUrl,
                ServerName,
                _cancellationTokenSource.Token);

            if (!startResult.IsSuccess)
            {
                ErrorMessage = startResult.Error;
                IsStartingInstance = false;
                return;
            }

            InstanceId = startResult.Value;
            IsStartingInstance = false;

            // Automatically load the resource
            await LoadResourceAsync();
        }
        catch (OperationCanceledException)
        {
            // Cancelled
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
    /// Retries loading the resource.
    /// </summary>
    [RelayCommand]
    public async Task RetryAsync()
    {
        if (string.IsNullOrEmpty(InstanceId))
        {
            await StartInstanceAndLoadAsync();
        }
        else
        {
            await LoadResourceAsync();
        }
    }

    private async Task LoadResourceAsync()
    {
        if (Resource == null || string.IsNullOrEmpty(InstanceId))
        {
            ErrorMessage = "No resource or instance selected";
            return;
        }

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        IsLoading = true;
        ErrorMessage = null;
        Result = null;

        try
        {
            var result = await _resourceViewerService.ReadResourceAsync(
                ServerUrl,
                ServerName,
                InstanceId,
                Resource.Uri,
                _cancellationTokenSource.Token);

            if (result.IsSuccess)
            {
                Result = result.Value;
                OnPropertyChanged(nameof(Content));
            }
            else
            {
                ErrorMessage = result.Error;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled by user
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

    private async Task StopInstanceAsync()
    {
        if (!string.IsNullOrEmpty(InstanceId))
        {
            try
            {
                await _toolInvocationService.StopInstanceAsync(
                    ServerUrl,
                    ServerName,
                    InstanceId);
            }
            catch
            {
                // Ignore errors when stopping
            }
        }
    }
}
