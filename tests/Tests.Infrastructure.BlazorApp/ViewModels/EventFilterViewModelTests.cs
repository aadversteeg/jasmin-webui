using Core.Application.Storage;
using Core.Domain.Events;
using Core.Infrastructure.BlazorApp.ViewModels;
using FluentAssertions;
using Moq;
using Tests.Infrastructure.BlazorApp.Helpers;
using Xunit;

namespace Tests.Infrastructure.BlazorApp.ViewModels;

public class EventFilterViewModelTests
{
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly EventFilterViewModel _sut;

    public EventFilterViewModelTests()
    {
        _localStorageMock = new Mock<ILocalStorageService>();
        _sut = new EventFilterViewModel(_localStorageMock.Object);
    }

    [Fact(DisplayName = "EFV-001: All event types should be enabled by default")]
    public void EFV001()
    {
        // Assert
        _sut.EnabledEventTypes.Should().HaveCount(24);
        foreach (var eventType in Enum.GetValues<McpServerEventType>())
        {
            _sut.IsEventTypeEnabled(eventType).Should().BeTrue();
        }
    }

    [Fact(DisplayName = "EFV-002: SetEventTypeEnabled should raise PropertyChanged for EnabledEventTypes")]
    public void EFV002()
    {
        // Arrange
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.SetEventTypeEnabled(McpServerEventType.Started, false);

        // Assert
        tracker.HasChanged(nameof(EventFilterViewModel.EnabledEventTypes)).Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-003: SetEventTypeEnabled should raise FilterChanged")]
    public void EFV003()
    {
        // Arrange
        var filterChangedRaised = false;
        _sut.FilterChanged += () => filterChangedRaised = true;

        // Act
        _sut.SetEventTypeEnabled(McpServerEventType.Started, false);

        // Assert
        filterChangedRaised.Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-004: SelectedServer property change should raise PropertyChanged")]
    public void EFV004()
    {
        // Arrange
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.SelectedServer = "test-server";

        // Assert
        tracker.HasChanged(nameof(EventFilterViewModel.SelectedServer)).Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-005: SelectedServer change should persist to local storage")]
    public async Task EFV005()
    {
        // Act
        _sut.SelectedServer = "test-server";

        // Assert (give async operation time to complete)
        await Task.Delay(50);
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:server-filter", "test-server"),
            Times.Once);
    }

    [Fact(DisplayName = "EFV-006: EnableAllEventTypesCommand should enable all types and notify")]
    public void EFV006()
    {
        // Arrange
        _sut.DisableAllEventTypesCommand.Execute(null);
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.EnableAllEventTypesCommand.Execute(null);

        // Assert
        _sut.EnabledEventTypes.Should().HaveCount(24);
        tracker.HasChanged(nameof(EventFilterViewModel.EnabledEventTypes)).Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-007: DisableAllEventTypesCommand should clear types and notify")]
    public void EFV007()
    {
        // Arrange
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.DisableAllEventTypesCommand.Execute(null);

        // Assert
        _sut.EnabledEventTypes.Should().BeEmpty();
        tracker.HasChanged(nameof(EventFilterViewModel.EnabledEventTypes)).Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-008: AddKnownServer should notify when new server added")]
    public void EFV008()
    {
        // Arrange
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.AddKnownServer("server-a");

        // Assert
        tracker.HasChanged(nameof(EventFilterViewModel.KnownServers)).Should().BeTrue();
        _sut.KnownServers.Should().Contain("server-a");
    }

    [Fact(DisplayName = "EFV-009: AddKnownServer should not notify for duplicate")]
    public void EFV009()
    {
        // Arrange
        _sut.AddKnownServer("server-a");
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.AddKnownServer("server-a");

        // Assert
        tracker.HasChanged(nameof(EventFilterViewModel.KnownServers)).Should().BeFalse();
    }

    [Fact(DisplayName = "EFV-010: InitializeAsync should load saved server filter")]
    public async Task EFV010()
    {
        // Arrange
        _localStorageMock
            .Setup(x => x.GetAsync<string>("jasmin-webui:server-filter"))
            .ReturnsAsync("saved-server");

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.SelectedServer.Should().Be("saved-server");
    }

    [Fact(DisplayName = "EFV-011: InitializeAsync should load saved event types")]
    public async Task EFV011()
    {
        // Arrange
        var savedTypes = new List<McpServerEventType>
        {
            McpServerEventType.Started,
            McpServerEventType.Stopped
        };
        _localStorageMock
            .Setup(x => x.GetAsync<List<McpServerEventType>>("jasmin-webui:event-type-filter"))
            .ReturnsAsync(savedTypes);

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.EnabledEventTypes.Should().HaveCount(2);
        _sut.EnabledEventTypes.Should().Contain(McpServerEventType.Started);
        _sut.EnabledEventTypes.Should().Contain(McpServerEventType.Stopped);
    }

    [Fact(DisplayName = "EFV-012: MatchesFilter should filter by server and event type")]
    public void EFV012()
    {
        // Arrange
        _sut.SelectedServer = "server-a";
        _sut.SetEventTypeEnabled(McpServerEventType.Stopped, false);

        var matchingEvent = new McpServerEvent("server-a", McpServerEventType.Started, DateTimeOffset.Now);
        var wrongServer = new McpServerEvent("server-b", McpServerEventType.Started, DateTimeOffset.Now);
        var wrongType = new McpServerEvent("server-a", McpServerEventType.Stopped, DateTimeOffset.Now);

        // Assert
        _sut.MatchesFilter(matchingEvent).Should().BeTrue();
        _sut.MatchesFilter(wrongServer).Should().BeFalse();
        _sut.MatchesFilter(wrongType).Should().BeFalse();
    }

    [Fact(DisplayName = "EFV-013: ClearKnownServers should reset servers and selection")]
    public void EFV013()
    {
        // Arrange
        _sut.AddKnownServer("server-a");
        _sut.SelectedServer = "server-a";
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.ClearKnownServers();

        // Assert
        _sut.KnownServers.Should().BeEmpty();
        _sut.SelectedServer.Should().BeNull();
        tracker.HasChanged(nameof(EventFilterViewModel.KnownServers)).Should().BeTrue();
    }
}
