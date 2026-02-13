using Core.Application.Events;
using Core.Application.McpServers;
using Core.Application.Storage;
using Core.Domain.Events;
using Core.Infrastructure.BlazorApp.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Infrastructure.BlazorApp.Helpers;
using Xunit;

namespace Tests.Infrastructure.BlazorApp.ViewModels;

public class InstanceManagementViewModelTests
{
    private readonly Mock<IToolInvocationService> _invocationServiceMock;
    private readonly Mock<IApplicationStateService> _appStateMock;
    private readonly Mock<IEventStreamService> _eventStreamMock;
    private readonly Mock<IInstanceLogService> _logServiceMock;
    private readonly EventViewerViewModel _eventViewerViewModel;
    private readonly InstanceManagementViewModel _sut;

    public InstanceManagementViewModelTests()
    {
        _invocationServiceMock = new Mock<IToolInvocationService>();
        _appStateMock = new Mock<IApplicationStateService>();
        _eventStreamMock = new Mock<IEventStreamService>();
        _logServiceMock = new Mock<IInstanceLogService>();

        _appStateMock.Setup(x => x.ServerUrl).Returns("http://localhost:5000");

        _invocationServiceMock
            .Setup(x => x.GetInstancesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolInvocationServiceResult<IReadOnlyList<McpServerInstance>>.Success(
                Array.Empty<McpServerInstance>()));

        var preferencesMock = new Mock<IUserPreferencesService>();
        preferencesMock.Setup(x => x.KnownServers).Returns(new List<string>());
        preferencesMock.Setup(x => x.SelectedServers).Returns(new List<string>());
        preferencesMock.Setup(x => x.EnabledEventTypes).Returns(new HashSet<int>());
        preferencesMock.Setup(x => x.IsServerFilterExpanded).Returns(true);
        preferencesMock.Setup(x => x.IsEventTypeFilterExpanded).Returns(true);
        var filterViewModel = new EventFilterViewModel(
            preferencesMock.Object,
            new Mock<IJasminApiService>().Object,
            new Mock<ILogger<EventFilterViewModel>>().Object);
        var serverListViewModel = new McpServerListViewModel(
            new Mock<IMcpServerListService>().Object,
            _invocationServiceMock.Object);
        _eventViewerViewModel = new EventViewerViewModel(
            _eventStreamMock.Object,
            _appStateMock.Object,
            new Mock<IMcpServerDetailService>().Object,
            filterViewModel,
            serverListViewModel);

        _sut = new InstanceManagementViewModel(
            _invocationServiceMock.Object,
            _appStateMock.Object,
            _eventStreamMock.Object,
            _logServiceMock.Object,
            _eventViewerViewModel);
    }

