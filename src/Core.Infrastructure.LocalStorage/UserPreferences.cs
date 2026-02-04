namespace Core.Infrastructure.LocalStorage;

/// <summary>
/// User preferences for the application.
/// </summary>
public record UserPreferences
{
    // Side Panel
    public bool IsPanelOpen { get; init; } = false;
    public int PanelWidth { get; init; } = 400;

    // Filters
    public List<string> KnownServers { get; init; } = new();
    public List<string> SelectedServers { get; init; } = new();
    public List<int> EnabledEventTypes { get; init; } = new();
    public bool IsServerFilterExpanded { get; init; } = true;
    public bool IsEventTypeFilterExpanded { get; init; } = true;

    // Connection
    public string? ServerUrl { get; init; }
    public string? LastEventId { get; init; }
}
