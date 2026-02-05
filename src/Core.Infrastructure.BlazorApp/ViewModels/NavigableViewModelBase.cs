using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// Base ViewModel for pages that load data asynchronously on navigation.
/// Provides IsLoading and LoadError properties, and a standardized data loading lifecycle.
/// </summary>
public abstract partial class NavigableViewModelBase : ViewModelBase
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _loadError;

    /// <summary>
    /// Called when the page navigates to this ViewModel.
    /// Implementations should load data from APIs or services.
    /// </summary>
    protected abstract Task LoadDataAsync();

    /// <summary>
    /// Triggers the data loading lifecycle with error handling.
    /// Call this from OnParametersSetAsync or when route params change.
    /// </summary>
    public async Task InitializeDataAsync()
    {
        IsLoading = true;
        LoadError = null;

        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            LoadError = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
