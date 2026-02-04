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
    private readonly IEventStreamService _eventStreamService;
    private readonly IApplicationStateService _appState;

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
        IApplicationStateService appState)
    {
        _eventStreamService = eventStreamService;
        _appState = appState;
    }

    /// <summary>
    /// Opens the configuration dialog.
    /// </summary>
    [RelayCommand]
    private async Task OpenAsync()
    {
        // Load current URL from state
        await _appState.LoadAsync();
        ServerUrl = _appState.ServerUrl ?? "";
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
    private Task SaveAsync()
    {
        _appState.ServerUrl = ServerUrl;
        IsOpen = false;
        UrlSaved?.Invoke(ServerUrl);
        return Task.CompletedTask;
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
    private Task DisconnectAsync()
    {
        _appState.ServerUrl = null;
        ServerUrl = "";
        TestState = ConnectionTestState.None;
        TestErrorMessage = null;
        IsOpen = false;
        DisconnectRequested?.Invoke();
        return Task.CompletedTask;
    }
}
