using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.Storage;

namespace Core.Infrastructure.BlazorApp.ViewModels;

public partial class SidePanelViewModel : ViewModelBase
{
    private readonly IUserPreferencesService _preferences;
    private const int MinWidth = 200;
    private const int MaxWidth = 800;

    [ObservableProperty]
    private bool _isPanelOpen;

    [ObservableProperty]
    private int _panelWidth = 400;

    [ObservableProperty]
    private string _panelTitle = "Details";

    public SidePanelViewModel(IUserPreferencesService preferences)
    {
        _preferences = preferences;
    }

    public override async Task OnInitializedAsync()
    {
        await _preferences.LoadAsync();
        PanelWidth = _preferences.PanelWidth;
        IsPanelOpen = _preferences.IsPanelOpen;
    }

    partial void OnPanelWidthChanged(int value)
    {
        _preferences.PanelWidth = value;
    }

    partial void OnIsPanelOpenChanged(bool value)
    {
        _preferences.IsPanelOpen = value;
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
