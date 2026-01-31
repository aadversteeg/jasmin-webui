using Core.Application.Storage;
using Core.Domain.Events;
using Core.Infrastructure.BlazorApp.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace Tests.Infrastructure.BlazorApp.Services;

public class EventFilterStateTests
{
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly EventFilterState _sut;

    public EventFilterStateTests()
    {
        _localStorageMock = new Mock<ILocalStorageService>();
        _sut = new EventFilterState(_localStorageMock.Object);
    }

    [Fact(DisplayName = "FIL-001: EventFilterState should have all event types enabled by default")]
    public void FIL001()
    {
        // Assert
        _sut.EnabledEventTypes.Should().HaveCount(24);
        foreach (var eventType in Enum.GetValues<McpServerEventType>())
        {
            _sut.IsEventTypeEnabled(eventType).Should().BeTrue();
        }
    }

    [Fact(DisplayName = "FIL-002: SetEventTypeEnabled should toggle event type")]
    public void FIL002()
    {
        // Act
        _sut.SetEventTypeEnabled(McpServerEventType.Started, false);

        // Assert
        _sut.IsEventTypeEnabled(McpServerEventType.Started).Should().BeFalse();
        _sut.EnabledEventTypes.Should().HaveCount(23);
    }

    [Fact(DisplayName = "FIL-003: MatchesFilter should filter by server name")]
    public void FIL003()
    {
        // Arrange
        _sut.AddKnownServer("server-a");
        _sut.AddKnownServer("server-b");
        _sut.SelectedServer = "server-a";

        var eventA = new McpServerEvent("server-a", McpServerEventType.Started, DateTimeOffset.Now);
        var eventB = new McpServerEvent("server-b", McpServerEventType.Started, DateTimeOffset.Now);

        // Assert
        _sut.MatchesFilter(eventA).Should().BeTrue();
        _sut.MatchesFilter(eventB).Should().BeFalse();
    }

    [Fact(DisplayName = "FIL-004: MatchesFilter should filter by event type")]
    public void FIL004()
    {
        // Arrange
        _sut.SetEventTypeEnabled(McpServerEventType.Started, false);

        var startedEvent = new McpServerEvent("server", McpServerEventType.Started, DateTimeOffset.Now);
        var stoppedEvent = new McpServerEvent("server", McpServerEventType.Stopped, DateTimeOffset.Now);

        // Assert
        _sut.MatchesFilter(startedEvent).Should().BeFalse();
        _sut.MatchesFilter(stoppedEvent).Should().BeTrue();
    }

    [Fact(DisplayName = "FIL-005: DisableAllEventTypes should clear all enabled types")]
    public void FIL005()
    {
        // Act
        _sut.DisableAllEventTypes();

        // Assert
        _sut.EnabledEventTypes.Should().BeEmpty();
    }

    [Fact(DisplayName = "FIL-006: EnableAllEventTypes should enable all types")]
    public void FIL006()
    {
        // Arrange
        _sut.DisableAllEventTypes();

        // Act
        _sut.EnableAllEventTypes();

        // Assert
        _sut.EnabledEventTypes.Should().HaveCount(24);
    }

    [Fact(DisplayName = "FIL-007: AddKnownServer should track unique servers")]
    public void FIL007()
    {
        // Act
        _sut.AddKnownServer("server-a");
        _sut.AddKnownServer("server-b");
        _sut.AddKnownServer("server-a"); // duplicate

        // Assert
        _sut.KnownServers.Should().HaveCount(2);
        _sut.KnownServers.Should().Contain("server-a");
        _sut.KnownServers.Should().Contain("server-b");
    }
}