    [Fact(DisplayName = "IMV-001: IsOpen should default to false")]
    public void IMV001()
    {
        _sut.IsOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "IMV-002: SelectedInstanceId should default to null")]
    public void IMV002()
    {
        _sut.SelectedInstanceId.Should().BeNull();
    }

    [Fact(DisplayName = "IMV-003: LogEntries should default to empty")]
    public void IMV003()
    {
        _sut.LogEntries.Should().BeEmpty();
    }

    [Fact(DisplayName = "IMV-004: OpenAsync should set IsOpen and load instances")]
    public async Task IMV004()
    {
        // Arrange
        var instances = new McpServerInstance[]
        {
            new("inst-1", "my-server", DateTimeOffset.UtcNow)
        };
        _invocationServiceMock
            .Setup(x => x.GetInstancesAsync("http://localhost:5000", "my-server", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolInvocationServiceResult<IReadOnlyList<McpServerInstance>>.Success(instances));

        // Act
        await _sut.OpenCommand.ExecuteAsync("my-server");

        // Assert
        _sut.IsOpen.Should().BeTrue();
        _sut.ServerName.Should().Be("my-server");
        _sut.Instances.Should().HaveCount(1);
        _sut.Instances[0].InstanceId.Should().Be("inst-1");
    }

    [Fact(DisplayName = "IMV-005: SelectInstanceAsync should set SelectedInstanceId")]
    public async Task IMV005()
    {
        // Act
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");

        // Assert
        _sut.SelectedInstanceId.Should().Be("inst-1");
    }

    [Fact(DisplayName = "IMV-006: SelectInstanceAsync should start log stream")]
    public async Task IMV006()
    {
        // Act
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");

        // Assert
        _logServiceMock.Verify(x => x.StartStreamAsync(
            "http://localhost:5000",
            It.IsAny<string>(),
            "inst-1",
            0,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "IMV-007: SelectInstanceAsync with different instance should stop previous stream first")]
    public async Task IMV007()
    {
        // Arrange - select first instance
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");
        _logServiceMock.Invocations.Clear();

        // Act - select different instance
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-2");

        // Assert - should disconnect previous before connecting new
        _logServiceMock.Verify(x => x.StopStreamAsync(), Times.Once);
        _sut.SelectedInstanceId.Should().Be("inst-2");
    }

    [Fact(DisplayName = "IMV-008: SelectInstanceAsync with same instance should be no-op")]
    public async Task IMV008()
    {
        // Arrange - select first instance
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");
        _logServiceMock.Invocations.Clear();

        // Act - select same instance again
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");

        // Assert - no additional calls
        _logServiceMock.Verify(x => x.StartStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "IMV-009: CloseAsync should stop log stream and clear selection")]
    public async Task IMV009()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");

        // Act
        await _sut.CloseCommand.ExecuteAsync(null);

        // Assert
        _logServiceMock.Verify(x => x.StopStreamAsync(), Times.AtLeastOnce);
        _sut.SelectedInstanceId.Should().BeNull();
        _sut.LogEntries.Should().BeEmpty();
        _sut.IsOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "IMV-010: CloseAsync should unsubscribe from event stream")]
    public async Task IMV010()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");

        // Act
        await _sut.CloseCommand.ExecuteAsync(null);

        // Assert - verify that SSE events after close don't affect state
        _sut.IsOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "IMV-011: StopInstanceAsync for selected instance should clear selection")]
    public async Task IMV011()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");
        _sut.Instances.Add(new McpServerInstance("inst-1", "my-server", DateTimeOffset.UtcNow));
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");

        _invocationServiceMock
            .Setup(x => x.StopInstanceAsync("http://localhost:5000", "my-server", "inst-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolInvocationServiceResult.Success());

        // Act
        await _sut.StopInstanceCommand.ExecuteAsync("inst-1");

        // Assert
        _sut.SelectedInstanceId.Should().BeNull();
        _sut.LogEntries.Should().BeEmpty();
    }

    [Fact(DisplayName = "IMV-012: StopInstanceAsync for non-selected instance should not affect logs")]
    public async Task IMV012()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");
        _sut.Instances.Add(new McpServerInstance("inst-1", "my-server", DateTimeOffset.UtcNow));
        _sut.Instances.Add(new McpServerInstance("inst-2", "my-server", DateTimeOffset.UtcNow));
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");

        _invocationServiceMock
            .Setup(x => x.StopInstanceAsync("http://localhost:5000", "my-server", "inst-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolInvocationServiceResult.Success());

        // Act
        await _sut.StopInstanceCommand.ExecuteAsync("inst-2");

        // Assert
        _sut.SelectedInstanceId.Should().Be("inst-1");
    }

    [Fact(DisplayName = "IMV-013: LogEntryReceived should add entry to LogEntries")]
    public async Task IMV013()
    {
        // Arrange
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");
        var entry = new InstanceLogEntry(1, DateTimeOffset.UtcNow, "test log line");

        // Act - simulate log entry received
        _logServiceMock.Raise(x => x.LogEntryReceived += null, _logServiceMock.Object, entry);

        // Assert
        _sut.LogEntries.Should().HaveCount(1);
        _sut.LogEntries[0].Text.Should().Be("test log line");
    }

    [Fact(DisplayName = "IMV-014: LogEntryReceived should raise LogEntriesChanged")]
    public async Task IMV014()
    {
        // Arrange
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");
        var logEntriesChangedRaised = false;
        _sut.LogEntriesChanged += () => logEntriesChangedRaised = true;
        var entry = new InstanceLogEntry(1, DateTimeOffset.UtcNow, "test");

        // Act
        _logServiceMock.Raise(x => x.LogEntryReceived += null, _logServiceMock.Object, entry);

        // Assert
        logEntriesChangedRaised.Should().BeTrue();
    }

    [Fact(DisplayName = "IMV-015: SSE Stopped event for selected instance should clear selection")]
    public async Task IMV015()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");
        _sut.Instances.Add(new McpServerInstance("inst-1", "my-server", DateTimeOffset.UtcNow));
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");

        var stoppedEvent = new McpServerEvent(
            "my-server",
            McpServerEventType.Stopped,
            DateTimeOffset.UtcNow,
            Errors: null,
            InstanceId: "inst-1");

        // Act - simulate SSE event
        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, stoppedEvent);

        // Assert
        _sut.SelectedInstanceId.Should().BeNull();
        _sut.Instances.Should().BeEmpty();
    }

    [Fact(DisplayName = "IMV-016: StartNewInstanceAsync should refresh list")]
    public async Task IMV016()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");
        _invocationServiceMock
            .Setup(x => x.StartInstanceAsync("http://localhost:5000", "my-server", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolInvocationServiceResult<string>.Success("inst-new"));

        // Act
        await _sut.StartNewInstanceCommand.ExecuteAsync(null);

        // Assert - GetInstancesAsync should have been called at least twice (initial open + after start)
        _invocationServiceMock.Verify(x => x.GetInstancesAsync(
            "http://localhost:5000", "my-server", It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact(DisplayName = "IMV-017: RefreshAsync should populate Instances")]
    public async Task IMV017()
    {
        // Arrange
        var instances = new McpServerInstance[]
        {
            new("inst-1", "my-server", DateTimeOffset.UtcNow),
            new("inst-2", "my-server", DateTimeOffset.UtcNow)
        };
        _invocationServiceMock
            .Setup(x => x.GetInstancesAsync("http://localhost:5000", "my-server", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolInvocationServiceResult<IReadOnlyList<McpServerInstance>>.Success(instances));

        // Act
        await _sut.OpenCommand.ExecuteAsync("my-server");

        // Assert
        _sut.Instances.Should().HaveCount(2);
    }

    [Fact(DisplayName = "IMV-018: InstanceEvents should default to empty")]
    public void IMV018()
    {
        _sut.InstanceEvents.Should().BeEmpty();
    }

    [Fact(DisplayName = "IMV-019: SSE event should be stored and raise EventsChanged")]
    public async Task IMV019()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");
        var eventsChangedRaised = false;
        _sut.EventsChanged += () => eventsChangedRaised = true;

        var evt = new McpServerEvent(
            "my-server",
            McpServerEventType.ToolsRetrieving,
            DateTimeOffset.UtcNow,
            Errors: null,
            InstanceId: "inst-1");

        // Act
        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt);

        // Assert
        eventsChangedRaised.Should().BeTrue();
    }

    [Fact(DisplayName = "IMV-020: InstanceEvents should filter by SelectedInstanceId")]
    public async Task IMV020()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");

        var evt1 = new McpServerEvent("my-server", McpServerEventType.Started, DateTimeOffset.UtcNow, Errors: null, InstanceId: "inst-1");
        var evt2 = new McpServerEvent("my-server", McpServerEventType.Started, DateTimeOffset.UtcNow, Errors: null, InstanceId: "inst-2");

        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt1);
        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt2);

        // Act
        var result = _sut.InstanceEvents;

        // Assert
        result.Should().HaveCount(1);
        result[0].InstanceId.Should().Be("inst-1");
    }

    [Fact(DisplayName = "IMV-021: SSE events for other servers should be ignored")]
    public async Task IMV021()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");
        var eventsChangedRaised = false;
        _sut.EventsChanged += () => eventsChangedRaised = true;

        var evt = new McpServerEvent(
            "other-server",
            McpServerEventType.Started,
            DateTimeOffset.UtcNow,
            Errors: null,
            InstanceId: "inst-1");

        // Act
        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt);

        // Assert
        eventsChangedRaised.Should().BeFalse();
    }

    [Fact(DisplayName = "IMV-022: CloseAsync should clear stored events")]
    public async Task IMV022()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-1");

        var evt = new McpServerEvent("my-server", McpServerEventType.Started, DateTimeOffset.UtcNow, Errors: null, InstanceId: "inst-1");
        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt);
        _sut.InstanceEvents.Should().HaveCount(1);

        // Act
        await _sut.CloseCommand.ExecuteAsync(null);

        // Assert
        _sut.InstanceEvents.Should().BeEmpty();
    }

    [Fact(DisplayName = "IMV-023: SSE Started event should both store event and add instance")]
    public async Task IMV023()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");

        var evt = new McpServerEvent(
            "my-server",
            McpServerEventType.Started,
            DateTimeOffset.UtcNow,
            Errors: null,
            InstanceId: "inst-new");

        // Act
        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt);

        // Assert - event stored and instance added
        await _sut.SelectInstanceCommand.ExecuteAsync("inst-new");
        _sut.InstanceEvents.Should().HaveCount(1);
        _sut.Instances.Should().Contain(i => i.InstanceId == "inst-new");
    }

    [Fact(DisplayName = "IMV-024: GetEventId should produce deterministic identifier")]
    public void IMV024()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var evt = new McpServerEvent("my-server", McpServerEventType.Started, timestamp, Errors: null, InstanceId: "inst-1");

        // Act
        var id = InstanceManagementViewModel.GetEventId(evt);

        // Assert
        id.Should().Be($"{timestamp.Ticks}_my-server_Started");
    }

    [Fact(DisplayName = "IMV-025: InstanceEvents should return empty when no instance selected")]
    public async Task IMV025()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync("my-server");

        var evt = new McpServerEvent("my-server", McpServerEventType.Started, DateTimeOffset.UtcNow, Errors: null, InstanceId: "inst-1");
        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt);

        // Act - no instance selected
        var result = _sut.InstanceEvents;

        // Assert
        result.Should().BeEmpty();
    }
}
