using System.Collections.ObjectModel;
using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.McpServers;
using Core.Application.Storage;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for the prompt invocation dialog.
/// </summary>
public partial class PromptInvocationViewModel : ViewModelBase
{
    private readonly IPromptInvocationService _invocationService;
    private readonly IPromptHistoryService _historyService;
    private readonly IUserPreferencesService _preferences;
    private readonly IApplicationStateService _appState;

    // Track whether we started the current instance (vs. reusing existing)
    private bool _instanceStartedByUs;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private InstanceLifecycleMode _lifecycleMode = InstanceLifecycleMode.PerDialog;

    [ObservableProperty]
    private string? _selectedExistingInstanceId;

    [ObservableProperty]
    private InvocationView _currentView = InvocationView.Input;

    [ObservableProperty]
    private McpServerPrompt? _prompt;

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
    private PromptInvocationResult? _result;

    [ObservableProperty]
    private DateTime? _lastInvokedAt;

    [ObservableProperty]
    private int _historyPosition = -1; // -1 = draft, 0..N = history entries (0 = oldest)

    private CancellationTokenSource? _cancellationTokenSource;
    private IReadOnlyList<McpServerPrompt> _availablePrompts = Array.Empty<McpServerPrompt>();
    private int _currentPromptIndex;

    /// <summary>
    /// Argument values for the prompt.
    /// Key is argument name, value is the string value.
    /// </summary>
    public Dictionary<string, string?> ArgumentValues { get; } = new();

    /// <summary>
    /// Gets the available prompts for navigation.
    /// </summary>
    public IReadOnlyList<McpServerPrompt> AvailablePrompts => _availablePrompts;

    /// <summary>
    /// Gets the running instances available for reuse.
    /// </summary>
    public ObservableCollection<McpServerInstance> RunningInstances { get; } = new();

    /// <summary>
    /// Gets the current prompt index in the available prompts list.
    /// </summary>
    public int CurrentPromptIndex => _currentPromptIndex;

