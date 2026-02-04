using Core.Application.Storage;
using Core.Infrastructure.LocalStorage;
using FluentAssertions;
using Moq;
using Xunit;

namespace Tests.Infrastructure.LocalStorage;

public class UserPreferencesServiceTests
{
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly UserPreferencesService _sut;

    public UserPreferencesServiceTests()
    {
        _localStorageMock = new Mock<ILocalStorageService>();
        _sut = new UserPreferencesService(_localStorageMock.Object);
    }

    [Fact(DisplayName = "UPS-001: IsLoaded should be false before LoadAsync is called")]
    public void UPS001()
    {
        _sut.IsLoaded.Should().BeFalse();
    }

    [Fact(DisplayName = "UPS-002: IsLoaded should be true after LoadAsync is called")]
    public async Task UPS002()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsLoaded.Should().BeTrue();
    }

    [Fact(DisplayName = "UPS-003: LoadAsync should only load once")]
    public async Task UPS003()
    {
        // Act
        await _sut.LoadAsync();
        await _sut.LoadAsync();

        // Assert
        _localStorageMock.Verify(
            x => x.GetAsync<UserPreferences>(It.IsAny<string>()),
            Times.Once);
    }

    [Fact(DisplayName = "UPS-004: IsPanelOpen should default to false")]
    public void UPS004()
    {
        _sut.IsPanelOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "UPS-005: PanelWidth should default to 400")]
    public void UPS005()
    {
        _sut.PanelWidth.Should().Be(400);
    }

    [Fact(DisplayName = "UPS-006: Setting IsPanelOpen should persist to localStorage")]
    public void UPS006()
    {
        // Act
        _sut.IsPanelOpen = true;

        // Assert
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:preferences", It.Is<UserPreferences>(p => p.IsPanelOpen == true)),
            Times.Once);
    }

    [Fact(DisplayName = "UPS-007: Setting PanelWidth should persist to localStorage")]
    public void UPS007()
    {
        // Act
        _sut.PanelWidth = 500;

        // Assert
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:preferences", It.Is<UserPreferences>(p => p.PanelWidth == 500)),
            Times.Once);
    }

    [Fact(DisplayName = "UPS-008: PanelWidth should clamp to minimum 200")]
    public void UPS008()
    {
        // Act
        _sut.PanelWidth = 100;

        // Assert
        _sut.PanelWidth.Should().Be(200);
    }

    [Fact(DisplayName = "UPS-009: PanelWidth should clamp to maximum 800")]
    public void UPS009()
    {
        // Act
        _sut.PanelWidth = 1000;

        // Assert
        _sut.PanelWidth.Should().Be(800);
    }

    [Fact(DisplayName = "UPS-010: LoadAsync should restore saved preferences")]
    public async Task UPS010()
    {
        // Arrange
        var savedPreferences = new UserPreferences
        {
            IsPanelOpen = true,
            PanelWidth = 600,
            IsServerFilterExpanded = false,
            IsEventTypeFilterExpanded = false
        };
        _localStorageMock
            .Setup(x => x.GetAsync<UserPreferences>("jasmin-webui:preferences"))
            .ReturnsAsync(savedPreferences);

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsPanelOpen.Should().BeTrue();
        _sut.PanelWidth.Should().Be(600);
        _sut.IsServerFilterExpanded.Should().BeFalse();
        _sut.IsEventTypeFilterExpanded.Should().BeFalse();
    }

    [Fact(DisplayName = "UPS-011: LoadAsync should clamp saved PanelWidth if too small")]
    public async Task UPS011()
    {
        // Arrange
        var savedPreferences = new UserPreferences { PanelWidth = 50 };
        _localStorageMock
            .Setup(x => x.GetAsync<UserPreferences>("jasmin-webui:preferences"))
            .ReturnsAsync(savedPreferences);

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.PanelWidth.Should().Be(200);
    }

    [Fact(DisplayName = "UPS-012: LoadAsync should clamp saved PanelWidth if too large")]
    public async Task UPS012()
    {
        // Arrange
        var savedPreferences = new UserPreferences { PanelWidth = 1500 };
        _localStorageMock
            .Setup(x => x.GetAsync<UserPreferences>("jasmin-webui:preferences"))
            .ReturnsAsync(savedPreferences);

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.PanelWidth.Should().Be(800);
    }

    [Fact(DisplayName = "UPS-013: PreferencesChanged should fire when property changes")]
    public void UPS013()
    {
        // Arrange
        var eventFired = false;
        _sut.PreferencesChanged += () => eventFired = true;

        // Act
        _sut.IsPanelOpen = true;

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact(DisplayName = "UPS-014: SelectedServers should default to empty")]
    public void UPS014()
    {
        _sut.SelectedServers.Should().BeEmpty();
    }

    [Fact(DisplayName = "UPS-015: SetSelectedServers should persist to localStorage")]
    public void UPS015()
    {
        // Act
        _sut.SetSelectedServers(new[] { "server-a", "server-b" });

        // Assert
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:preferences",
                It.Is<UserPreferences>(p =>
                    p.SelectedServers.Contains("server-a") &&
                    p.SelectedServers.Contains("server-b"))),
            Times.Once);
    }

    [Fact(DisplayName = "UPS-016: SetSelectedServers should update SelectedServers property")]
    public void UPS016()
    {
        // Act
        _sut.SetSelectedServers(new[] { "server-a", "server-b" });

        // Assert
        _sut.SelectedServers.Should().HaveCount(2);
        _sut.SelectedServers.Should().Contain("server-a");
        _sut.SelectedServers.Should().Contain("server-b");
    }

    [Fact(DisplayName = "UPS-017: EnabledEventTypes should default to empty")]
    public void UPS017()
    {
        _sut.EnabledEventTypes.Should().BeEmpty();
    }

    [Fact(DisplayName = "UPS-018: SetEnabledEventTypes should persist to localStorage")]
    public void UPS018()
    {
        // Act
        _sut.SetEnabledEventTypes(new[] { 1, 2, 3 });

        // Assert
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:preferences",
                It.Is<UserPreferences>(p =>
                    p.EnabledEventTypes.Contains(1) &&
                    p.EnabledEventTypes.Contains(2) &&
                    p.EnabledEventTypes.Contains(3))),
            Times.Once);
    }

    [Fact(DisplayName = "UPS-019: SetEnabledEventTypes should update EnabledEventTypes property")]
    public void UPS019()
    {
        // Act
        _sut.SetEnabledEventTypes(new[] { 1, 2, 3 });

        // Assert
        _sut.EnabledEventTypes.Should().HaveCount(3);
        _sut.EnabledEventTypes.Should().Contain(1);
        _sut.EnabledEventTypes.Should().Contain(2);
        _sut.EnabledEventTypes.Should().Contain(3);
    }

    [Fact(DisplayName = "UPS-020: IsServerFilterExpanded should default to true")]
    public void UPS020()
    {
        _sut.IsServerFilterExpanded.Should().BeTrue();
    }

    [Fact(DisplayName = "UPS-021: IsEventTypeFilterExpanded should default to true")]
    public void UPS021()
    {
        _sut.IsEventTypeFilterExpanded.Should().BeTrue();
    }

    [Fact(DisplayName = "UPS-022: Setting IsServerFilterExpanded should persist")]
    public void UPS022()
    {
        // Act
        _sut.IsServerFilterExpanded = false;

        // Assert
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:preferences",
                It.Is<UserPreferences>(p => p.IsServerFilterExpanded == false)),
            Times.Once);
    }

    [Fact(DisplayName = "UPS-023: Setting IsEventTypeFilterExpanded should persist")]
    public void UPS023()
    {
        // Act
        _sut.IsEventTypeFilterExpanded = false;

        // Assert
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:preferences",
                It.Is<UserPreferences>(p => p.IsEventTypeFilterExpanded == false)),
            Times.Once);
    }

    [Fact(DisplayName = "UPS-028: LoadAsync should restore saved servers")]
    public async Task UPS028()
    {
        // Arrange
        var savedPreferences = new UserPreferences
        {
            SelectedServers = new List<string> { "saved-server-1", "saved-server-2" }
        };
        _localStorageMock
            .Setup(x => x.GetAsync<UserPreferences>("jasmin-webui:preferences"))
            .ReturnsAsync(savedPreferences);

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.SelectedServers.Should().HaveCount(2);
        _sut.SelectedServers.Should().Contain("saved-server-1");
        _sut.SelectedServers.Should().Contain("saved-server-2");
    }

    [Fact(DisplayName = "UPS-029: LoadAsync should restore saved event types")]
    public async Task UPS029()
    {
        // Arrange
        var savedPreferences = new UserPreferences
        {
            EnabledEventTypes = new List<int> { 1, 2, 5 }
        };
        _localStorageMock
            .Setup(x => x.GetAsync<UserPreferences>("jasmin-webui:preferences"))
            .ReturnsAsync(savedPreferences);

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.EnabledEventTypes.Should().HaveCount(3);
        _sut.EnabledEventTypes.Should().Contain(1);
        _sut.EnabledEventTypes.Should().Contain(2);
        _sut.EnabledEventTypes.Should().Contain(5);
    }

    [Fact(DisplayName = "UPS-030: PreferencesChanged should fire for all property updates")]
    public void UPS030()
    {
        // Arrange
        var eventCount = 0;
        _sut.PreferencesChanged += () => eventCount++;

        // Act
        _sut.IsPanelOpen = true;
        _sut.PanelWidth = 500;
        _sut.IsServerFilterExpanded = false;
        _sut.IsEventTypeFilterExpanded = false;

        // Assert
        eventCount.Should().Be(4);
    }

    [Fact(DisplayName = "UPS-031: LoadAsync with null preferences should use defaults")]
    public async Task UPS031()
    {
        // Arrange
        _localStorageMock
            .Setup(x => x.GetAsync<UserPreferences>("jasmin-webui:preferences"))
            .ReturnsAsync((UserPreferences?)null);

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsPanelOpen.Should().BeFalse();
        _sut.PanelWidth.Should().Be(400);
        _sut.IsServerFilterExpanded.Should().BeTrue();
        _sut.IsEventTypeFilterExpanded.Should().BeTrue();
        _sut.SelectedServers.Should().BeEmpty();
        _sut.EnabledEventTypes.Should().BeEmpty();
    }
}
