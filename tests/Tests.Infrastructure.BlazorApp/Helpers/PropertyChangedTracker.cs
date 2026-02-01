using System.ComponentModel;

namespace Tests.Infrastructure.BlazorApp.Helpers;

/// <summary>
/// Helper class to track property change notifications in tests.
/// </summary>
public class PropertyChangedTracker : IDisposable
{
    private readonly INotifyPropertyChanged _target;
    private readonly List<string> _changedProperties = new();

    public PropertyChangedTracker(INotifyPropertyChanged target)
    {
        _target = target;
        _target.PropertyChanged += OnPropertyChanged;
    }

    public IReadOnlyList<string> ChangedProperties => _changedProperties;

    public bool HasChanged(string propertyName) =>
        _changedProperties.Contains(propertyName);

    public int ChangeCount(string propertyName) =>
        _changedProperties.Count(p => p == propertyName);

    public void Clear() => _changedProperties.Clear();

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        _changedProperties.Add(e.PropertyName!);

    public void Dispose() =>
        _target.PropertyChanged -= OnPropertyChanged;
}
