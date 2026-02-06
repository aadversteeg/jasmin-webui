using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.McpServers;
using Core.Application.Storage;

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
    private readonly IToolHistoryService _historyService;

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

    [ObservableProperty]
    private DateTime? _lastInvokedAt;

    [ObservableProperty]
    private int _historyPosition = -1; // -1 = draft, 0..N = history entries (0 = oldest)

    private CancellationTokenSource? _cancellationTokenSource;
    private IReadOnlyList<McpServerTool> _availableTools = Array.Empty<McpServerTool>();
    private int _currentToolIndex;

    /// <summary>
    /// Input values for the tool parameters.
    /// Key is parameter name, value is the input value.
    /// </summary>
    public Dictionary<string, object?> InputValues { get; } = new();

    /// <summary>
    /// Gets the available tools for navigation.
    /// </summary>
    public IReadOnlyList<McpServerTool> AvailableTools => _availableTools;

    /// <summary>
    /// Gets the current tool index in the available tools list.
    /// </summary>
    public int CurrentToolIndex => _currentToolIndex;

    /// <summary>
    /// Gets the total number of history entries for the current tool.
    /// </summary>
    public int HistoryCount => Tool != null ? _historyService.GetHistory(ServerName, Tool.Name).Count : 0;

    /// <summary>
    /// Gets a display string for the current history position.
    /// </summary>
    public string HistoryPositionDisplay
    {
        get
        {
            var count = HistoryCount;
            if (count == 0)
            {
                return "new";
            }
            if (HistoryPosition == -1)
            {
                // Show "new" with history count indicator
                return $"new ({count})";
            }
            // Position is 0-indexed (oldest first), display as 1-indexed
            // HistoryPosition 0 = oldest = "1/count", HistoryPosition count-1 = newest = "count/count"
            return $"{HistoryPosition + 1}/{count}";
        }
    }

    /// <summary>
    /// Gets whether we can navigate up (to older history).
    /// From draft: can go to newest entry. From history: can go to older entry.
    /// </summary>
    public bool CanGoHistoryUp => HistoryCount > 0 && (HistoryPosition == -1 || HistoryPosition > 0);

    /// <summary>
    /// Gets whether we can navigate down (to newer history or draft).
    /// </summary>
    public bool CanGoHistoryDown => HistoryPosition >= 0;

    /// <summary>
    /// Gets whether we can navigate to the previous tool.
    /// </summary>
    public bool CanGoPreviousTool => _availableTools.Count > 1 && _currentToolIndex > 0;

    /// <summary>
    /// Gets whether we can navigate to the next tool.
    /// </summary>
    public bool CanGoNextTool => _availableTools.Count > 1 && _currentToolIndex < _availableTools.Count - 1;

    /// <summary>
    /// Gets the invocation timestamp when viewing a history entry, null otherwise.
    /// </summary>
    public DateTime? HistoricalInvokedAt
    {
        get
        {
            if (HistoryPosition < 0 || Tool == null) return null;
            var history = _historyService.GetHistory(ServerName, Tool.Name);
            if (HistoryPosition < history.Count)
                return history[HistoryPosition].InvokedAt;
            return null;
        }
    }

    public ToolInvocationViewModel(IToolInvocationService invocationService, IToolHistoryService historyService)
    {
        _invocationService = invocationService;
        _historyService = historyService;
    }

    /// <summary>
    /// Opens the dialog to invoke a tool.
    /// </summary>
    /// <param name="tool">The tool to invoke.</param>
    /// <param name="serverName">The MCP server name.</param>
    /// <param name="serverUrl">The jasmin-server URL.</param>
    /// <param name="availableTools">All available tools for navigation.</param>
    public async Task OpenAsync(McpServerTool tool, string serverName, string? serverUrl, IReadOnlyList<McpServerTool>? availableTools = null)
    {
        if (string.IsNullOrEmpty(serverUrl))
        {
            return;
        }

        // Load history service if not loaded
        if (!_historyService.IsLoaded)
        {
            await _historyService.LoadAsync();
        }

        // Reset state
        ServerName = serverName;
        ServerUrl = serverUrl;
        _availableTools = availableTools ?? new List<McpServerTool> { tool };
        _currentToolIndex = FindToolIndex(_availableTools, tool);
        if (_currentToolIndex < 0) _currentToolIndex = 0;

        CurrentView = InvocationView.Input;
        ErrorMessage = null;
        Result = null;
        InstanceId = null;

        // Set tool and load draft
        await SwitchToToolAsync(tool);

        IsOpen = true;
        _cancellationTokenSource = new CancellationTokenSource();

        // Start instance in background
        await StartInstanceAsync();

        // Notify navigation state changed
        NotifyNavigationChanged();
    }

    private async Task SwitchToToolAsync(McpServerTool tool)
    {
        // Save current draft if there's a current tool
        if (Tool != null && InputValues.Count > 0)
        {
            await _historyService.SaveDraftAsync(ServerName, Tool.Name, InputValues);
        }

        Tool = tool;
        HistoryPosition = -1;
        InputValues.Clear();

        // Try to load draft
        var draft = _historyService.GetDraft(ServerName, tool.Name);
        if (draft != null)
        {
            foreach (var kvp in draft)
            {
                InputValues[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            // Initialize with defaults
            if (tool.InputSchema?.Parameters != null)
            {
                foreach (var param in tool.InputSchema.Parameters)
                {
                    InputValues[param.Name] = param.Default;
                }
            }
        }

        // Clear output state when switching tools
        Result = null;
        LastInvokedAt = null;
        ErrorMessage = null;
        CurrentView = InvocationView.Input;

        NotifyNavigationChanged();
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
    /// Clears output when user edits inputs (entering draft mode).
    /// </summary>
    public void SetInputValue(string parameterName, object? value)
    {
        InputValues[parameterName] = value;

        // Clear output when user edits - they're now in draft mode
        if (HistoryPosition >= 0)
        {
            HistoryPosition = -1;
            Result = null;
            LastInvokedAt = null;
            NotifyNavigationChanged();
        }
        else if (Result != null)
        {
            Result = null;
            LastInvokedAt = null;
        }
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
                LastInvokedAt = null; // Clear - we'll use HistoricalInvokedAt from history
                CurrentView = InvocationView.Output;

                // Add to history (including output) and clear draft
                await _historyService.AddEntryAsync(ServerName, Tool.Name, InputValues, result.Value);
                await _historyService.ClearDraftAsync(ServerName, Tool.Name);

                // Stay at the newest history entry (the one we just added)
                HistoryPosition = HistoryCount - 1;
                NotifyNavigationChanged();
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

    partial void OnIsStartingInstanceChanged(bool value)
    {
        InvokeCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsInvokingChanged(bool value)
    {
        InvokeCommand.NotifyCanExecuteChanged();
    }

    partial void OnInstanceIdChanged(string? value)
    {
        InvokeCommand.NotifyCanExecuteChanged();
    }

    private static int FindToolIndex(IReadOnlyList<McpServerTool> tools, McpServerTool tool)
    {
        for (int i = 0; i < tools.Count; i++)
        {
            if (tools[i].Name == tool.Name)
            {
                return i;
            }
        }
        return -1;
    }

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
    /// Navigate to older history entry.
    /// </summary>
    [RelayCommand]
    private async Task HistoryUpAsync()
    {
        if (!CanGoHistoryUp || Tool == null) return;

        // Save draft before navigating away from it
        if (HistoryPosition == -1)
        {
            await _historyService.SaveDraftAsync(ServerName, Tool.Name, InputValues);
            // Go to newest entry
            HistoryPosition = HistoryCount - 1;
        }
        else
        {
            // Go to older entry (lower index)
            HistoryPosition--;
        }
        LoadHistoryEntry();
        NotifyNavigationChanged();
    }

    /// <summary>
    /// Navigate to newer history entry or draft.
    /// </summary>
    [RelayCommand]
    private void HistoryDown()
    {
        if (!CanGoHistoryDown || Tool == null) return;

        if (HistoryPosition >= HistoryCount - 1)
        {
            // At newest entry, go to draft
            HistoryPosition = -1;
            LoadDraft();
        }
        else
        {
            // Go to newer entry (higher index)
            HistoryPosition++;
            LoadHistoryEntry();
        }
        NotifyNavigationChanged();
    }

    private void LoadHistoryEntry()
    {
        if (Tool == null) return;

        var history = _historyService.GetHistory(ServerName, Tool.Name);
        if (HistoryPosition >= 0 && HistoryPosition < history.Count)
        {
            var entry = history[HistoryPosition];
            InputValues.Clear();
            foreach (var kvp in entry.InputValues)
            {
                InputValues[kvp.Key] = kvp.Value;
            }
            // Load historical output
            Result = entry.Output;
        }
    }

    private void LoadDraft()
    {
        if (Tool == null) return;

        InputValues.Clear();
        var draft = _historyService.GetDraft(ServerName, Tool.Name);
        if (draft != null)
        {
            foreach (var kvp in draft)
            {
                InputValues[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            // Initialize with defaults
            if (Tool.InputSchema?.Parameters != null)
            {
                foreach (var param in Tool.InputSchema.Parameters)
                {
                    InputValues[param.Name] = param.Default;
                }
            }
        }

        // Clear output when entering draft mode
        Result = null;
        LastInvokedAt = null;
    }

    /// <summary>
    /// Navigate to the previous tool in the list.
    /// </summary>
    [RelayCommand]
    private async Task PreviousToolAsync()
    {
        if (!CanGoPreviousTool) return;

        _currentToolIndex--;
        await SwitchToToolAsync(_availableTools[_currentToolIndex]);
    }

    /// <summary>
    /// Navigate to the next tool in the list.
    /// </summary>
    [RelayCommand]
    private async Task NextToolAsync()
    {
        if (!CanGoNextTool) return;

        _currentToolIndex++;
        await SwitchToToolAsync(_availableTools[_currentToolIndex]);
    }

    /// <summary>
    /// Clear the current draft and reset to defaults.
    /// </summary>
    [RelayCommand]
    private async Task ClearDraftAsync()
    {
        if (Tool == null) return;

        await _historyService.ClearDraftAsync(ServerName, Tool.Name);
        InputValues.Clear();

        // Initialize with defaults
        if (Tool.InputSchema?.Parameters != null)
        {
            foreach (var param in Tool.InputSchema.Parameters)
            {
                InputValues[param.Name] = param.Default;
            }
        }

        // Clear output
        Result = null;
        LastInvokedAt = null;

        HistoryPosition = -1;
        NotifyNavigationChanged();
    }

    /// <summary>
    /// Clear all history for the current tool.
    /// </summary>
    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        if (Tool == null) return;

        await _historyService.ClearHistoryAsync(ServerName, Tool.Name);

        // Reset to draft mode
        HistoryPosition = -1;
        Result = null;
        LastInvokedAt = null;
        NotifyNavigationChanged();
    }

    private void NotifyNavigationChanged()
    {
        OnPropertyChanged(nameof(HistoryCount));
        OnPropertyChanged(nameof(HistoryPositionDisplay));
        OnPropertyChanged(nameof(CanGoHistoryUp));
        OnPropertyChanged(nameof(CanGoHistoryDown));
        OnPropertyChanged(nameof(CanGoPreviousTool));
        OnPropertyChanged(nameof(CanGoNextTool));
        OnPropertyChanged(nameof(CurrentToolIndex));
        OnPropertyChanged(nameof(HistoricalInvokedAt));
    }

    /// <summary>
    /// Closes the dialog and stops the instance.
    /// </summary>
    [RelayCommand]
    private async Task CloseAsync()
    {
        // Save draft before closing
        if (Tool != null && InputValues.Count > 0)
        {
            await _historyService.SaveDraftAsync(ServerName, Tool.Name, InputValues);
        }

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
