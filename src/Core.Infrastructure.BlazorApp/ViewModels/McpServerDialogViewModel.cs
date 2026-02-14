using System.Text.Json;
using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.McpServers;
using Core.Application.Storage;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for the MCP server add/edit dialog.
/// </summary>
public partial class McpServerDialogViewModel : ViewModelBase
{
    private readonly IMcpServerConfigService _configService;
    private readonly IApplicationStateService _appState;
    private readonly IToolInvocationService _invocationService;
    private readonly IUserPreferencesService _preferences;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _serverName = string.Empty;

    [ObservableProperty]
    private string _command = string.Empty;

    [ObservableProperty]
    private List<string> _arguments = new();

    [ObservableProperty]
    private Dictionary<string, string> _environmentVariables = new();

    [ObservableProperty]
    private ConnectionTestState _testState = ConnectionTestState.None;

    [ObservableProperty]
    private string? _testErrorMessage;

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _autoRefreshMetadataOnAdd;

    [ObservableProperty]
    private bool _isRefreshingMetadata;

    [ObservableProperty]
    private bool _isJsonMode;

    [ObservableProperty]
    private string _jsonInputValue = "{}";

    [ObservableProperty]
    private string? _jsonParseError;

    [ObservableProperty]
    private IReadOnlyList<string> _stderrLines = Array.Empty<string>();

    [ObservableProperty]
    private bool _showStderrPanel;

    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Event raised when a server is successfully saved.
    /// </summary>
    public event Action<string>? ServerSaved;

    public McpServerDialogViewModel(
        IMcpServerConfigService configService,
        IApplicationStateService appState,
        IToolInvocationService invocationService,
        IUserPreferencesService preferences)
    {
        _configService = configService;
        _appState = appState;
        _invocationService = invocationService;
        _preferences = preferences;
    }

    /// <summary>
    /// Gets the dialog title based on the mode.
    /// </summary>
    public string DialogTitle => IsEditMode ? "Edit MCP Server" : "Add MCP Server";

    /// <summary>
    /// Gets whether the form can be tested.
    /// </summary>
    public bool CanTest => !string.IsNullOrWhiteSpace(Command) && TestState != ConnectionTestState.Testing;

    /// <summary>
    /// Opens the dialog for adding a new server.
    /// </summary>
    [RelayCommand]
    private void OpenForAdd()
    {
        ResetForm();
        IsEditMode = false;
        AutoRefreshMetadataOnAdd = _preferences.AutoRefreshMetadataOnAdd;
        IsOpen = true;
    }

