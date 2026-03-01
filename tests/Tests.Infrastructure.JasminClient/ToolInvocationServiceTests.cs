using System.Net;
using System.Text.Json;
using Core.Application.McpServers;
using Core.Infrastructure.JasminClient;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Infrastructure.JasminClient;

public class ToolInvocationServiceTests
{
    private readonly Mock<ILogger<ToolInvocationService>> _loggerMock = new();

    private ToolInvocationService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new ToolInvocationService(httpClient, _loggerMock.Object);
    }

    [Fact(DisplayName = "TIS-001: TestConfigurationAsync should return stderr lines on success")]
    public async Task TIS001()
    {
        // Arrange
        var requestId = "req-123";
        var handler = new MockHttpHandler(new[]
        {
            // POST /v1/requests -> 202 with request ID
            new MockResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(new
            {
                id = requestId,
                action = "mcp-server.test-configuration",
                target = (string?)null,
                status = "pending",
                createdAt = "2025-01-15T10:30:00Z",
                completedAt = (string?)null,
                parameters = (object?)null,
                output = (object?)null,
                errors = (object?)null
            })),
            // GET /v1/requests/{id} -> completed with stderr
            new MockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                id = requestId,
                action = "mcp-server.test-configuration",
                target = (string?)null,
                status = "completed",
                createdAt = "2025-01-15T10:30:00Z",
                completedAt = "2025-01-15T10:30:05Z",
                parameters = (object?)null,
                output = new { success = true, stderr = new[] { "Server starting...", "Listening on stdio" } },
                errors = (object?)null
            }))
        });

        var sut = CreateService(handler);

        // Act
        var result = await sut.TestConfigurationAsync(
            "http://localhost:5000",
            "npx",
            new List<string> { "-y", "@anthropic/mcp-server-time" },
            new Dictionary<string, string> { ["TZ"] = "UTC" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.StderrLines.Should().HaveCount(2);
        result.StderrLines[0].Should().Be("Server starting...");
        result.StderrLines[1].Should().Be("Listening on stdio");
    }

    [Fact(DisplayName = "TIS-002: TestConfigurationAsync should return error on failure")]
    public async Task TIS002()
    {
        // Arrange
        var requestId = "req-456";
        var handler = new MockHttpHandler(new[]
        {
            new MockResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(new
            {
                id = requestId,
                action = "mcp-server.test-configuration",
                target = (string?)null,
                status = "pending",
                createdAt = "2025-01-15T10:30:00Z",
                completedAt = (string?)null,
                parameters = (object?)null,
                output = (object?)null,
                errors = (object?)null
            })),
            new MockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                id = requestId,
                action = "mcp-server.test-configuration",
                target = (string?)null,
                status = "failed",
                createdAt = "2025-01-15T10:30:00Z",
                completedAt = "2025-01-15T10:30:30Z",
                parameters = (object?)null,
                output = new { success = false, stderr = new[] { "Error: command not found" } },
                errors = new[] { new { code = "McpServer.TestConfiguration.ConnectionFailed", message = "Failed to start MCP server" } }
            }))
        });

        var sut = CreateService(handler);

        // Act
        var result = await sut.TestConfigurationAsync(
            "http://localhost:5000",
            "nonexistent-server",
            null,
            null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed to start MCP server");
        result.StderrLines.Should().HaveCount(1);
        result.StderrLines[0].Should().Be("Error: command not found");
    }

    [Fact(DisplayName = "TIS-003: TestConfigurationAsync should return failure when POST fails")]
    public async Task TIS003()
    {
        // Arrange
        var handler = new MockHttpHandler(new[]
        {
            new MockResponse(HttpStatusCode.BadRequest, "{\"errors\":[{\"code\":\"McpServer.Parameters.CommandRequired\",\"message\":\"Command is required\"}]}")
        });

        var sut = CreateService(handler);

        // Act
        var result = await sut.TestConfigurationAsync(
            "http://localhost:5000",
            "",
            null,
            null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("BadRequest");
        result.StderrLines.Should().BeEmpty();
    }

    [Fact(DisplayName = "TIS-004: TestConfigurationAsync should propagate cancellation")]
    public async Task TIS004()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new MockHttpHandler(Array.Empty<MockResponse>());
        var sut = CreateService(handler);

        // Act & Assert
        await sut.Invoking(s => s.TestConfigurationAsync(
            "http://localhost:5000",
            "npx",
            null,
            null,
            cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "TIS-005: TestConfigurationAsync should return empty list when no stderr in output")]
    public async Task TIS005()
    {
        // Arrange
        var requestId = "req-789";
        var handler = new MockHttpHandler(new[]
        {
            new MockResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(new
            {
                id = requestId,
                action = "mcp-server.test-configuration",
                target = (string?)null,
                status = "pending",
                createdAt = "2025-01-15T10:30:00Z",
                completedAt = (string?)null,
                parameters = (object?)null,
                output = (object?)null,
                errors = (object?)null
            })),
            new MockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                id = requestId,
                action = "mcp-server.test-configuration",
                target = (string?)null,
                status = "completed",
                createdAt = "2025-01-15T10:30:00Z",
                completedAt = "2025-01-15T10:30:05Z",
                parameters = (object?)null,
                output = new { success = true },
                errors = (object?)null
            }))
        });

        var sut = CreateService(handler);

        // Act
        var result = await sut.TestConfigurationAsync(
            "http://localhost:5000",
            "npx",
            null,
            null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.StderrLines.Should().BeEmpty();
    }

    [Fact(DisplayName = "TIS-006: TestConfigurationAsync should send null target in request")]
    public async Task TIS006()
    {
        // Arrange
        string? capturedBody = null;
        var requestId = "req-abc";
        var handler = new MockHttpHandler(new[]
        {
            new MockResponse(HttpStatusCode.Accepted, JsonSerializer.Serialize(new
            {
                id = requestId,
                action = "mcp-server.test-configuration",
                target = (string?)null,
                status = "completed",
                createdAt = "2025-01-15T10:30:00Z",
                completedAt = "2025-01-15T10:30:05Z",
                parameters = (object?)null,
                output = new { success = true, stderr = Array.Empty<string>() },
                errors = (object?)null
            }))
        }, onRequest: body => capturedBody = body);

        var sut = CreateService(handler);

        // Act
        await sut.TestConfigurationAsync(
            "http://localhost:5000",
            "npx",
            new List<string> { "-y", "server" },
            null);

        // Assert
        capturedBody.Should().NotBeNull();
        var json = JsonDocument.Parse(capturedBody!);
        json.RootElement.GetProperty("action").GetString().Should().Be("mcp-server.test-configuration");
        json.RootElement.GetProperty("target").ValueKind.Should().Be(JsonValueKind.Null);
        json.RootElement.GetProperty("parameters").GetProperty("command").GetString().Should().Be("npx");
    }

    private record MockResponse(HttpStatusCode StatusCode, string Body);

    private class MockHttpHandler : HttpMessageHandler
    {
        private readonly MockResponse[] _responses;
        private readonly Action<string>? _onRequest;
        private int _index;

        public MockHttpHandler(MockResponse[] responses, Action<string>? onRequest = null)
        {
            _responses = responses;
            _onRequest = onRequest;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request.Content != null)
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken);
                _onRequest?.Invoke(body);
            }

            if (_index >= _responses.Length)
            {
                throw new InvalidOperationException($"No more mock responses available (index: {_index})");
            }

            var mockResponse = _responses[_index++];
            return new HttpResponseMessage(mockResponse.StatusCode)
            {
                Content = new StringContent(mockResponse.Body, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
