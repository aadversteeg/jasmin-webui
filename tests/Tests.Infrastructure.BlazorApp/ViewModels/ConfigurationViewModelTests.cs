using Core.Application.Events;
using Core.Application.Storage;
using Core.Infrastructure.BlazorApp.ViewModels;
using FluentAssertions;
using Moq;
using Tests.Infrastructure.BlazorApp.Helpers;
using Xunit;

namespace Tests.Infrastructure.BlazorApp.ViewModels;

public class ConfigurationViewModelTests
{
    private readonly Mock<IEventStreamService> _eventStreamMock;
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly ConfigurationViewModel _sut;

    public ConfigurationViewModelTests()
    {
        _eventStreamMock = new Mock<IEventStreamService>();
        _localStorageMock = new Mock<ILocalStorageService>();

        _sut = new ConfigurationViewModel(
            _eventStreamMock.Object,
            _localStorageMock.Object);
    }

    [Fact(DisplayName = "CFG-001: IsOpen should default to false")]
    public void CFG001()
    {
        _sut.IsOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "CFG-002: OpenCommand should set IsOpen to true")]
    public async Task CFG002()
    {
        // Act
        await _sut.OpenCommand.ExecuteAsync(null);

        // Assert
        _sut.IsOpen.Should().BeTrue();
    }

    [Fact(DisplayName = "CFG-003: OpenCommand should load saved URL from storage")]
    public async Task CFG003()
    {
        // Arrange
        _localStorageMock
            .Setup(x => x.GetAsync<string>("jasmin-webui:server-url"))
            .ReturnsAsync("http://saved-server.com");

        // Act
        await _sut.OpenCommand.ExecuteAsync(null);

        // Assert
        _sut.ServerUrl.Should().Be("http://saved-server.com");
    }

    [Fact(DisplayName = "CFG-004: OpenCommand should reset TestState to None")]
    public async Task CFG004()
    {
        // Arrange
        _sut.ServerUrl = "http://test.com";
        _eventStreamMock
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>()))
            .ReturnsAsync((true, null));
        await _sut.TestConnectionCommand.ExecuteAsync(null);
        _sut.TestState.Should().Be(ConnectionTestState.Success);

        // Act
        await _sut.OpenCommand.ExecuteAsync(null);

        // Assert
        _sut.TestState.Should().Be(ConnectionTestState.None);
    }

    [Fact(DisplayName = "CFG-005: TestConnectionCommand should set TestState to Testing")]
    public async Task CFG005()
    {
        // Arrange
        _sut.ServerUrl = "http://test.com";
        var tcs = new TaskCompletionSource<(bool, string?)>();
        _eventStreamMock
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>()))
            .Returns(tcs.Task);

        // Act
        var testTask = _sut.TestConnectionCommand.ExecuteAsync(null);
        await Task.Delay(10);

        // Assert
        _sut.TestState.Should().Be(ConnectionTestState.Testing);

        // Cleanup
        tcs.SetResult((true, null));
        await testTask;
    }

    [Fact(DisplayName = "CFG-006: TestConnectionCommand success should set TestState to Success")]
    public async Task CFG006()
    {
        // Arrange
        _sut.ServerUrl = "http://test.com";
        _eventStreamMock
            .Setup(x => x.TestConnectionAsync("http://test.com"))
            .ReturnsAsync((true, null));

        // Act
        await _sut.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        _sut.TestState.Should().Be(ConnectionTestState.Success);
        _sut.TestErrorMessage.Should().BeNull();
    }

    [Fact(DisplayName = "CFG-007: TestConnectionCommand failure should set TestState to Failed")]
    public async Task CFG007()
    {
        // Arrange
        _sut.ServerUrl = "http://test.com";
        _eventStreamMock
            .Setup(x => x.TestConnectionAsync("http://test.com"))
            .ReturnsAsync((false, "Connection refused"));

        // Act
        await _sut.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        _sut.TestState.Should().Be(ConnectionTestState.Failed);
        _sut.TestErrorMessage.Should().Be("Connection refused");
    }

    [Fact(DisplayName = "CFG-008: TestConnectionCommand with empty URL should fail")]
    public async Task CFG008()
    {
        // Arrange
        _sut.ServerUrl = "";

        // Act
        await _sut.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        _sut.TestState.Should().Be(ConnectionTestState.Failed);
        _sut.TestErrorMessage.Should().Be("Please enter a server URL");
    }

    [Fact(DisplayName = "CFG-009: SaveCommand should only be enabled when TestState is Success")]
    public async Task CFG009()
    {
        // Initially disabled
        _sut.SaveCommand.CanExecute(null).Should().BeFalse();

        // After failed test - still disabled
        _sut.ServerUrl = "http://test.com";
        _eventStreamMock
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>()))
            .ReturnsAsync((false, "error"));
        await _sut.TestConnectionCommand.ExecuteAsync(null);
        _sut.SaveCommand.CanExecute(null).Should().BeFalse();

        // After successful test - enabled
        _eventStreamMock
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>()))
            .ReturnsAsync((true, null));
        await _sut.TestConnectionCommand.ExecuteAsync(null);
        _sut.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact(DisplayName = "CFG-010: SaveCommand should save URL to storage")]
    public async Task CFG010()
    {
        // Arrange
        _sut.ServerUrl = "http://new-server.com";
        _eventStreamMock
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>()))
            .ReturnsAsync((true, null));
        await _sut.TestConnectionCommand.ExecuteAsync(null);

        // Act
        await _sut.SaveCommand.ExecuteAsync(null);

        // Assert
        _localStorageMock.Verify(
            x => x.SetAsync("jasmin-webui:server-url", "http://new-server.com"),
            Times.Once);
    }

    [Fact(DisplayName = "CFG-011: SaveCommand should close dialog")]
    public async Task CFG011()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync(null);
        _sut.ServerUrl = "http://test.com";
        _eventStreamMock
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>()))
            .ReturnsAsync((true, null));
        await _sut.TestConnectionCommand.ExecuteAsync(null);

        // Act
        await _sut.SaveCommand.ExecuteAsync(null);

        // Assert
        _sut.IsOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "CFG-012: SaveCommand should raise UrlSaved event")]
    public async Task CFG012()
    {
        // Arrange
        _sut.ServerUrl = "http://new-server.com";
        _eventStreamMock
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>()))
            .ReturnsAsync((true, null));
        await _sut.TestConnectionCommand.ExecuteAsync(null);

        string? savedUrl = null;
        _sut.UrlSaved += url => savedUrl = url;

        // Act
        await _sut.SaveCommand.ExecuteAsync(null);

        // Assert
        savedUrl.Should().Be("http://new-server.com");
    }

    [Fact(DisplayName = "CFG-013: CancelCommand should close dialog")]
    public async Task CFG013()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync(null);

        // Act
        _sut.CancelCommand.Execute(null);

        // Assert
        _sut.IsOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "CFG-014: CancelCommand should reset TestState")]
    public async Task CFG014()
    {
        // Arrange
        _sut.ServerUrl = "http://test.com";
        _eventStreamMock
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>()))
            .ReturnsAsync((true, null));
        await _sut.TestConnectionCommand.ExecuteAsync(null);

        // Act
        _sut.CancelCommand.Execute(null);

        // Assert
        _sut.TestState.Should().Be(ConnectionTestState.None);
        _sut.TestErrorMessage.Should().BeNull();
    }

    [Fact(DisplayName = "CFG-015: DisconnectCommand should clear URL from storage")]
    public async Task CFG015()
    {
        // Act
        await _sut.DisconnectCommand.ExecuteAsync(null);

        // Assert
        _localStorageMock.Verify(
            x => x.RemoveAsync("jasmin-webui:server-url"),
            Times.Once);
    }

    [Fact(DisplayName = "CFG-016: DisconnectCommand should clear ServerUrl")]
    public async Task CFG016()
    {
        // Arrange
        _sut.ServerUrl = "http://test.com";

        // Act
        await _sut.DisconnectCommand.ExecuteAsync(null);

        // Assert
        _sut.ServerUrl.Should().BeEmpty();
    }

    [Fact(DisplayName = "CFG-017: DisconnectCommand should close dialog")]
    public async Task CFG017()
    {
        // Arrange
        await _sut.OpenCommand.ExecuteAsync(null);

        // Act
        await _sut.DisconnectCommand.ExecuteAsync(null);

        // Assert
        _sut.IsOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "CFG-018: DisconnectCommand should raise DisconnectRequested event")]
    public async Task CFG018()
    {
        // Arrange
        var disconnectRequested = false;
        _sut.DisconnectRequested += () => disconnectRequested = true;

        // Act
        await _sut.DisconnectCommand.ExecuteAsync(null);

        // Assert
        disconnectRequested.Should().BeTrue();
    }

    [Fact(DisplayName = "CFG-019: DisconnectCommand should reset TestState")]
    public async Task CFG019()
    {
        // Arrange
        _sut.ServerUrl = "http://test.com";
        _eventStreamMock
            .Setup(x => x.TestConnectionAsync(It.IsAny<string>()))
            .ReturnsAsync((true, null));
        await _sut.TestConnectionCommand.ExecuteAsync(null);

        // Act
        await _sut.DisconnectCommand.ExecuteAsync(null);

        // Assert
        _sut.TestState.Should().Be(ConnectionTestState.None);
        _sut.TestErrorMessage.Should().BeNull();
    }
}
