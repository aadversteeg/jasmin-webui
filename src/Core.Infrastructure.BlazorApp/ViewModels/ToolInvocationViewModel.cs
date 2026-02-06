using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.McpServers;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// View state for the tool invocation dialog.
/// </summary>
public enum InvocationView
{
    Input,
    Output
}

/// <summary>
/// ViewModel for the tool invocation dialog.
/// </summary>
public partial class ToolInvocationViewModel : ViewModelBase
{
    private readonly IToolInvocationService _invocationService;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private InvocationView _currentView = InvocationView.Input;

    [ObservableProperty]
    private McpServerTool? _tool;

    [ObservableProperty]
    private string _serverName = string.Empty;

    [ObservableProperty]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    private string? _instanceId;

    [ObservableProperty]
    private bool _isStartingInstance;

    [ObservableProperty]
    private bool _isInvoking;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ToolInvocationResult? _result;

    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Input values for the tool parameters.
    /// Key is parameter name, value is the input value.
    /// </summary>
    public Dictionary<string, object?> InputValues { get; } = new();

    public ToolInvocationViewModel(IToolInvocationService invocationService)
    {
        _invocationService = invocationService;
    }

    /// <summary>
    /// Opens the dialog to invoke a tool.
    /// </summary>
    /// <param name="tool">The tool to invoke.</param>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="serverUrl">The jasmin-server URL.</param>
    public async Task OpenAsync(McpServerTool tool, string serverName, string? serverUrl)
    {
        if (string.IsNullOrEmpty(serverUrl))
        {
            return;
        }

        // Reset state
        Tool = tool;
        ServerName = serverName;
        ServerUrl = serverUrl;
        CurrentView = InvocationView.Input;
        ErrorMessage = null;
        Result = null;
        InstanceId = null;
        InputValues.Clear();

        // Initialize input values with defaults
        if (tool.InputSchema?.Parameters != null)
        {
            foreach (var param in tool.InputSchema.Parameters)
            {
                InputValues[param.Name] = param.Default;
            }
        }

        IsOpen = true;
        _cancellationTokenSource = new CancellationTokenSource();

        // Start instance in background
        await StartInstanceAsync();
    }

    private async Task StartInstanceAsync()
    {
        IsStartingInstance = true;
        ErrorMessage = null;

        try
        {
            var result = await _invocationService.StartInstanceAsync(
                ServerUrl,
                ServerName,
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            if (result.IsSuccess)
            {
                InstanceId = result.Value;
            }
            else
            {
                ErrorMessage = result.Error;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled, ignore
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
    /// Sets an input value for a parameter.
    /// </summary>
    public void SetInputValue(string parameterName, object? value)
    {
        InputValues[parameterName] = value;
    }

    /// <summary>
    /// Invokes the tool with the current input values.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInvoke))]
    private async Task InvokeAsync()
    {
        if (Tool == null || string.IsNullOrEmpty(InstanceId))
        {
            return;
        }

        IsInvoking = true;
        ErrorMessage = null;
        Result = null;

        try
        {
            // Build input from InputValues, filtering out null/empty values for optional params
            var input = new Dictionary<string, object?>();
            if (Tool.InputSchema?.Parameters != null)
            {
                foreach (var param in Tool.InputSchema.Parameters)
                {
                    if (InputValues.TryGetValue(param.Name, out var value) && value != null)
                    {
                        // Convert string values to appropriate types
                        input[param.Name] = ConvertValue(value, param.Type);
                    }
                    else if (param.Required)
                    {
                        ErrorMessage = $"Required parameter '{param.Name}' is missing";
                        IsInvoking = false;
                        return;
                    }
                }
            }

            var result = await _invocationService.InvokeToolAsync(
                ServerUrl,
                ServerName,
                InstanceId,
                Tool.Name,
                input.Count > 0 ? input : null,
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            if (result.IsSuccess)
            {
                Result = result.Value;
                CurrentView = InvocationView.Output;
            }
            else
            {
                ErrorMessage = result.Error;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled, ignore
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsInvoking = false;
        }
    }

    private bool CanInvoke() => !IsStartingInstance && !IsInvoking && !string.IsNullOrEmpty(InstanceId);

    partial void OnIsStartingInstanceChanged(bool value) => InvokeCommand.NotifyCanExecuteChanged();
    partial void OnIsInvokingChanged(bool value) => InvokeCommand.NotifyCanExecuteChanged();
    partial void OnInstanceIdChanged(string? value) => InvokeCommand.NotifyCanExecuteChanged();

    private static object? ConvertValue(object value, string paramType)
    {
        if (value is string strValue)
        {
            return paramType.ToLowerInvariant() switch
            {
                "integer" or "number" => double.TryParse(strValue, out var num) ? num : strValue,
                "boolean" => bool.TryParse(strValue, out var b) ? b : strValue,
                _ => strValue
            };
        }
        return value;
    }

    /// <summary>
    /// Goes back to the input view.
    /// </summary>
    [RelayCommand]
    private void Back()
    {
        CurrentView = InvocationView.Input;
        ErrorMessage = null;
    }

    /// <summary>
    /// Closes the dialog and stops the instance.
    /// </summary>
    [RelayCommand]
    private async Task CloseAsync()
    {
        _cancellationTokenSource?.Cancel();

        // Stop instance if running
        if (!string.IsNullOrEmpty(InstanceId))
        {
            try
            {
                await _invocationService.StopInstanceAsync(
                    ServerUrl,
                    ServerName,
                    InstanceId);
            }
            catch
            {
                // Ignore errors when stopping
            }
        }

        IsOpen = false;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
}
