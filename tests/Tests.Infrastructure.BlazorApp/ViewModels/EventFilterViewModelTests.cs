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

public class EventFilterViewModelTests
{
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly Mock<IJasminApiService> _apiServiceMock;
    private readonly Mock<ILogger<EventFilterViewModel>> _loggerMock;
    private readonly EventFilterViewModel _sut;

    public EventFilterViewModelTests()
    {
        _localStorageMock = new Mock<ILocalStorageService>();
        _apiServiceMock = new Mock<IJasminApiService>();
        _loggerMock = new Mock<ILogger<EventFilterViewModel>>();
        _sut = new EventFilterViewModel(
            _localStorageMock.Object,
            _apiServiceMock.Object,
            _loggerMock.Object);
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

    [Fact(DisplayName = "EFV-004: SetServerSelected should raise PropertyChanged for SelectedServers")]
    public void EFV004()
    {
        // Arrange
        _sut.AddKnownServer("test-server");
        using var tracker = new PropertyChangedTracker(_sut);

        // Act - deselect the server (it's auto-selected when added)
        _sut.SetServerSelected("test-server", false);

        // Assert
        tracker.HasChanged(nameof(EventFilterViewModel.SelectedServers)).Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-005: SetServerSelected should persist to local storage")]
    public async Task EFV005()
    {
        // Arrange
        _sut.AddKnownServer("test-server");

        // Act
        _sut.SetServerSelected("test-server", false);

        // Assert (give async operation time to complete)
        await Task.Delay(50);
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:server-filter", It.IsAny<List<string>>()),
            Times.AtLeastOnce);
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
        var savedServers = new List<string> { "saved-server" };
        _localStorageMock
            .Setup(x => x.GetAsync<List<string>>("jasmin-webui:server-filter"))
            .ReturnsAsync(savedServers);

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.SelectedServers.Should().Contain("saved-server");
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
        _sut.AddKnownServer("server-a");
        _sut.AddKnownServer("server-b");
        // Deselect server-b so only server-a is selected
        _sut.SetServerSelected("server-b", false);
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
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        _sut.ClearKnownServers();

        // Assert
        _sut.KnownServers.Should().BeEmpty();
        _sut.SelectedServers.Should().BeEmpty();
        tracker.HasChanged(nameof(EventFilterViewModel.KnownServers)).Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-014: IsServerFilterExpanded should default to true")]
    public void EFV014()
    {
        // Assert
        _sut.IsServerFilterExpanded.Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-015: IsEventTypeFilterExpanded should default to true")]
    public void EFV015()
    {
        // Assert
        _sut.IsEventTypeFilterExpanded.Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-016: SelectAllServersCommand should select all known servers")]
    public void EFV016()
    {
        // Arrange
        _sut.AddKnownServer("server-a");
        _sut.AddKnownServer("server-b");
        _sut.DeselectAllServersCommand.Execute(null);

        // Act
        _sut.SelectAllServersCommand.Execute(null);

        // Assert
        _sut.IsServerSelected("server-a").Should().BeTrue();
        _sut.IsServerSelected("server-b").Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-017: DeselectAllServersCommand should deselect all servers")]
    public void EFV017()
    {
        // Arrange
        _sut.AddKnownServer("server-a");
        _sut.AddKnownServer("server-b");

        // Act
        _sut.DeselectAllServersCommand.Execute(null);

        // Assert
        _sut.IsServerSelected("server-a").Should().BeFalse();
        _sut.IsServerSelected("server-b").Should().BeFalse();
        _sut.SelectedServers.Should().BeEmpty();
    }

    [Fact(DisplayName = "EFV-018: IsServerSelected should return true for selected server")]
    public void EFV018()
    {
        // Arrange
        _sut.AddKnownServer("server-a");

        // Assert - server is auto-selected when added
        _sut.IsServerSelected("server-a").Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-019: SetServerSelected should update selection and raise FilterChanged")]
    public void EFV019()
    {
        // Arrange
        _sut.AddKnownServer("server-a");
        var filterChangedRaised = false;
        _sut.FilterChanged += () => filterChangedRaised = true;

        // Act
        _sut.SetServerSelected("server-a", false);

        // Assert
        _sut.IsServerSelected("server-a").Should().BeFalse();
        filterChangedRaised.Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-020: SelectEventTypeGroup should select all types in group")]
    public void EFV020()
    {
        // Arrange
        _sut.DisableAllEventTypesCommand.Execute(null);

        // Act
        _sut.SelectEventTypeGroup("Lifecycle");

        // Assert
        _sut.IsEventTypeEnabled(McpServerEventType.Starting).Should().BeTrue();
        _sut.IsEventTypeEnabled(McpServerEventType.Started).Should().BeTrue();
        _sut.IsEventTypeEnabled(McpServerEventType.Stopped).Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-021: DeselectEventTypeGroup should deselect all types in group")]
    public void EFV021()
    {
        // Act
        _sut.DeselectEventTypeGroup("Lifecycle");

        // Assert
        _sut.IsEventTypeEnabled(McpServerEventType.Starting).Should().BeFalse();
        _sut.IsEventTypeEnabled(McpServerEventType.Started).Should().BeFalse();
        _sut.IsEventTypeEnabled(McpServerEventType.Stopped).Should().BeFalse();
    }

    [Fact(DisplayName = "EFV-022: DefaultEventTypeGroups should return all 7 groups")]
    public void EFV022()
    {
        // Assert - using DefaultEventTypeGroups since EventTypeGroups is now instance-based
        EventFilterViewModel.DefaultEventTypeGroups.Should().HaveCount(7);
        EventFilterViewModel.DefaultEventTypeGroups.Keys.Should().Contain("Lifecycle");
        EventFilterViewModel.DefaultEventTypeGroups.Keys.Should().Contain("Configuration");
        EventFilterViewModel.DefaultEventTypeGroups.Keys.Should().Contain("Tools");
        EventFilterViewModel.DefaultEventTypeGroups.Keys.Should().Contain("Prompts");
        EventFilterViewModel.DefaultEventTypeGroups.Keys.Should().Contain("Resources");
        EventFilterViewModel.DefaultEventTypeGroups.Keys.Should().Contain("Invocations");
        EventFilterViewModel.DefaultEventTypeGroups.Keys.Should().Contain("Server");
    }

    [Fact(DisplayName = "EFV-023: SelectedServers should return only selected servers")]
    public void EFV023()
    {
        // Arrange
        _sut.AddKnownServer("server-a");
        _sut.AddKnownServer("server-b");
        _sut.SetServerSelected("server-b", false);

        // Assert
        _sut.SelectedServers.Should().HaveCount(1);
        _sut.SelectedServers.Should().Contain("server-a");
    }

    [Fact(DisplayName = "EFV-024: MatchesFilter should show all events when no servers selected")]
    public void EFV024()
    {
        // Arrange - no servers selected (but servers exist)
        _sut.AddKnownServer("server-a");
        _sut.DeselectAllServersCommand.Execute(null);

        var event1 = new McpServerEvent("server-a", McpServerEventType.Started, DateTimeOffset.Now);
        var event2 = new McpServerEvent("server-b", McpServerEventType.Started, DateTimeOffset.Now);

        // Assert - when no servers selected, all events should pass the server filter
        _sut.MatchesFilter(event1).Should().BeTrue();
        _sut.MatchesFilter(event2).Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-025: AddKnownServer should auto-select new servers")]
    public void EFV025()
    {
        // Act
        _sut.AddKnownServer("new-server");

        // Assert
        _sut.IsServerSelected("new-server").Should().BeTrue();
        _sut.SelectedServers.Should().Contain("new-server");
    }

    [Fact(DisplayName = "EFV-026: LoadServersFromApiAsync should populate known servers")]
    public async Task EFV026()
    {
        // Arrange
        var servers = new List<McpServerInfo>
        {
            new("server-1", "running", DateTimeOffset.Now),
            new("server-2", "stopped", DateTimeOffset.Now)
        };
        _apiServiceMock.Setup(x => x.GetMcpServersAsync("http://localhost:5000"))
            .ReturnsAsync(servers);

        // Act
        await _sut.LoadServersFromApiAsync("http://localhost:5000");

        // Assert
        _sut.KnownServers.Should().HaveCount(2);
        _sut.KnownServers.Should().Contain("server-1");
        _sut.KnownServers.Should().Contain("server-2");
    }

    [Fact(DisplayName = "EFV-027: LoadServersFromApiAsync should auto-select loaded servers")]
    public async Task EFV027()
    {
        // Arrange
        var servers = new List<McpServerInfo>
        {
            new("api-server", "running", null)
        };
        _apiServiceMock.Setup(x => x.GetMcpServersAsync(It.IsAny<string>()))
            .ReturnsAsync(servers);

        // Act
        await _sut.LoadServersFromApiAsync("http://localhost:5000");

        // Assert
        _sut.SelectedServers.Should().Contain("api-server");
        _sut.IsServerSelected("api-server").Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-028: LoadServersFromApiAsync should handle API errors gracefully")]
    public async Task EFV028()
    {
        // Arrange
        _apiServiceMock.Setup(x => x.GetMcpServersAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act - should not throw
        await _sut.LoadServersFromApiAsync("http://localhost:5000");

        // Assert - no servers added, but no exception
        _sut.KnownServers.Should().BeEmpty();
    }

    [Fact(DisplayName = "EFV-029: HandleServerEvent with ServerCreated should add server")]
    public void EFV029()
    {
        // Arrange
        var evt = new McpServerEvent("new-server", McpServerEventType.ServerCreated, DateTimeOffset.Now);

        // Act
        _sut.HandleServerEvent(evt);

        // Assert
        _sut.KnownServers.Should().Contain("new-server");
        _sut.IsServerSelected("new-server").Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-030: HandleServerEvent with ServerDeleted should remove server")]
    public void EFV030()
    {
        // Arrange
        _sut.AddKnownServer("old-server");
        _sut.KnownServers.Should().Contain("old-server");

        var evt = new McpServerEvent("old-server", McpServerEventType.ServerDeleted, DateTimeOffset.Now);

        // Act
        _sut.HandleServerEvent(evt);

        // Assert
        _sut.KnownServers.Should().NotContain("old-server");
        _sut.SelectedServers.Should().NotContain("old-server");
    }

    [Fact(DisplayName = "EFV-031: RemoveServer should raise FilterChanged")]
    public void EFV031()
    {
        // Arrange
        _sut.AddKnownServer("server-to-remove");
        var filterChangedRaised = false;
        _sut.FilterChanged += () => filterChangedRaised = true;

        // Act
        _sut.RemoveServer("server-to-remove");

        // Assert
        filterChangedRaised.Should().BeTrue();
    }

    [Fact(DisplayName = "EFV-032: RemoveServer should save updated selection to storage")]
    public void EFV032()
    {
        // Arrange
        _sut.AddKnownServer("server-to-remove");

        // Act
        _sut.RemoveServer("server-to-remove");

        // Assert
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:server-filter", It.IsAny<List<string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "EFV-033: LoadEventTypesFromApiAsync should update EventTypeGroups")]
    public async Task EFV033()
    {
        // Arrange
        var eventTypes = new List<EventTypeInfo>
        {
            new("Starting", 0, "Lifecycle", "Server is starting"),
            new("Started", 1, "Lifecycle", "Server has started"),
            new("ConfigurationCreated", 6, "Configuration", "Configuration was created")
        };
        _apiServiceMock.Setup(x => x.GetEventTypesAsync(It.IsAny<string>()))
            .ReturnsAsync(eventTypes);

        // Act
        await _sut.LoadEventTypesFromApiAsync("http://localhost:5000");

        // Assert
        _sut.EventTypeGroups.Should().HaveCount(2);
        _sut.EventTypeGroups.Keys.Should().Contain("Lifecycle");
        _sut.EventTypeGroups.Keys.Should().Contain("Configuration");
        _sut.EventTypeGroups["Lifecycle"].Should().HaveCount(2);
    }

    [Fact(DisplayName = "EFV-034: EventTypeGroups should return defaults when API not loaded")]
    public void EFV034()
    {
        // Assert - without loading from API, should return defaults
        _sut.EventTypeGroups.Should().BeEquivalentTo(EventFilterViewModel.DefaultEventTypeGroups);
    }

    [Fact(DisplayName = "EFV-035: LoadEventTypesFromApiAsync should handle API errors gracefully")]
    public async Task EFV035()
    {
        // Arrange
        _apiServiceMock.Setup(x => x.GetEventTypesAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act - should not throw
        await _sut.LoadEventTypesFromApiAsync("http://localhost:5000");

        // Assert - defaults should still work
        _sut.EventTypeGroups.Should().HaveCount(7);
    }

    [Fact(DisplayName = "EFV-036: LoadEventTypesFromApiAsync should raise PropertyChanged for EventTypeGroups")]
    public async Task EFV036()
    {
        // Arrange
        var eventTypes = new List<EventTypeInfo>
        {
            new("Starting", 0, "Lifecycle", "Server is starting")
        };
        _apiServiceMock.Setup(x => x.GetEventTypesAsync(It.IsAny<string>()))
            .ReturnsAsync(eventTypes);

        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        await _sut.LoadEventTypesFromApiAsync("http://localhost:5000");

        // Assert
        tracker.HasChanged(nameof(EventFilterViewModel.EventTypeGroups)).Should().BeTrue();
    }
}
