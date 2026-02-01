using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.Events;
using Core.Application.Storage;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for the configuration dialog.
/// </summary>
public partial class ConfigurationViewModel : ViewModelBase
{
    private const string ServerUrlKey = "jasmin-webui:server-url";

    private readonly IEventStreamService _eventStreamService;
    private readonly ILocalStorageService _localStorage;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _serverUrl = "";

    [ObservableProperty]
    private ConnectionTestState _testState = ConnectionTestState.None;

    [ObservableProperty]
    private string? _testErrorMessage;

    /// <summary>
    /// Event raised when a URL is successfully saved.
    /// </summary>
    public event Action<string>? UrlSaved;

    /// <summary>
    /// Event raised when disconnect is requested.
    /// </summary>
    public event Action? DisconnectRequested;

    public ConfigurationViewModel(
        IEventStreamService eventStreamService,
        ILocalStorageService localStorage)
    {
        _eventStreamService = eventStreamService;
        _localStorage = localStorage;
    }

    /// <summary>
    /// Opens the configuration dialog.
    /// </summary>
    [RelayCommand]
    private async Task OpenAsync()
    {
        // Load current URL from storage
        var savedUrl = await _localStorage.GetAsync<string>(ServerUrlKey);
        ServerUrl = savedUrl ?? "";
        TestState = ConnectionTestState.None;
        TestErrorMessage = null;
        IsOpen = true;
    }

    /// <summary>
    /// Tests the connection to the server.
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            TestState = ConnectionTestState.Failed;
            TestErrorMessage = "Please enter a server URL";
            return;
        }

        TestState = ConnectionTestState.Testing;
        TestErrorMessage = null;

        var (success, errorMessage) = await _eventStreamService.TestConnectionAsync(ServerUrl);

        if (success)
        {
            TestState = ConnectionTestState.Success;
            TestErrorMessage = null;
        }
        else
        {
            TestState = ConnectionTestState.Failed;
            TestErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Saves the URL and closes the dialog.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        await _localStorage.SetAsync(ServerUrlKey, ServerUrl);
        IsOpen = false;
        UrlSaved?.Invoke(ServerUrl);
    }

    private bool CanSave() => TestState == ConnectionTestState.Success;

    partial void OnTestStateChanged(ConnectionTestState value)
    {
        SaveCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Cancels and closes the dialog without saving.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        IsOpen = false;
        TestState = ConnectionTestState.None;
        TestErrorMessage = null;
    }

    /// <summary>
    /// Disconnects from the server and clears the URL.
    /// </summary>
    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _localStorage.RemoveAsync(ServerUrlKey);
        ServerUrl = "";
        TestState = ConnectionTestState.None;
        TestErrorMessage = null;
        IsOpen = false;
        DisconnectRequested?.Invoke();
    }
}
