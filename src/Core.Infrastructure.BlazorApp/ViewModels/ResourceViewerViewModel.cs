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
    private string _pageBasePath = string.Empty;

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

    /// <summary>
    /// Event raised when a resource is opened or navigated to.
    /// The parent page can subscribe to this to update the URL.
    /// </summary>
    public event EventHandler<ResourceChangedEventArgs>? ResourceChanged;

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
    /// Gets the URI of the currently displayed resource.
    /// </summary>
    public string? CurrentResourceUri => Resource?.Uri;

    /// <summary>
    /// Opens the resource viewer for the specified resource and automatically loads it.
    /// </summary>
    /// <param name="resource">The resource to view.</param>
    /// <param name="serverName">The server name.</param>
    /// <param name="serverUrl">The server URL.</param>
    /// <param name="pageBasePath">The base path of the page (e.g., /mcp-servers/everything).</param>
    /// <param name="raiseEvent">Whether to raise the ResourceChanged event.</param>
    public async Task OpenAsync(
        McpServerResource resource,
        string serverName,
        string serverUrl,
        string pageBasePath = "",
        bool raiseEvent = true)
    {
        Resource = resource;
        ServerName = serverName;
        ServerUrl = serverUrl;
        PageBasePath = pageBasePath;
        InstanceId = null;
        Result = null;
        ErrorMessage = null;
        IsOpen = true;

        if (raiseEvent)
        {
            RaiseResourceChanged(resource.Uri);
        }

        // Start instance and load resource automatically
        await StartInstanceAndLoadAsync();
    }

    /// <summary>
    /// Opens the resource viewer for a resource URI (used for deep linking).
    /// Creates a temporary McpServerResource from the URI.
    /// </summary>
    public async Task OpenByUriAsync(
        string resourceUri,
        string serverName,
        string serverUrl,
        string pageBasePath = "",
        bool raiseEvent = true)
    {
        // Create a temporary resource from the URI
        var name = ExtractNameFromUri(resourceUri);
        var resource = new McpServerResource(
            Name: name,
            Uri: resourceUri,
            Title: name,
            Description: null,
            MimeType: GuessMimeType(resourceUri));

        await OpenAsync(resource, serverName, serverUrl, pageBasePath, raiseEvent);
    }

    /// <summary>
    /// Navigates to a different resource within the same server.
    /// Used when an internal link is clicked.
    /// </summary>
    public async Task NavigateToResourceAsync(string resourceUri)
    {
        if (string.IsNullOrEmpty(ServerName) || string.IsNullOrEmpty(ServerUrl))
        {
            ErrorMessage = "No server context available";
            return;
        }

        // Create a temporary resource from the URI
        var name = ExtractNameFromUri(resourceUri);
        var resource = new McpServerResource(
            Name: name,
            Uri: resourceUri,
            Title: name,
            Description: null,
            MimeType: GuessMimeType(resourceUri));

        Resource = resource;
        Result = null;
        ErrorMessage = null;

        RaiseResourceChanged(resourceUri);

        // If we have an instance, load the new resource directly
        if (!string.IsNullOrEmpty(InstanceId))
        {
            await LoadResourceAsync();
        }
        else
        {
            await StartInstanceAndLoadAsync();
        }
    }

    /// <summary>
    /// Closes the resource viewer and stops the instance.
    /// </summary>
    /// <param name="raiseEvent">Whether to raise the ResourceChanged event with null URI.</param>
    [RelayCommand]
    public async Task CloseAsync(bool raiseEvent = true)
    {
        _cancellationTokenSource?.Cancel();
        await StopInstanceAsync();

        IsOpen = false;
        Resource = null;
        Result = null;
        ErrorMessage = null;
        InstanceId = null;

        if (raiseEvent)
        {
            RaiseResourceChanged(null);
        }
    }

    private void RaiseResourceChanged(string? resourceUri)
    {
        ResourceChanged?.Invoke(this, new ResourceChangedEventArgs(resourceUri));
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

    private static string ExtractNameFromUri(string uri)
    {
        // Extract the last segment of the URI as the name
        if (string.IsNullOrEmpty(uri))
            return "Resource";

        var lastSlash = uri.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < uri.Length - 1)
            return uri[(lastSlash + 1)..];

        return uri;
    }

    private static string? GuessMimeType(string uri)
    {
        if (string.IsNullOrEmpty(uri))
            return null;

        var extension = Path.GetExtension(uri).ToLowerInvariant();
        return extension switch
        {
            ".md" => "text/markdown",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "text/javascript",
            ".xml" => "text/xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => null
        };
    }
}

/// <summary>
/// Event args for when the resource changes.
/// </summary>
public class ResourceChangedEventArgs : EventArgs
{
    /// <summary>
    /// The URI of the new resource, or null if the viewer was closed.
    /// </summary>
    public string? ResourceUri { get; }

    public ResourceChangedEventArgs(string? resourceUri)
    {
        ResourceUri = resourceUri;
    }
}
