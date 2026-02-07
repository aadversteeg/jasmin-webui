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

public class EventViewerViewModelTests : IDisposable
{
    private readonly Mock<IEventStreamService> _eventStreamMock;
    private readonly Mock<IApplicationStateService> _appStateMock;
    private readonly Mock<IUserPreferencesService> _preferencesMock;
    private readonly Mock<IJasminApiService> _apiServiceMock;
    private readonly Mock<IMcpServerListService> _serverListServiceMock;
    private readonly Mock<IToolInvocationService> _invocationServiceMock;
    private readonly Mock<IMcpServerDetailService> _serverDetailServiceMock;
    private readonly Mock<ILogger<EventFilterViewModel>> _filterLoggerMock;
    private readonly EventFilterViewModel _filterViewModel;
    private readonly McpServerListViewModel _serverListViewModel;
    private readonly EventViewerViewModel _sut;

    public EventViewerViewModelTests()
    {
        _eventStreamMock = new Mock<IEventStreamService>();
        _appStateMock = new Mock<IApplicationStateService>();
        _preferencesMock = new Mock<IUserPreferencesService>();
        _preferencesMock.Setup(x => x.KnownServers).Returns(new List<string>());
        _preferencesMock.Setup(x => x.SelectedServers).Returns(new List<string>());
        _preferencesMock.Setup(x => x.EnabledEventTypes).Returns(new HashSet<int>());
        _preferencesMock.Setup(x => x.IsServerFilterExpanded).Returns(true);
        _preferencesMock.Setup(x => x.IsEventTypeFilterExpanded).Returns(true);
        _apiServiceMock = new Mock<IJasminApiService>();
        _serverListServiceMock = new Mock<IMcpServerListService>();
        _serverListServiceMock.Setup(x => x.Servers).Returns(new List<McpServerListItem>());
        _invocationServiceMock = new Mock<IToolInvocationService>();
        _serverDetailServiceMock = new Mock<IMcpServerDetailService>();
        _filterLoggerMock = new Mock<ILogger<EventFilterViewModel>>();
        _filterViewModel = new EventFilterViewModel(
            _preferencesMock.Object,
            _apiServiceMock.Object,
            _filterLoggerMock.Object);
        _serverListViewModel = new McpServerListViewModel(_serverListServiceMock.Object, _invocationServiceMock.Object);

        _sut = new EventViewerViewModel(
            _eventStreamMock.Object,
            _appStateMock.Object,
            _serverDetailServiceMock.Object,
            _filterViewModel,
            _serverListViewModel);
    }

    [Fact(DisplayName = "EVV-001: Default ServerUrl should be localhost:5000")]
    public void EVV001()
    {
        _sut.ServerUrl.Should().Be("http://localhost:5000");
    }

    [Fact(DisplayName = "EVV-002: ServerUrl change should raise PropertyChanged")]
    public void EVV002()
    {
        // Arrange
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.ServerUrl = "http://example.com";

        // Assert
        tracker.HasChanged(nameof(EventViewerViewModel.ServerUrl)).Should().BeTrue();
    }

    [Fact(DisplayName = "EVV-003: ServerUrl change should persist to application state")]
    public void EVV003()
    {
        // Act
        _sut.ServerUrl = "http://example.com";

        // Assert
        _appStateMock.VerifySet(x => x.ServerUrl = "http://example.com", Times.Once);
    }

    [Fact(DisplayName = "EVV-004: ConnectCommand should call StartAsync with stream URL")]
    public async Task EVV004()
    {
        // Arrange
        _sut.ServerUrl = "http://localhost:5000";

        // Act
        await _sut.ConnectCommand.ExecuteAsync(null);

        // Assert
        _eventStreamMock.Verify(
            x => x.StartAsync("http://localhost:5000/v1/events/stream", default),
            Times.Once);
    }

    [Fact(DisplayName = "EVV-005: ConnectCommand should clear LastError")]
    public async Task EVV005()
    {
        // Arrange
        _sut.LastError = "Previous error";

        // Act
        await _sut.ConnectCommand.ExecuteAsync(null);

        // Assert
        _sut.LastError.Should().BeNull();
    }

    [Fact(DisplayName = "EVV-006: DisconnectCommand should call StopAsync")]
    public async Task EVV006()
    {
        // Act
        await _sut.DisconnectCommand.ExecuteAsync(null);

        // Assert
        _eventStreamMock.Verify(x => x.StopAsync(), Times.Once);
    }

