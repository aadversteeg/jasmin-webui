using Core.Application.Events;
using Core.Application.McpServers;
using Core.Infrastructure.JasminClient;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace Tests.Infrastructure.JasminClient;

public class InstanceLogServiceTests : IAsyncDisposable
{
    private readonly Mock<IJSRuntime> _jsRuntimeMock;
    private readonly Mock<ILogger<InstanceLogService>> _loggerMock;
    private readonly InstanceLogService _sut;

    public InstanceLogServiceTests()
    {
        _jsRuntimeMock = new Mock<IJSRuntime>();
        _loggerMock = new Mock<ILogger<InstanceLogService>>();

        _jsRuntimeMock
            .Setup(x => x.InvokeAsync<int>(
                "eventSourceHelper.connect",
                It.IsAny<CancellationToken>(),
                It.IsAny<object[]>()))
            .ReturnsAsync(42);

        _sut = new InstanceLogService(_jsRuntimeMock.Object, _loggerMock.Object);
    }

    public async ValueTask DisposeAsync()
    {
        await _sut.DisposeAsync();
    }

    [Fact(DisplayName = "ILS-001: StartStreamAsync should invoke JS connect with correct URL and event name")]
    public async Task ILS001()
    {
        // Act
        await _sut.StartStreamAsync("http://localhost:5000", "my-server", "inst-1", afterLine: 10);

        // Assert
        _jsRuntimeMock.Verify(x => x.InvokeAsync<int>(
            "eventSourceHelper.connect",
            It.IsAny<CancellationToken>(),
            It.Is<object[]>(args =>
                args.Length == 5 &&
                ((string)args[0]).Contains("/v1/mcp-servers/my-server/instances/inst-1/logs/stream?afterLine=10") &&
                args[2] == null &&
                (string)args[3] == "instance-log" &&
                (string)args[4] == "OnLogEntryReceived")),
            Times.Once);
    }

    [Fact(DisplayName = "ILS-002: StopStreamAsync should invoke JS disconnect and set state to Disconnected")]
    public async Task ILS002()
    {
        // Arrange - start first to have an active connection
        await _sut.StartStreamAsync("http://localhost:5000", "server", "inst-1");

        // Act
        await _sut.StopStreamAsync();

        // Assert
        _jsRuntimeMock.Verify(x => x.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
            "eventSourceHelper.disconnect",
            It.IsAny<object[]>()),
            Times.Once);
        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
    }

    [Fact(DisplayName = "ILS-003: OnLogEntryReceived should deserialize JSON and raise LogEntryReceived")]
    public void ILS003()
    {
        // Arrange
        InstanceLogEntry? receivedEntry = null;
        _sut.LogEntryReceived += (_, entry) => receivedEntry = entry;

        var json = """{"lineNumber":5,"timestamp":"2025-01-15T10:30:00Z","text":"Hello stderr"}""";

        // Act
        _sut.OnLogEntryReceived(json, "5");

        // Assert
        receivedEntry.Should().NotBeNull();
        receivedEntry!.LineNumber.Should().Be(5);
        receivedEntry.Text.Should().Be("Hello stderr");
        receivedEntry.Timestamp.Should().Be(DateTimeOffset.Parse("2025-01-15T10:30:00Z"));
    }

    [Fact(DisplayName = "ILS-004: OnLogEntryReceived with invalid JSON should not throw")]
    public void ILS004()
    {
        // Arrange
        InstanceLogEntry? receivedEntry = null;
        _sut.LogEntryReceived += (_, entry) => receivedEntry = entry;

        // Act
        var act = () => _sut.OnLogEntryReceived("not valid json{{{", "1");

        // Assert
        act.Should().NotThrow();
        receivedEntry.Should().BeNull();
    }

    [Fact(DisplayName = "ILS-005: OnConnected should set ConnectionState to Connected")]
    public void ILS005()
    {
        // Act
        _sut.OnConnected();

        // Assert
        _sut.ConnectionState.Should().Be(ConnectionState.Connected);
    }

    [Fact(DisplayName = "ILS-006: OnError should raise ErrorOccurred event and set state to Error")]
    public void ILS006()
    {
        // Arrange
        string? receivedError = null;
        _sut.ErrorOccurred += (_, error) => receivedError = error;

        // Act
        _sut.OnError("Connection lost");

        // Assert
        receivedError.Should().Be("Connection lost");
        _sut.ConnectionState.Should().Be(ConnectionState.Error);
    }

    [Fact(DisplayName = "ILS-007: StartStreamAsync when already connected should stop previous stream first")]
    public async Task ILS007()
    {
        // Arrange - start initial connection
        await _sut.StartStreamAsync("http://localhost:5000", "server", "inst-1");

        // Act - start a new connection
        await _sut.StartStreamAsync("http://localhost:5000", "server", "inst-2");

        // Assert - disconnect should have been called for the first connection
        _jsRuntimeMock.Verify(x => x.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
            "eventSourceHelper.disconnect",
            It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact(DisplayName = "ILS-008: DisposeAsync should call StopStreamAsync")]
    public async Task ILS008()
    {
        // Arrange - start a connection
        await _sut.StartStreamAsync("http://localhost:5000", "server", "inst-1");

        // Act
        await _sut.DisposeAsync();

        // Assert
        _jsRuntimeMock.Verify(x => x.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
            "eventSourceHelper.disconnect",
            It.IsAny<object[]>()),
            Times.Once);
        _sut.ConnectionState.Should().Be(ConnectionState.Disconnected);
    }
}
