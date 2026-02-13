using System.Collections.ObjectModel;
using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.Events;
using Core.Application.McpServers;
using Core.Application.Storage;
using Core.Domain.Events;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for the instance management dialog with log viewer.
/// </summary>
public partial class InstanceManagementViewModel : ViewModelBase
{
    private const int MaxEvents = 1000;

    private readonly IToolInvocationService _invocationService;
    private readonly IApplicationStateService _appState;
    private readonly IEventStreamService _eventStream;
    private readonly IInstanceLogService _logService;
    private readonly EventViewerViewModel _eventViewer;
    private readonly List<McpServerEvent> _serverEvents = new();

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

    [ObservableProperty]
    private string? _selectedInstanceId;

    [ObservableProperty]
    private bool _isLogStreamConnected;

    [ObservableProperty]
    private string? _logStreamError;

    /// <summary>
    /// The list of running instances.
    /// </summary>
    public ObservableCollection<McpServerInstance> Instances { get; } = new();

    /// <summary>
    /// The log entries for the selected instance.
    /// </summary>
    public ObservableCollection<InstanceLogEntry> LogEntries { get; } = new();

    /// <summary>
    /// Event raised when instances are updated via SSE events.
    /// </summary>
    public event Action? InstancesChanged;

    /// <summary>
    /// Event raised when new log entries are added (for auto-scroll in the UI).
    /// </summary>
    public event Action? LogEntriesChanged;

    /// <summary>
    /// Child ViewModel for managing expanded state of event cards.
    /// </summary>
    public ExpandableItemsViewModel EventExpandState { get; } = new();

    /// <summary>
    /// Events filtered for the currently selected instance.
    /// </summary>
    public IReadOnlyList<McpServerEvent> InstanceEvents =>
        string.IsNullOrEmpty(SelectedInstanceId)
            ? Array.Empty<McpServerEvent>()
            : _serverEvents.Where(e => e.InstanceId == SelectedInstanceId).ToList();

    /// <summary>
    /// Event raised when new events are received (for auto-scroll in the UI).
    /// </summary>
    public event Action? EventsChanged;

    public InstanceManagementViewModel(
        IToolInvocationService invocationService,
        IApplicationStateService appState,
        IEventStreamService eventStream,
        IInstanceLogService logService,
        EventViewerViewModel eventViewer)
    {
        _invocationService = invocationService;
        _appState = appState;
        _eventStream = eventStream;
        _logService = logService;
        _eventViewer = eventViewer;
    }

    /// <summary>
    /// Opens the dialog for managing instances of a server.
    /// </summary>
    [RelayCommand]
    private async Task OpenAsync(string serverName)
    {
        ServerName = serverName;
        ErrorMessage = null;
        SelectedInstanceId = null;
        LogEntries.Clear();
        Instances.Clear();
        _serverEvents.Clear();
        EventExpandState.CollapseAll();

        // Seed with existing events for this server from the main event viewer
        _serverEvents.AddRange(
            _eventViewer.Events.Where(e =>
                string.Equals(e.ServerName, serverName, StringComparison.OrdinalIgnoreCase)));

        IsOpen = true;

        // Subscribe to events for automatic updates
        _eventStream.EventReceived += OnEventReceived;

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

                // If the stopped instance was selected, clear selection and disconnect log stream
                if (instanceId == SelectedInstanceId)
                {
                    await DisconnectLogStreamAsync();
                    SelectedInstanceId = null;
                    LogEntries.Clear();
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
    /// Selects an instance and starts streaming its logs.
    /// </summary>
    [RelayCommand]
    private async Task SelectInstanceAsync(string instanceId)
    {
        if (SelectedInstanceId == instanceId)
        {
            return;
        }

        // Disconnect previous log stream
        await DisconnectLogStreamAsync();

        SelectedInstanceId = instanceId;
        LogEntries.Clear();
        LogStreamError = null;

        if (!string.IsNullOrEmpty(instanceId))
        {
            await ConnectLogStreamAsync(instanceId);
        }
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    [RelayCommand]
    private async Task CloseAsync()
    {
        // Unsubscribe from events
        _eventStream.EventReceived -= OnEventReceived;

        await DisconnectLogStreamAsync();
        SelectedInstanceId = null;
        LogEntries.Clear();
        _serverEvents.Clear();
        EventExpandState.CollapseAll();
        IsOpen = false;
    }

    private async Task ConnectLogStreamAsync(string instanceId)
    {
        await _appState.LoadAsync();
        var serverUrl = _appState.ServerUrl;
        if (string.IsNullOrEmpty(serverUrl))
        {
            return;
        }

        _logService.LogEntryReceived += OnLogEntryReceived;
        _logService.ErrorOccurred += OnLogErrorOccurred;

        await _logService.StartStreamAsync(serverUrl, ServerName, instanceId, afterLine: 0);
    }

    private async Task DisconnectLogStreamAsync()
    {
        _logService.LogEntryReceived -= OnLogEntryReceived;
        _logService.ErrorOccurred -= OnLogErrorOccurred;

        await _logService.StopStreamAsync();
        IsLogStreamConnected = false;
    }

    private void OnLogEntryReceived(object? sender, InstanceLogEntry entry)
    {
        LogEntries.Add(entry);
        IsLogStreamConnected = true;
        LogEntriesChanged?.Invoke();
    }

    private void OnLogErrorOccurred(object? sender, string error)
    {
        LogStreamError = error;
    }

    private void OnEventReceived(object? sender, McpServerEvent e)
    {
        // Only handle events for the current server
        if (!string.Equals(e.ServerName, ServerName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Store the event for the events tab
        _serverEvents.Add(e);
        while (_serverEvents.Count > MaxEvents)
        {
            _serverEvents.RemoveAt(0);
        }
        EventsChanged?.Invoke();

        switch (e.EventType)
        {
            case McpServerEventType.Started:
                // Add new instance to the list
                if (!string.IsNullOrEmpty(e.InstanceId))
                {
                    var existingInstance = Instances.FirstOrDefault(i => i.InstanceId == e.InstanceId);
                    if (existingInstance == null)
                    {
                        Instances.Add(new McpServerInstance(e.InstanceId, e.ServerName, e.Timestamp));
                        InstancesChanged?.Invoke();
                    }
                }
                break;

            case McpServerEventType.Stopped:
                // Remove instance from the list
                if (!string.IsNullOrEmpty(e.InstanceId))
                {
                    var instance = Instances.FirstOrDefault(i => i.InstanceId == e.InstanceId);
                    if (instance != null)
                    {
                        Instances.Remove(instance);

                        if (e.InstanceId == SelectedInstanceId)
                        {
                            _ = DisconnectLogStreamAsync();
                            SelectedInstanceId = null;
                            LogEntries.Clear();
                        }

                        InstancesChanged?.Invoke();
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Gets a unique identifier for an event.
    /// </summary>
    public static string GetEventId(McpServerEvent evt)
        => $"{evt.Timestamp.Ticks}_{evt.ServerName}_{evt.EventType}";

    /// <summary>
    /// Expands all events for the currently selected instance.
    /// </summary>
    public void ExpandAllInstanceEvents()
        => EventExpandState.ExpandAll(InstanceEvents.Select(GetEventId));

    /// <summary>
    /// Collapses all events.
    /// </summary>
    public void CollapseAllInstanceEvents()
        => EventExpandState.CollapseAll();
}
