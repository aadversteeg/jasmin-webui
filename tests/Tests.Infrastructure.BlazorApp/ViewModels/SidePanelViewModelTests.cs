using Core.Application.Storage;
using Core.Infrastructure.BlazorApp.ViewModels;
using FluentAssertions;
using Moq;
using Tests.Infrastructure.BlazorApp.Helpers;
using Xunit;

namespace Tests.Infrastructure.BlazorApp.ViewModels;

public class SidePanelViewModelTests : IDisposable
{
    private readonly Mock<IUserPreferencesService> _preferencesMock;
    private readonly SidePanelViewModel _sut;

    public SidePanelViewModelTests()
    {
        _preferencesMock = new Mock<IUserPreferencesService>();
        _sut = new SidePanelViewModel(_preferencesMock.Object);
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

    [Fact(DisplayName = "SPV-008: PanelWidth change should persist to preferences")]
    public void SPV008()
    {
        // Act
        _sut.SetWidth(450);

        // Assert
        _preferencesMock.VerifySet(x => x.PanelWidth = 450, Times.Once);
    }

    [Fact(DisplayName = "SPV-009: IsPanelOpen change should persist to preferences")]
    public void SPV009()
    {
        // Act
        _sut.TogglePanelCommand.Execute(null);

        // Assert
        _preferencesMock.VerifySet(x => x.IsPanelOpen = true, Times.Once);
    }

    [Fact(DisplayName = "SPV-010: OnInitializedAsync should load saved width")]
    public async Task SPV010()
    {
        // Arrange
        _preferencesMock.Setup(x => x.PanelWidth).Returns(500);

        // Act
        await _sut.OnInitializedAsync();

        // Assert
        _sut.PanelWidth.Should().Be(500);
    }

    [Fact(DisplayName = "SPV-011: OnInitializedAsync should load saved open state")]
    public async Task SPV011()
    {
        // Arrange
        _preferencesMock.Setup(x => x.IsPanelOpen).Returns(true);

        // Act
        await _sut.OnInitializedAsync();

        // Assert
        _sut.IsPanelOpen.Should().BeTrue();
    }

    [Fact(DisplayName = "SPV-012: OnInitializedAsync should call LoadAsync on preferences")]
    public async Task SPV012()
    {
        // Act
        await _sut.OnInitializedAsync();

        // Assert
        _preferencesMock.Verify(x => x.LoadAsync(), Times.Once);
    }

    [Fact(DisplayName = "SPV-013: OnInitializedAsync should use default when preferences not loaded")]
    public async Task SPV013()
    {
        // Arrange - preferences returns defaults
        _preferencesMock.Setup(x => x.PanelWidth).Returns(400);

        // Act
        await _sut.OnInitializedAsync();

        // Assert
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
