using Core.Application.Storage;
using Core.Infrastructure.BlazorApp.ViewModels;
using FluentAssertions;
using Moq;
using Tests.Infrastructure.BlazorApp.Helpers;
using Xunit;

namespace Tests.Infrastructure.BlazorApp.ViewModels;

public class SidePanelViewModelTests : IDisposable
{
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly SidePanelViewModel _sut;

    public SidePanelViewModelTests()
    {
        _localStorageMock = new Mock<ILocalStorageService>();
        _sut = new SidePanelViewModel(_localStorageMock.Object);
    }

    [Fact(DisplayName = "SPV-001: IsPanelOpen should default to false")]
    public void SPV001()
    {
        _sut.IsPanelOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "SPV-002: PanelWidth should default to 400")]
    public void SPV002()
    {
        _sut.PanelWidth.Should().Be(400);
    }

    [Fact(DisplayName = "SPV-003: TogglePanelCommand should toggle IsPanelOpen")]
    public void SPV003()
    {
        // Act
        _sut.TogglePanelCommand.Execute(null);

        // Assert
        _sut.IsPanelOpen.Should().BeTrue();

        // Act again
        _sut.TogglePanelCommand.Execute(null);

        // Assert
        _sut.IsPanelOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "SPV-004: ClosePanelCommand should set IsPanelOpen to false")]
    public void SPV004()
    {
        // Arrange
        _sut.TogglePanelCommand.Execute(null); // Open panel

        // Act
        _sut.ClosePanelCommand.Execute(null);

        // Assert
        _sut.IsPanelOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "SPV-005: SetWidth should clamp to min 200")]
    public void SPV005()
    {
        // Act
        _sut.SetWidth(100);

        // Assert
        _sut.PanelWidth.Should().Be(200);
    }

    [Fact(DisplayName = "SPV-006: SetWidth should clamp to max 800")]
    public void SPV006()
    {
        // Act
        _sut.SetWidth(1000);

        // Assert
        _sut.PanelWidth.Should().Be(800);
    }

    [Fact(DisplayName = "SPV-007: SetWidth should accept values in valid range")]
    public void SPV007()
    {
        // Act
        _sut.SetWidth(500);

        // Assert
        _sut.PanelWidth.Should().Be(500);
    }

    [Fact(DisplayName = "SPV-008: PanelWidth change should persist to localStorage")]
    public async Task SPV008()
    {
        // Act
        _sut.SetWidth(450);

        // Assert
        await Task.Delay(50);
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:panel-width", 450),
            Times.Once);
    }

    [Fact(DisplayName = "SPV-009: IsPanelOpen change should persist to localStorage")]
    public async Task SPV009()
    {
        // Act
        _sut.TogglePanelCommand.Execute(null);

        // Assert
        await Task.Delay(50);
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:panel-open", true),
            Times.Once);
    }

    [Fact(DisplayName = "SPV-010: OnInitializedAsync should load saved width")]
    public async Task SPV010()
    {
        // Arrange
        _localStorageMock
            .Setup(x => x.GetAsync<int?>("jasmin-webui:panel-width"))
            .ReturnsAsync(500);

        // Act
        await _sut.OnInitializedAsync();

        // Assert
        _sut.PanelWidth.Should().Be(500);
    }

    [Fact(DisplayName = "SPV-011: OnInitializedAsync should load saved open state")]
    public async Task SPV011()
    {
        // Arrange
        _localStorageMock
            .Setup(x => x.GetAsync<bool?>("jasmin-webui:panel-open"))
            .ReturnsAsync(true);

        // Act
        await _sut.OnInitializedAsync();

        // Assert
        _sut.IsPanelOpen.Should().BeTrue();
    }

    [Fact(DisplayName = "SPV-012: OnInitializedAsync should ignore invalid saved width (too small)")]
    public async Task SPV012()
    {
        // Arrange
        _localStorageMock
            .Setup(x => x.GetAsync<int?>("jasmin-webui:panel-width"))
            .ReturnsAsync(50);

        // Act
        await _sut.OnInitializedAsync();

        // Assert - should keep default
        _sut.PanelWidth.Should().Be(400);
    }

    [Fact(DisplayName = "SPV-013: OnInitializedAsync should ignore invalid saved width (too large)")]
    public async Task SPV013()
    {
        // Arrange
        _localStorageMock
            .Setup(x => x.GetAsync<int?>("jasmin-webui:panel-width"))
            .ReturnsAsync(1500);

        // Act
        await _sut.OnInitializedAsync();

        // Assert - should keep default
        _sut.PanelWidth.Should().Be(400);
    }

    [Fact(DisplayName = "SPV-014: IsPanelOpen change should raise PropertyChanged")]
    public void SPV014()
    {
        // Arrange
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.TogglePanelCommand.Execute(null);

        // Assert
        tracker.HasChanged(nameof(SidePanelViewModel.IsPanelOpen)).Should().BeTrue();
    }

    [Fact(DisplayName = "SPV-015: PanelWidth change should raise PropertyChanged")]
    public void SPV015()
    {
        // Arrange
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.SetWidth(500);

        // Assert
        tracker.HasChanged(nameof(SidePanelViewModel.PanelWidth)).Should().BeTrue();
    }

    [Fact(DisplayName = "SPV-016: PanelTitle should default to 'Details'")]
    public void SPV016()
    {
        _sut.PanelTitle.Should().Be("Details");
    }

    public void Dispose()
    {
        // No disposal needed for this ViewModel
    }
}
