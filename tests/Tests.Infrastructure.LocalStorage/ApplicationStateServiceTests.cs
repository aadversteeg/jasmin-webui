using Core.Application.Storage;
using Core.Infrastructure.LocalStorage;
using FluentAssertions;
using Moq;
using Xunit;

namespace Tests.Infrastructure.LocalStorage;

public class ApplicationStateServiceTests
{
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly ApplicationStateService _sut;

    public ApplicationStateServiceTests()
    {
        _localStorageMock = new Mock<ILocalStorageService>();
        _sut = new ApplicationStateService(_localStorageMock.Object);
    }

    [Fact(DisplayName = "ASS-001: IsLoaded should be false before LoadAsync is called")]
    public void ASS001()
    {
        _sut.IsLoaded.Should().BeFalse();
    }

    [Fact(DisplayName = "ASS-002: IsLoaded should be true after LoadAsync is called")]
    public async Task ASS002()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsLoaded.Should().BeTrue();
    }

    [Fact(DisplayName = "ASS-003: LoadAsync should only load once")]
    public async Task ASS003()
    {
        // Act
        await _sut.LoadAsync();
        await _sut.LoadAsync();

        // Assert
        _localStorageMock.Verify(
            x => x.GetAsync<ApplicationState>(It.IsAny<string>()),
            Times.Once);
    }

    [Fact(DisplayName = "ASS-004: ServerUrl should default to null")]
    public void ASS004()
    {
        _sut.ServerUrl.Should().BeNull();
    }

    [Fact(DisplayName = "ASS-005: Setting ServerUrl should persist to localStorage")]
    public void ASS005()
    {
        // Act
        _sut.ServerUrl = "http://example.com";

        // Assert
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:app-state",
                It.Is<ApplicationState>(s => s.ServerUrl == "http://example.com")),
            Times.Once);
    }

    [Fact(DisplayName = "ASS-006: LastEventId should default to null")]
    public void ASS006()
    {
        _sut.LastEventId.Should().BeNull();
    }

    [Fact(DisplayName = "ASS-007: Setting LastEventId should persist to localStorage")]
    public void ASS007()
    {
        // Act
        _sut.LastEventId = "evt-456";

        // Assert
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:app-state",
                It.Is<ApplicationState>(s => s.LastEventId == "evt-456")),
            Times.Once);
    }

    [Fact(DisplayName = "ASS-008: LoadAsync should restore saved state")]
    public async Task ASS008()
    {
        // Arrange
        var savedState = new ApplicationState
        {
            ServerUrl = "http://saved-server.com",
            LastEventId = "evt-123"
        };
        _localStorageMock
            .Setup(x => x.GetAsync<ApplicationState>("jasmin-webui:app-state"))
            .ReturnsAsync(savedState);

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.ServerUrl.Should().Be("http://saved-server.com");
        _sut.LastEventId.Should().Be("evt-123");
    }

    [Fact(DisplayName = "ASS-009: StateChanged should fire when property changes")]
    public void ASS009()
    {
        // Arrange
        var eventFired = false;
        _sut.StateChanged += () => eventFired = true;

        // Act
        _sut.ServerUrl = "http://test.com";

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact(DisplayName = "ASS-010: LoadAsync with null state should use defaults")]
    public async Task ASS010()
    {
        // Arrange
        _localStorageMock
            .Setup(x => x.GetAsync<ApplicationState>("jasmin-webui:app-state"))
            .ReturnsAsync((ApplicationState?)null);

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.ServerUrl.Should().BeNull();
        _sut.LastEventId.Should().BeNull();
    }

    [Fact(DisplayName = "ASS-011: StateChanged should fire for all property updates")]
    public void ASS011()
    {
        // Arrange
        var eventCount = 0;
        _sut.StateChanged += () => eventCount++;

        // Act
        _sut.ServerUrl = "http://test.com";
        _sut.LastEventId = "evt-789";

        // Assert
        eventCount.Should().Be(2);
    }

    [Fact(DisplayName = "ASS-012: LoadAsync should migrate from legacy server-url key")]
    public async Task ASS012()
    {
        // Arrange
        _localStorageMock
            .Setup(x => x.GetAsync<ApplicationState>("jasmin-webui:app-state"))
            .ReturnsAsync((ApplicationState?)null);
        _localStorageMock
            .Setup(x => x.GetAsync<string>("jasmin-webui:server-url"))
            .ReturnsAsync("http://legacy-server.com");

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.ServerUrl.Should().Be("http://legacy-server.com");
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:app-state",
                It.Is<ApplicationState>(s => s.ServerUrl == "http://legacy-server.com")),
            Times.Once);
    }

    [Fact(DisplayName = "ASS-013: LoadAsync should not migrate if state already exists")]
    public async Task ASS013()
    {
        // Arrange
        var savedState = new ApplicationState
        {
            ServerUrl = "http://new-server.com"
        };
        _localStorageMock
            .Setup(x => x.GetAsync<ApplicationState>("jasmin-webui:app-state"))
            .ReturnsAsync(savedState);
        _localStorageMock
            .Setup(x => x.GetAsync<string>("jasmin-webui:server-url"))
            .ReturnsAsync("http://legacy-server.com");

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.ServerUrl.Should().Be("http://new-server.com");
        _localStorageMock.Verify(
            x => x.GetAsync<string>("jasmin-webui:server-url"),
            Times.Never);
    }
}
