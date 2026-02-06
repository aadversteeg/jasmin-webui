using Blazing.Mvvm.ComponentModel;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for managing expanded state of a list of expandable items.
/// Can be used as a standalone ViewModel or embedded as a child ViewModel.
/// </summary>
public class ExpandableItemsViewModel : ViewModelBase
{
    private readonly HashSet<string> _expandedItems = new();

    /// <summary>
    /// Event raised when the expanded state of any item changes.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Checks if an item is currently expanded.
    /// </summary>
    public bool IsExpanded(string name) => _expandedItems.Contains(name);

    /// <summary>
    /// Toggles the expanded state of an item.
    /// </summary>
    public void ToggleExpand(string name)
    {
        if (!_expandedItems.Remove(name))
        {
            _expandedItems.Add(name);
        }
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Expands all items with the given names.
    /// </summary>
    public void ExpandAll(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            _expandedItems.Add(name);
        }
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Collapses all expanded items.
    /// </summary>
    public void CollapseAll()
    {
        _expandedItems.Clear();
        StateChanged?.Invoke();
    }
}
