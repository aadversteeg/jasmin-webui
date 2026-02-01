namespace Core.Infrastructure.BlazorApp.Components;

public record FilterPanelItem(
    string Id,
    string Label,
    bool IsSelected,
    Action<bool> OnSelectionChanged,
    bool IsDeleted = false
);

public record FilterPanelGroup(
    string Name,
    IEnumerable<FilterPanelItem> Items,
    Action? OnSelectAll = null,
    Action? OnDeselectAll = null
);
