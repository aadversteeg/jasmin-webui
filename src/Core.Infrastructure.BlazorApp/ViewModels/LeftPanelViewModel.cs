using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.Storage;

namespace Core.Infrastructure.BlazorApp.ViewModels;

public partial class LeftPanelViewModel : ViewModelBase
{
    private readonly IUserPreferencesService _preferences;
    private const int MinWidth = 200;
    private const int MaxWidth = 800;

    [ObservableProperty]
    private bool _isPanelOpen;

    [ObservableProperty]
    private int _panelWidth = 300;

    public LeftPanelViewModel(IUserPreferencesService preferences)
    {
        _preferences = preferences;
    }

    public override async Task OnInitializedAsync()
    {
        await _preferences.LoadAsync();
        PanelWidth = _preferences.LeftPanelWidth;
        IsPanelOpen = _preferences.IsLeftPanelOpen;
    }

    partial void OnPanelWidthChanged(int value)
    {
        _preferences.LeftPanelWidth = value;
    }

    partial void OnIsPanelOpenChanged(bool value)
    {
        _preferences.IsLeftPanelOpen = value;
    }

    [RelayCommand]
    private void TogglePanel()
    {
        IsPanelOpen = !IsPanelOpen;
    }

    [RelayCommand]
    private void ClosePanel()
    {
        IsPanelOpen = false;
    }

    public void SetWidth(int width)
    {
        PanelWidth = Math.Clamp(width, MinWidth, MaxWidth);
    }
}
