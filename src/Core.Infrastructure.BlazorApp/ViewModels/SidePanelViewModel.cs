using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Application.Storage;

namespace Core.Infrastructure.BlazorApp.ViewModels;

public partial class SidePanelViewModel : ViewModelBase
{
    private readonly ILocalStorageService _localStorage;
    private const string PanelWidthKey = "jasmin-webui:panel-width";
    private const string PanelOpenKey = "jasmin-webui:panel-open";
    private const int DefaultWidth = 400;
    private const int MinWidth = 200;
    private const int MaxWidth = 800;

    [ObservableProperty]
    private bool _isPanelOpen;

    [ObservableProperty]
    private int _panelWidth = DefaultWidth;

    [ObservableProperty]
    private string _panelTitle = "Details";

    public SidePanelViewModel(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task OnInitializedAsync()
    {
        var savedWidth = await _localStorage.GetAsync<int?>(PanelWidthKey);
        if (savedWidth.HasValue && savedWidth.Value >= MinWidth && savedWidth.Value <= MaxWidth)
        {
            PanelWidth = savedWidth.Value;
        }

        var savedOpen = await _localStorage.GetAsync<bool?>(PanelOpenKey);
        if (savedOpen.HasValue)
        {
            IsPanelOpen = savedOpen.Value;
        }
    }

    partial void OnPanelWidthChanged(int value)
    {
        _ = _localStorage.SetAsync(PanelWidthKey, value);
    }

    partial void OnIsPanelOpenChanged(bool value)
    {
        _ = _localStorage.SetAsync(PanelOpenKey, value);
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