    /// <summary>
    /// Gets the total number of history entries for the current prompt.
    /// </summary>
    public int HistoryCount => Prompt != null ? _historyService.GetHistory(ServerName, Prompt.Name).Count : 0;

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
                return $"new ({count})";
            }
            return $"{HistoryPosition + 1}/{count}";
        }
    }

    /// <summary>
    /// Gets whether we can navigate up (to older history).
    /// </summary>
    public bool CanGoHistoryUp => HistoryCount > 0 && (HistoryPosition == -1 || HistoryPosition > 0);

    /// <summary>
    /// Gets whether we can navigate down (to newer history or draft).
    /// </summary>
    public bool CanGoHistoryDown => HistoryPosition >= 0;

    /// <summary>
    /// Gets whether we can navigate to the previous prompt.
    /// </summary>
    public bool CanGoPreviousPrompt => _availablePrompts.Count > 1 && _currentPromptIndex > 0;

    /// <summary>
    /// Gets whether we can navigate to the next prompt.
    /// </summary>
    public bool CanGoNextPrompt => _availablePrompts.Count > 1 && _currentPromptIndex < _availablePrompts.Count - 1;

    /// <summary>
    /// Gets the invocation timestamp when viewing a history entry, null otherwise.
    /// </summary>
    public DateTime? HistoricalInvokedAt
    {
        get
        {
            if (HistoryPosition < 0 || Prompt == null) return null;
            var history = _historyService.GetHistory(ServerName, Prompt.Name);
            if (HistoryPosition < history.Count)
                return history[HistoryPosition].InvokedAt;
            return null;
        }
    }

    public PromptInvocationViewModel(
        IPromptInvocationService invocationService,
        IPromptHistoryService historyService,
        IUserPreferencesService preferences,
        IApplicationStateService appState)
    {
        _invocationService = invocationService;
        _historyService = historyService;
        _preferences = preferences;
        _appState = appState;
    }

    /// <summary>
    /// Opens the dialog to invoke a prompt.
    /// </summary>
    public async Task OpenAsync(McpServerPrompt prompt, string serverName, string? serverUrl, IReadOnlyList<McpServerPrompt>? availablePrompts = null)
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

        // Load preferences if not loaded
        if (!_preferences.IsLoaded)
        {
            await _preferences.LoadAsync();
        }

        // Reset state
        ServerName = serverName;
        ServerUrl = serverUrl;
        _availablePrompts = availablePrompts ?? new List<McpServerPrompt> { prompt };
        _currentPromptIndex = FindPromptIndex(_availablePrompts, prompt);
        if (_currentPromptIndex < 0) _currentPromptIndex = 0;

        CurrentView = InvocationView.Input;
        ErrorMessage = null;
        Result = null;
        InstanceId = null;
        _instanceStartedByUs = false;

        // Set prompt and load draft
        await SwitchToPromptAsync(prompt);

        IsOpen = true;
        _cancellationTokenSource = new CancellationTokenSource();

        // Load running instances for selector
        await LoadRunningInstancesAsync();

        // Load saved preferences
        LifecycleMode = _preferences.GetInstanceLifecycleMode(ServerName);
        SelectedExistingInstanceId = _preferences.GetSelectedInstanceId(ServerName);

        // Validate selected instance still exists
        if (LifecycleMode == InstanceLifecycleMode.ExistingInstance)
        {
            if (string.IsNullOrEmpty(SelectedExistingInstanceId) ||
                !RunningInstances.Any(i => i.InstanceId == SelectedExistingInstanceId))
            {
                // Instance no longer exists, fall back to default
                LifecycleMode = InstanceLifecycleMode.PerDialog;
                SelectedExistingInstanceId = null;
                _preferences.SetInstanceLifecycleMode(ServerName, LifecycleMode);
                _preferences.SetSelectedInstanceId(ServerName, null);
            }
        }

        // DON'T start instance here - wait for invoke

        // Notify navigation state changed
        NotifyNavigationChanged();
    }

    private async Task LoadRunningInstancesAsync()
    {
        RunningInstances.Clear();

        try
        {
            var result = await _invocationService.GetInstancesAsync(
                ServerUrl,
                ServerName,
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var instance in result.Value)
                {
                    RunningInstances.Add(instance);
                }
            }
        }
        catch
        {
            // Ignore errors when loading instances
        }
    }

    private async Task SwitchToPromptAsync(McpServerPrompt prompt)
    {
        // Save current draft if there's a current prompt
        if (Prompt != null && ArgumentValues.Count > 0)
        {
            await _historyService.SaveDraftAsync(ServerName, Prompt.Name, ArgumentValues);
        }

        Prompt = prompt;
        HistoryPosition = -1;
        ArgumentValues.Clear();

        // Try to load draft
        var draft = _historyService.GetDraft(ServerName, prompt.Name);
        if (draft != null)
        {
            foreach (var kvp in draft)
            {
                ArgumentValues[kvp.Key] = kvp.Value;
            }
        }

        // Clear output state when switching prompts
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
                InstanceId = result.InstanceId;
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
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
    /// Sets an argument value.
    /// Clears output when user edits arguments (entering draft mode).
    /// </summary>
    public void SetArgumentValue(string argumentName, string? value)
    {
        ArgumentValues[argumentName] = value;

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
    /// Invokes the prompt with the current argument values.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInvoke))]
    private async Task InvokeAsync()
    {
        if (Prompt == null)
        {
            return;
        }

        IsInvoking = true;
        ErrorMessage = null;
        Result = null;

        try
        {
            // Start instance if needed
            if (string.IsNullOrEmpty(InstanceId))
            {
                if (LifecycleMode == InstanceLifecycleMode.ExistingInstance &&
                    !string.IsNullOrEmpty(SelectedExistingInstanceId))
                {
                    // Reuse existing instance
                    InstanceId = SelectedExistingInstanceId;
                    _instanceStartedByUs = false;
                }
                else
                {
                    // Start a new instance
                    await StartInstanceAsync();
                    if (string.IsNullOrEmpty(InstanceId))
                    {
                        IsInvoking = false;
                        return; // Failed to start
                    }
                    _instanceStartedByUs = true;
                }
            }

            // Build arguments, filtering out null/empty values for optional args
            var arguments = new Dictionary<string, string?>();
            if (Prompt.Arguments != null)
            {
                foreach (var arg in Prompt.Arguments)
                {
                    if (ArgumentValues.TryGetValue(arg.Name, out var value) && !string.IsNullOrEmpty(value))
                    {
                        arguments[arg.Name] = value;
                    }
                    else if (arg.Required)
                    {
                        ErrorMessage = $"Required argument '{arg.Name}' is missing";
                        IsInvoking = false;
                        return;
                    }
                }
            }

            var result = await _invocationService.GetPromptAsync(
                ServerUrl,
                ServerName,
                InstanceId,
                Prompt.Name,
                arguments.Count > 0 ? arguments : null,
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            // If invocation failed and we were using an existing instance, try starting a new one
            if (!result.IsSuccess && !_instanceStartedByUs)
            {
                // Clear the invalid instance and start a new one
                InstanceId = null;
                await StartInstanceAsync();
                if (!string.IsNullOrEmpty(InstanceId))
                {
                    _instanceStartedByUs = true;
                    // Retry invocation with the new instance
                    result = await _invocationService.GetPromptAsync(
                        ServerUrl,
                        ServerName,
                        InstanceId,
                        Prompt.Name,
                        arguments.Count > 0 ? arguments : null,
                        _cancellationTokenSource?.Token ?? CancellationToken.None);
                }
            }

            if (result.IsSuccess)
            {
                Result = result.Value;
                LastInvokedAt = null;
                CurrentView = InvocationView.Output;

                // Add to history and clear draft
                await _historyService.AddEntryAsync(ServerName, Prompt.Name, ArgumentValues, result.Value);
                await _historyService.ClearDraftAsync(ServerName, Prompt.Name);

                // Stay at the newest history entry
                HistoryPosition = HistoryCount - 1;
                NotifyNavigationChanged();
            }
            else
            {
                ErrorMessage = result.Error;
            }

            // Stop instance if PerInvocation mode and we started it
            if (LifecycleMode == InstanceLifecycleMode.PerInvocation && _instanceStartedByUs)
            {
                await StopCurrentInstanceAsync();
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

    private async Task StopCurrentInstanceAsync()
    {
        if (string.IsNullOrEmpty(InstanceId))
            return;

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
        finally
        {
            InstanceId = null;
            _instanceStartedByUs = false;
        }
    }

    private bool CanInvoke() => !IsStartingInstance && !IsInvoking;

    partial void OnIsStartingInstanceChanged(bool value)
    {
        InvokeCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsInvokingChanged(bool value)
    {
        InvokeCommand.NotifyCanExecuteChanged();
    }

    private static int FindPromptIndex(IReadOnlyList<McpServerPrompt> prompts, McpServerPrompt prompt)
    {
        for (int i = 0; i < prompts.Count; i++)
        {
            if (prompts[i].Name == prompt.Name)
            {
                return i;
            }
        }
        return -1;
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
        if (!CanGoHistoryUp || Prompt == null) return;

        // Save draft before navigating away from it
        if (HistoryPosition == -1)
        {
            await _historyService.SaveDraftAsync(ServerName, Prompt.Name, ArgumentValues);
            HistoryPosition = HistoryCount - 1;
        }
        else
        {
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
        if (!CanGoHistoryDown || Prompt == null) return;

        if (HistoryPosition >= HistoryCount - 1)
        {
            HistoryPosition = -1;
            LoadDraft();
        }
        else
        {
            HistoryPosition++;
            LoadHistoryEntry();
        }
        NotifyNavigationChanged();
    }

    private void LoadHistoryEntry()
    {
        if (Prompt == null) return;

        var history = _historyService.GetHistory(ServerName, Prompt.Name);
        if (HistoryPosition >= 0 && HistoryPosition < history.Count)
        {
            var entry = history[HistoryPosition];
            ArgumentValues.Clear();
            foreach (var kvp in entry.ArgumentValues)
            {
                ArgumentValues[kvp.Key] = kvp.Value;
            }
            Result = entry.Output;
        }
    }

    private void LoadDraft()
    {
        if (Prompt == null) return;

        ArgumentValues.Clear();
        var draft = _historyService.GetDraft(ServerName, Prompt.Name);
        if (draft != null)
        {
            foreach (var kvp in draft)
            {
                ArgumentValues[kvp.Key] = kvp.Value;
            }
        }

        Result = null;
        LastInvokedAt = null;
    }

    /// <summary>
    /// Navigate to the previous prompt in the list.
    /// </summary>
    [RelayCommand]
    private async Task PreviousPromptAsync()
    {
        if (!CanGoPreviousPrompt) return;

        _currentPromptIndex--;
        await SwitchToPromptAsync(_availablePrompts[_currentPromptIndex]);
    }

    /// <summary>
    /// Navigate to the next prompt in the list.
    /// </summary>
    [RelayCommand]
    private async Task NextPromptAsync()
    {
        if (!CanGoNextPrompt) return;

        _currentPromptIndex++;
        await SwitchToPromptAsync(_availablePrompts[_currentPromptIndex]);
    }

    /// <summary>
    /// Clear the current draft and reset to defaults.
    /// </summary>
    [RelayCommand]
    private async Task ClearDraftAsync()
    {
        if (Prompt == null) return;

        await _historyService.ClearDraftAsync(ServerName, Prompt.Name);
        ArgumentValues.Clear();

        Result = null;
        LastInvokedAt = null;

        HistoryPosition = -1;
        NotifyNavigationChanged();
    }

    /// <summary>
    /// Clear all history for the current prompt.
    /// </summary>
    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        if (Prompt == null) return;

        await _historyService.ClearHistoryAsync(ServerName, Prompt.Name);

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
        OnPropertyChanged(nameof(CanGoPreviousPrompt));
        OnPropertyChanged(nameof(CanGoNextPrompt));
        OnPropertyChanged(nameof(CurrentPromptIndex));
        OnPropertyChanged(nameof(HistoricalInvokedAt));
    }

    /// <summary>
    /// Closes the dialog and stops the instance.
    /// </summary>
    [RelayCommand]
    private async Task CloseAsync()
    {
        // Save draft before closing
        if (Prompt != null && ArgumentValues.Count > 0)
        {
            await _historyService.SaveDraftAsync(ServerName, Prompt.Name, ArgumentValues);
        }

        // Save lifecycle preferences
        _preferences.SetInstanceLifecycleMode(ServerName, LifecycleMode);
        if (LifecycleMode == InstanceLifecycleMode.ExistingInstance)
        {
            _preferences.SetSelectedInstanceId(ServerName, SelectedExistingInstanceId);
        }

        _cancellationTokenSource?.Cancel();

        // Only stop instance if PerDialog mode and we started it
        if (LifecycleMode == InstanceLifecycleMode.PerDialog &&
            _instanceStartedByUs &&
            !string.IsNullOrEmpty(InstanceId))
        {
            await StopCurrentInstanceAsync();
        }

        // For Persistent and ExistingInstance modes, don't stop the instance

        IsOpen = false;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    /// <summary>
    /// Sets the lifecycle mode and saves preference.
    /// </summary>
    public void SetLifecycleMode(InstanceLifecycleMode mode)
    {
        LifecycleMode = mode;
        _preferences.SetInstanceLifecycleMode(ServerName, mode);

        // Clear instance if switching away from ExistingInstance mode
        if (mode != InstanceLifecycleMode.ExistingInstance)
        {
            SelectedExistingInstanceId = null;
            _preferences.SetSelectedInstanceId(ServerName, null);
        }
    }

    /// <summary>
    /// Sets the selected existing instance and saves preference.
    /// </summary>
    public void SetSelectedExistingInstanceId(string? instanceId)
    {
        SelectedExistingInstanceId = instanceId;
        if (instanceId != null)
        {
            LifecycleMode = InstanceLifecycleMode.ExistingInstance;
            _preferences.SetInstanceLifecycleMode(ServerName, LifecycleMode);
        }
        _preferences.SetSelectedInstanceId(ServerName, instanceId);
    }
}
