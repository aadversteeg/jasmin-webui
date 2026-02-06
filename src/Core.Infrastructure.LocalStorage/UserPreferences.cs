namespace Core.Infrastructure.LocalStorage;

/// <summary>
/// User preferences for the application.
/// </summary>
public record UserPreferences
{
    // Right Panel (Filters)
    public bool IsPanelOpen { get; init; } = false;
    public int PanelWidth { get; init; } = 400;

    // Left Panel
    public bool IsLeftPanelOpen { get; init; } = false;
    public int LeftPanelWidth { get; init; } = 300;

    // Filters
    public List<string> KnownServers { get; init; } = new();
    public List<string> SelectedServers { get; init; } = new();
    public List<int> EnabledEventTypes { get; init; } = new();
    public bool IsServerFilterExpanded { get; init; } = true;
    public bool IsEventTypeFilterExpanded { get; init; } = true;

    // Configuration view
    public bool ShowConfigAsJson { get; init; } = false;

    // Tool invocation dialog
    public int ToolInvocationInputPanelWidthPercent { get; init; } = 33;
    public int ToolInvocationHistoryMaxItems { get; init; } = 20;
}
