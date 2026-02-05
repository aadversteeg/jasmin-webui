using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Infrastructure.BlazorApp.ViewModels;

/// <summary>
/// ViewModel for the MCP server detail page.
/// </summary>
public partial class ServerDetailViewModel : NavigableViewModelBase
{
    [ObservableProperty]
    private string _serverName = string.Empty;

    /// <inheritdoc />
    protected override Task LoadDataAsync()
    {
        // Placeholder: will load server details and tools from API in the future
        return Task.CompletedTask;
    }
}
