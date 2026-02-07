using Core.Application.McpServers;
using Core.Infrastructure.BlazorApp.ViewModels;
using FluentAssertions;
using Moq;
using Tests.Infrastructure.BlazorApp.Helpers;
using Xunit;

namespace Tests.Infrastructure.BlazorApp.ViewModels;

public class ResourceViewerViewModelTests : IDisposable
{
    private readonly Mock<IResourceViewerService> _resourceViewerServiceMock;
    private readonly Mock<IToolInvocationService> _toolInvocationServiceMock;
    private readonly ResourceViewerViewModel _sut;

    public ResourceViewerViewModelTests()
    {
        _resourceViewerServiceMock = new Mock<IResourceViewerService>();
        _toolInvocationServiceMock = new Mock<IToolInvocationService>();
        _sut = new ResourceViewerViewModel(
            _resourceViewerServiceMock.Object,
            _toolInvocationServiceMock.Object);
    }

    [Fact(DisplayName = "RVV-001: IsOpen should default to false")]
    public void RVV001()
    {
        _sut.IsOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "RVV-002: Resource should default to null")]
    public void RVV002()
    {
        _sut.Resource.Should().BeNull();
    }

    [Fact(DisplayName = "RVV-003: ServerName should default to empty string")]
    public void RVV003()
    {
        _sut.ServerName.Should().BeEmpty();
    }

    [Fact(DisplayName = "RVV-004: Content should return null when Result is null")]
    public void RVV004()
    {
        _sut.Content.Should().BeNull();
    }

    [Fact(DisplayName = "RVV-005: OpenAsync should set IsOpen to true")]
    public async Task RVV005()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _sut.IsOpen.Should().BeTrue();
    }

    [Fact(DisplayName = "RVV-006: OpenAsync should set Resource")]
    public async Task RVV006()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _sut.Resource.Should().Be(resource);
    }

    [Fact(DisplayName = "RVV-007: OpenAsync should set ServerName")]
    public async Task RVV007()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _sut.ServerName.Should().Be("test-server");
    }

    [Fact(DisplayName = "RVV-008: OpenAsync should set ServerUrl")]
    public async Task RVV008()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _sut.ServerUrl.Should().Be("http://localhost:5000");
    }

    [Fact(DisplayName = "RVV-009: OpenAsync should start instance")]
    public async Task RVV009()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _toolInvocationServiceMock.Verify(
            x => x.StartInstanceAsync("http://localhost:5000", "test-server", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "RVV-010: OpenAsync should set InstanceId after successful start")]
    public async Task RVV010()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceSuccess();

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _sut.InstanceId.Should().Be("instance-123");
    }

    [Fact(DisplayName = "RVV-011: OpenAsync should set ErrorMessage when start fails")]
    public async Task RVV011()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceFailure("Failed to start server");

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _sut.ErrorMessage.Should().Be("Failed to start server");
    }

    [Fact(DisplayName = "RVV-012: OpenAsync should load resource after starting instance")]
    public async Task RVV012()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceSuccess();

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _resourceViewerServiceMock.Verify(
            x => x.ReadResourceAsync(
                "http://localhost:5000",
                "test-server",
                "instance-123",
                resource.Uri,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "RVV-013: OpenAsync should set Result after successful load")]
    public async Task RVV013()
    {
        // Arrange
        var resource = CreateTestResource();
        var content = new McpResourceContent(resource.Uri, "text/markdown", "# Hello", null);
        var readResult = new McpResourceReadResult(new[] { content });
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceSuccess(readResult);

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _sut.Result.Should().Be(readResult);
    }

    [Fact(DisplayName = "RVV-014: Content should return first content from Result")]
    public async Task RVV014()
    {
        // Arrange
        var resource = CreateTestResource();
        var content = new McpResourceContent(resource.Uri, "text/markdown", "# Hello", null);
        var readResult = new McpResourceReadResult(new[] { content });
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceSuccess(readResult);

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _sut.Content.Should().Be(content);
    }

    [Fact(DisplayName = "RVV-015: OpenAsync should set ErrorMessage when read fails")]
    public async Task RVV015()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceFailure("Failed to read resource");

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _sut.ErrorMessage.Should().Be("Failed to read resource");
    }

    [Fact(DisplayName = "RVV-016: CloseAsync should set IsOpen to false")]
    public async Task RVV016()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceSuccess();
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Act
        await _sut.CloseAsync();

        // Assert
        _sut.IsOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "RVV-017: CloseAsync should stop instance")]
    public async Task RVV017()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceSuccess();
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Act
        await _sut.CloseAsync();

        // Assert
        _toolInvocationServiceMock.Verify(
            x => x.StopInstanceAsync("http://localhost:5000", "test-server", "instance-123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "RVV-018: CloseAsync should clear Resource")]
    public async Task RVV018()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceSuccess();
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Act
        await _sut.CloseAsync();

        // Assert
        _sut.Resource.Should().BeNull();
    }

    [Fact(DisplayName = "RVV-019: CloseAsync should clear Result")]
    public async Task RVV019()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceSuccess();
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Act
        await _sut.CloseAsync();

        // Assert
        _sut.Result.Should().BeNull();
    }

    [Fact(DisplayName = "RVV-020: CloseAsync should clear InstanceId")]
    public async Task RVV020()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceSuccess();
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Act
        await _sut.CloseAsync();

        // Assert
        _sut.InstanceId.Should().BeNull();
    }

    [Fact(DisplayName = "RVV-021: RetryAsync should restart instance when InstanceId is null")]
    public async Task RVV021()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceFailure("First attempt failed");
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Reset mock for retry
        _toolInvocationServiceMock.Reset();
        SetupStartInstanceSuccess("instance-456");
        SetupReadResourceSuccess();

        // Act
        await _sut.RetryAsync();

        // Assert
        _toolInvocationServiceMock.Verify(
            x => x.StartInstanceAsync("http://localhost:5000", "test-server", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "RVV-022: RetryAsync should only reload resource when InstanceId exists")]
    public async Task RVV022()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceFailure("First read failed");
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Reset mock for retry
        _resourceViewerServiceMock.Reset();
        SetupReadResourceSuccess();

        // Act
        await _sut.RetryAsync();

        // Assert
        _resourceViewerServiceMock.Verify(
            x => x.ReadResourceAsync(
                "http://localhost:5000",
                "test-server",
                "instance-123",
                resource.Uri,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _toolInvocationServiceMock.Verify(
            x => x.StartInstanceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once); // Only the initial call, not a retry
    }

    [Fact(DisplayName = "RVV-023: IsOpen change should raise PropertyChanged")]
    public async Task RVV023()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        tracker.HasChanged(nameof(ResourceViewerViewModel.IsOpen)).Should().BeTrue();
    }

    [Fact(DisplayName = "RVV-024: Resource change should raise PropertyChanged")]
    public async Task RVV024()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        using var tracker = new PropertyChangedTracker(_sut);

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        tracker.HasChanged(nameof(ResourceViewerViewModel.Resource)).Should().BeTrue();
    }

    [Fact(DisplayName = "RVV-025: IsLoading should be false after successful load")]
    public async Task RVV025()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceSuccess();

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact(DisplayName = "RVV-026: IsStartingInstance should be false after successful start")]
    public async Task RVV026()
    {
        // Arrange
        var resource = CreateTestResource();
        SetupStartInstanceSuccess("instance-123");
        SetupReadResourceSuccess();

        // Act
        await _sut.OpenAsync(resource, "test-server", "http://localhost:5000");

        // Assert
        _sut.IsStartingInstance.Should().BeFalse();
    }

    private static McpServerResource CreateTestResource()
    {
        return new McpServerResource(
            "test-resource",
            "demo://resource/test",
            "Test Resource",
            "A test resource",
            "text/markdown");
    }

    private void SetupStartInstanceSuccess(string instanceId)
    {
        _toolInvocationServiceMock
            .Setup(x => x.StartInstanceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolInvocationServiceResult<string>.Success(instanceId));
    }

    private void SetupStartInstanceFailure(string error)
    {
        _toolInvocationServiceMock
            .Setup(x => x.StartInstanceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolInvocationServiceResult<string>.Failure(error));
    }

    private void SetupReadResourceSuccess(McpResourceReadResult? result = null)
    {
        result ??= new McpResourceReadResult(new[]
        {
            new McpResourceContent("demo://resource/test", "text/markdown", "# Test", null)
        });

        _resourceViewerServiceMock
            .Setup(x => x.ReadResourceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResourceViewerServiceResult<McpResourceReadResult>.Success(result));
    }

    private void SetupReadResourceFailure(string error)
    {
        _resourceViewerServiceMock
            .Setup(x => x.ReadResourceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResourceViewerServiceResult<McpResourceReadResult>.Failure(error));
    }

    public void Dispose()
    {
        // No disposal needed for this ViewModel
    }
}