    /// <summary>
    /// Opens the dialog for editing an existing server.
    /// </summary>
    [RelayCommand]
    private async Task OpenForEditAsync(string serverName)
    {
        ResetForm();
        IsEditMode = true;
        ServerName = serverName;
        IsLoading = true;
        IsOpen = true;

        try
        {
            await _appState.LoadAsync();
            var serverUrl = _appState.ServerUrl;
            if (string.IsNullOrEmpty(serverUrl))
            {
                ValidationError = "No server URL configured";
                IsLoading = false;
                return;
            }

            var result = await _configService.GetConfigurationAsync(serverUrl, serverName);
            if (result.IsSuccess && result.Value != null)
            {
                Command = result.Value.Command;
                Arguments = result.Value.Args.ToList();
                EnvironmentVariables = new Dictionary<string, string>(result.Value.Env);
                SyncFormToJson();
            }
            else
            {
                ValidationError = result.Error ?? "Failed to load configuration";
            }
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Tests the configuration by starting a temporary instance without persisting.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task TestConfigurationAsync()
    {
        ValidationError = null;

        if (!ValidateForm())
        {
            TestState = ConnectionTestState.Failed;
            return;
        }

        await _appState.LoadAsync();
        var serverUrl = _appState.ServerUrl;
        if (string.IsNullOrEmpty(serverUrl))
        {
            TestState = ConnectionTestState.Failed;
            TestErrorMessage = "No server URL configured";
            return;
        }

        TestState = ConnectionTestState.Testing;
        TestErrorMessage = null;
        ShowStderrPanel = true;
        StderrLines = Array.Empty<string>();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var args = Arguments.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
            var env = EnvironmentVariables
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var testResult = await _invocationService.TestConfigurationAsync(
                serverUrl,
                Command,
                args,
                env,
                _cancellationTokenSource.Token);

            StderrLines = testResult.StderrLines;

            if (!testResult.IsSuccess)
            {
                TestState = ConnectionTestState.Failed;
                TestErrorMessage = testResult.ErrorMessage;
                return;
            }

            TestState = ConnectionTestState.Success;
            TestErrorMessage = null;
        }
        catch (OperationCanceledException)
        {
            TestState = ConnectionTestState.None;
            ShowStderrPanel = false;
        }
        catch (Exception ex)
        {
            TestState = ConnectionTestState.Failed;
            TestErrorMessage = ex.Message;
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Saves the server configuration.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        await _appState.LoadAsync();
        var serverUrl = _appState.ServerUrl;
        if (string.IsNullOrEmpty(serverUrl))
        {
            ValidationError = "No server URL configured";
            return;
        }

        var args = Arguments.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
        var env = EnvironmentVariables
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        McpServerConfigServiceResult result;

        if (IsEditMode)
        {
            // Update existing server configuration
            result = await _configService.UpdateConfigurationAsync(
                serverUrl,
                ServerName,
                Command,
                args,
                env);
        }
        else
        {
            // Create new server
            result = await _configService.CreateServerAsync(
                serverUrl,
                ServerName,
                Command,
                args,
                env);
        }

        if (!result.IsSuccess)
        {
            ValidationError = result.Error ?? "Failed to save configuration";
            return;
        }

        // Save the auto-refresh preference
        _preferences.AutoRefreshMetadataOnAdd = AutoRefreshMetadataOnAdd;

        // Trigger auto-refresh for new servers if enabled
        if (!IsEditMode && AutoRefreshMetadataOnAdd)
        {
            await RefreshMetadataForServerAsync(serverUrl, ServerName);
        }

        IsOpen = false;
        ServerSaved?.Invoke(ServerName);
    }

    private bool CanSave() => TestState == ConnectionTestState.Success;

    private async Task RefreshMetadataForServerAsync(string serverUrl, string serverName)
    {
        IsRefreshingMetadata = true;

        try
        {
            // Start a temporary instance
            var startResult = await _invocationService.StartInstanceAsync(serverUrl, serverName);
            if (!startResult.IsSuccess)
            {
                // Failed to start instance - don't block the save
                return;
            }

            var instanceId = startResult.InstanceId!;

            // Refresh metadata
            await _invocationService.RefreshMetadataAsync(serverUrl, serverName, instanceId);

            // Stop the temporary instance
            await _invocationService.StopInstanceAsync(serverUrl, serverName, instanceId);
        }
        finally
        {
            IsRefreshingMetadata = false;
        }
    }

    partial void OnTestStateChanged(ConnectionTestState value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        TestConfigurationCommand.NotifyCanExecuteChanged();
    }

    partial void OnCommandChanged(string value)
    {
        TestConfigurationCommand.NotifyCanExecuteChanged();
        // Reset test state when form changes
        if (TestState == ConnectionTestState.Success)
        {
            TestState = ConnectionTestState.None;
        }
    }

    partial void OnArgumentsChanged(List<string> value)
    {
        if (TestState == ConnectionTestState.Success)
        {
            TestState = ConnectionTestState.None;
        }
    }

    partial void OnEnvironmentVariablesChanged(Dictionary<string, string> value)
    {
        if (TestState == ConnectionTestState.Success)
        {
            TestState = ConnectionTestState.None;
        }
    }

    /// <summary>
    /// Collapses the stderr output panel.
    /// </summary>
    [RelayCommand]
    private void CollapseStderrPanel() => ShowStderrPanel = false;

    /// <summary>
    /// Cancels and closes the dialog.
    /// </summary>
    [RelayCommand]
    private Task CancelAsync()
    {
        _cancellationTokenSource?.Cancel();
        IsOpen = false;
        return Task.CompletedTask;
    }

    private void ResetForm()
    {
        ServerName = string.Empty;
        Command = string.Empty;
        Arguments = new List<string>();
        EnvironmentVariables = new Dictionary<string, string>();
        TestState = ConnectionTestState.None;
        TestErrorMessage = null;
        ValidationError = null;
        IsLoading = false;
        IsJsonMode = false;
        JsonInputValue = "{}";
        JsonParseError = null;
        StderrLines = Array.Empty<string>();
        ShowStderrPanel = false;
    }

    /// <summary>
    /// Serializes the current form fields (Command, Arguments, EnvironmentVariables) to JSON.
    /// </summary>
    public void SyncFormToJson()
    {
        try
        {
            var config = new Dictionary<string, object?>
            {
                ["command"] = Command,
                ["args"] = Arguments,
                ["env"] = EnvironmentVariables
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonInputValue = JsonSerializer.Serialize(config, options);
            JsonParseError = null;
        }
        catch (Exception ex)
        {
            JsonParseError = $"Error serializing: {ex.Message}";
        }
    }

    /// <summary>
    /// Deserializes JSON input into form fields (Command, Arguments, EnvironmentVariables).
    /// </summary>
    public void SyncJsonToForm()
    {
        try
        {
            Command = string.Empty;
            Arguments = new List<string>();
            EnvironmentVariables = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(JsonInputValue))
            {
                JsonParseError = null;
                return;
            }

            var parsed = JsonSerializer.Deserialize<JsonElement>(JsonInputValue);
            JsonElement config = parsed;

            // Check if this is wrapped format: {"serverName": {"command": "..."}}
            if (!parsed.TryGetProperty("command", out _))
            {
                foreach (var prop in parsed.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Object &&
                        prop.Value.TryGetProperty("command", out _))
                    {
                        config = prop.Value;
                        if (string.IsNullOrWhiteSpace(ServerName))
                        {
                            ServerName = prop.Name;
                        }
                        break;
                    }
                }
            }

            if (config.TryGetProperty("command", out var commandElement))
            {
                Command = commandElement.GetString() ?? string.Empty;
            }

            if (config.TryGetProperty("args", out var argsElement) && argsElement.ValueKind == JsonValueKind.Array)
            {
                Arguments = argsElement.EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .ToList();
            }

            if (config.TryGetProperty("env", out var envElement) && envElement.ValueKind == JsonValueKind.Object)
            {
                EnvironmentVariables = envElement.EnumerateObject()
                    .ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty);
            }

            JsonParseError = null;
        }
        catch (JsonException ex)
        {
            JsonParseError = $"Invalid JSON: {ex.Message}";
        }
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(ServerName))
        {
            ValidationError = "Server name is required";
            return false;
        }

        if (ServerName.Contains(' '))
        {
            ValidationError = "Server name cannot contain spaces";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Command))
        {
            ValidationError = "Command is required";
            return false;
        }

        ValidationError = null;
        return true;
    }

}
