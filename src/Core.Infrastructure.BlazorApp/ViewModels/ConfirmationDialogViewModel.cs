using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for a reusable confirmation dialog.
/// Callers use <see cref="ShowAsync"/> to display the dialog and await the user's decision.
/// </summary>
public partial class ConfirmationDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _confirmButtonText = "Confirm";

    [ObservableProperty]
    private string _confirmButtonStyle = "danger";

    private TaskCompletionSource<bool>? _tcs;

    /// <summary>
    /// Shows the confirmation dialog and awaits the user's decision.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The confirmation message.</param>
    /// <param name="confirmButtonText">Text for the confirm button (default: "Delete").</param>
    /// <param name="confirmButtonStyle">Bootstrap button style suffix (default: "danger").</param>
    /// <returns>True if confirmed, false if cancelled.</returns>
    public Task<bool> ShowAsync(
        string title,
        string message,
        string confirmButtonText = "Delete",
        string confirmButtonStyle = "danger")
    {
        Title = title;
        Message = message;
        ConfirmButtonText = confirmButtonText;
        ConfirmButtonStyle = confirmButtonStyle;
        _tcs = new TaskCompletionSource<bool>();
        IsOpen = true;
        return _tcs.Task;
    }

    [RelayCommand]
    private void Confirm()
    {
        IsOpen = false;
        _tcs?.TrySetResult(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        IsOpen = false;
        _tcs?.TrySetResult(false);
    }
}