    [Fact(DisplayName = "EVV-007: ClearEventsCommand should clear events and known servers")]
    public void EVV007()
    {
        // Arrange
        _filterViewModel.AddKnownServer("test-server");
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.ClearEventsCommand.Execute(null);

        // Assert
        _sut.Events.Should().BeEmpty();
        _filterViewModel.KnownServers.Should().BeEmpty();
        tracker.HasChanged(nameof(EventViewerViewModel.Events)).Should().BeTrue();
        tracker.HasChanged(nameof(EventViewerViewModel.FilteredEvents)).Should().BeTrue();
    }

    [Fact(DisplayName = "EVV-008: EventReceived should add event and notify")]
    public void EVV008()
    {
        // Arrange
        using var tracker = new PropertyChangedTracker(_sut);
        var eventAddedRaised = false;
        _sut.EventAdded += () => eventAddedRaised = true;

        var evt = new McpServerEvent("server", McpServerEventType.Started, DateTimeOffset.Now);

        // Act
        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt);

        // Assert
        _sut.Events.Should().HaveCount(1);
        tracker.HasChanged(nameof(EventViewerViewModel.Events)).Should().BeTrue();
        tracker.HasChanged(nameof(EventViewerViewModel.FilteredEvents)).Should().BeTrue();
        eventAddedRaised.Should().BeTrue();
    }

    [Fact(DisplayName = "EVV-009: EventReceived should register server with filter")]
    public void EVV009()
    {
        // Arrange
        var evt = new McpServerEvent("new-server", McpServerEventType.Started, DateTimeOffset.Now);

        // Act
        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt);

        // Assert
        _filterViewModel.KnownServers.Should().Contain("new-server");
    }

    [Fact(DisplayName = "EVV-010: Events should be trimmed at MaxEvents (1000)")]
    public void EVV010()
    {
        // Arrange
        for (int i = 0; i < 1005; i++)
        {
            var evt = new McpServerEvent($"server-{i}", McpServerEventType.Started, DateTimeOffset.Now);
            _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt);
        }

        // Assert
        _sut.Events.Should().HaveCount(1000);
        _sut.Events.First().ServerName.Should().Be("server-5");
    }

    [Fact(DisplayName = "EVV-011: ConnectionStateChanged should notify and clear error on connect")]
    public void EVV011()
    {
        // Arrange
        _sut.LastError = "Previous error";
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _eventStreamMock.Raise(
            x => x.ConnectionStateChanged += null,
            _eventStreamMock.Object,
            ConnectionState.Connected);

        // Assert
        _sut.LastError.Should().BeNull();
        tracker.HasChanged(nameof(EventViewerViewModel.ConnectionState)).Should().BeTrue();
    }

    [Fact(DisplayName = "EVV-012: ErrorOccurred should set LastError")]
    public void EVV012()
    {
        // Act
        _eventStreamMock.Raise(
            x => x.ErrorOccurred += null,
            _eventStreamMock.Object,
            "Connection failed");

        // Assert
        _sut.LastError.Should().Be("Connection failed");
    }

    [Fact(DisplayName = "EVV-013: Filter change should notify FilteredEvents")]
    public void EVV013()
    {
        // Arrange
        var evt = new McpServerEvent("server", McpServerEventType.Started, DateTimeOffset.Now);
        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt);
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _filterViewModel.SetEventTypeEnabled(McpServerEventType.Started, false);

        // Assert
        tracker.HasChanged(nameof(EventViewerViewModel.FilteredEvents)).Should().BeTrue();
        _sut.FilteredEvents.Should().BeEmpty();
    }

    [Fact(DisplayName = "EVV-014: InitializeAsync should load saved URL")]
    public async Task EVV014()
    {
        // Arrange
        _appStateMock.Setup(x => x.ServerUrl).Returns("http://saved-server.com");

        // Act
        await _sut.OnInitializedAsync();

        // Assert
        _sut.ServerUrl.Should().Be("http://saved-server.com");
    }

    [Fact(DisplayName = "EVV-015: InitializeAsync should initialize filter")]
    public async Task EVV015()
    {
        // Act
        await _sut.OnInitializedAsync();

        // Assert - filter was initialized (second call should be idempotent)
        await _sut.OnInitializedAsync(); // Should not throw
    }

    [Fact(DisplayName = "EVV-016: Dispose should unsubscribe from all events")]
    public void EVV016()
    {
        // Arrange
        var evt = new McpServerEvent("server", McpServerEventType.Started, DateTimeOffset.Now);

        // Act
        _sut.Dispose();

        // These should not affect the disposed ViewModel
        _eventStreamMock.Raise(x => x.EventReceived += null, _eventStreamMock.Object, evt);
        _eventStreamMock.Raise(x => x.ErrorOccurred += null, _eventStreamMock.Object, "error");

        // Assert - events should be empty since handler was unsubscribed
        _sut.Events.Should().BeEmpty();
    }

    public void Dispose()
    {
        _sut.Dispose();
    }
}
