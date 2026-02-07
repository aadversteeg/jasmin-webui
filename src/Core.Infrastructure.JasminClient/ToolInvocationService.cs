using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.McpServers;
using Core.Infrastructure.JasminClient.Dtos;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Service for invoking tools on MCP servers via the jasmin-server API.
/// </summary>
public class ToolInvocationService : IToolInvocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ToolInvocationService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaxWaitTime = TimeSpan.FromMinutes(5);

    public ToolInvocationService(HttpClient httpClient, ILogger<ToolInvocationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ToolInvocationServiceResult<string>> StartInstanceAsync(
        string serverUrl,
        string serverName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new CreateRequestDto("start", null, null, null);
            var result = await ExecuteRequestAsync(serverUrl, serverName, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return ToolInvocationServiceResult<string>.Failure(result.Error!);
            }

            var response = result.Value!;
            if (string.IsNullOrEmpty(response.ResultInstanceId))
            {
                return ToolInvocationServiceResult<string>.Failure("No instance ID returned");
            }

            return ToolInvocationServiceResult<string>.Success(response.ResultInstanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start instance for server {ServerName}", serverName);
            return ToolInvocationServiceResult<string>.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ToolInvocationServiceResult<ToolInvocationResult>> InvokeToolAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        string toolName,
        Dictionary<string, object?>? input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            JsonElement? inputElement = null;
            if (input != null && input.Count > 0)
            {
                var json = JsonSerializer.Serialize(input);
                inputElement = JsonDocument.Parse(json).RootElement;
            }

            var request = new CreateRequestDto("invokeTool", instanceId, toolName, inputElement);
            var result = await ExecuteRequestAsync(serverUrl, serverName, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return ToolInvocationServiceResult<ToolInvocationResult>.Failure(result.Error!);
            }

            var response = result.Value!;
            if (response.Output == null)
            {
                return ToolInvocationServiceResult<ToolInvocationResult>.Failure("No output in response");
            }

            var invocationResult = new ToolInvocationResult(
                response.Output.Content.Select(c => new ToolContentBlock(
                    c.Type,
                    c.Text,
                    c.MimeType,
                    c.Data,
                    c.Uri)).ToList(),
                response.Output.StructuredContent,
                response.Output.IsError);

            return ToolInvocationServiceResult<ToolInvocationResult>.Success(invocationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke tool {ToolName} on server {ServerName}", toolName, serverName);
            return ToolInvocationServiceResult<ToolInvocationResult>.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ToolInvocationServiceResult> StopInstanceAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new CreateRequestDto("stop", instanceId, null, null);
            var result = await ExecuteRequestAsync(serverUrl, serverName, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return ToolInvocationServiceResult.Failure(result.Error!);
            }

            return ToolInvocationServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop instance {InstanceId} on server {ServerName}", instanceId, serverName);
            return ToolInvocationServiceResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ToolInvocationServiceResult<IReadOnlyList<McpServerInstance>>> GetInstancesAsync(
        string serverUrl,
        string serverName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = BuildUrl(serverUrl, $"/v1/mcp-servers/{Uri.EscapeDataString(serverName)}/instances");
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Get instances failed with status {Status}: {Content}", response.StatusCode, errorContent);
                return ToolInvocationServiceResult<IReadOnlyList<McpServerInstance>>.Failure($"Failed to get instances: {response.StatusCode}");
            }

            var listResponse = await response.Content.ReadFromJsonAsync<InstanceListResponseDto>(cancellationToken);
            if (listResponse == null)
            {
                return ToolInvocationServiceResult<IReadOnlyList<McpServerInstance>>.Failure("Invalid response from server");
            }

            var instances = listResponse.Items.Select(i => new McpServerInstance(
                i.InstanceId,
                i.ServerName,
                DateTimeOffset.Parse(i.StartedAt))).ToList();

            return ToolInvocationServiceResult<IReadOnlyList<McpServerInstance>>.Success(instances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get instances for server {ServerName}", serverName);
            return ToolInvocationServiceResult<IReadOnlyList<McpServerInstance>>.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ToolInvocationServiceResult> RefreshMetadataAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new CreateRequestDto("refreshMetadata", instanceId, null, null);
            var result = await ExecuteRequestAsync(serverUrl, serverName, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return ToolInvocationServiceResult.Failure(result.Error!);
            }

            return ToolInvocationServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh metadata for instance {InstanceId} on server {ServerName}", instanceId, serverName);
            return ToolInvocationServiceResult.Failure(ex.Message);
        }
    }

    private async Task<ToolInvocationServiceResult<RequestResponseDto>> ExecuteRequestAsync(
        string serverUrl,
        string serverName,
        CreateRequestDto request,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(serverUrl, $"/v1/mcp-servers/{Uri.EscapeDataString(serverName)}/requests");

        // Create the request
        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Request failed with status {Status}: {Content}", response.StatusCode, errorContent);
            return ToolInvocationServiceResult<RequestResponseDto>.Failure($"Request failed: {response.StatusCode}");
        }

        var requestResponse = await response.Content.ReadFromJsonAsync<RequestResponseDto>(cancellationToken);
        if (requestResponse == null)
        {
            return ToolInvocationServiceResult<RequestResponseDto>.Failure("Invalid response from server");
        }

        // Poll for completion
        return await PollForCompletionAsync(serverUrl, serverName, requestResponse.RequestId, cancellationToken);
    }

    private async Task<ToolInvocationServiceResult<RequestResponseDto>> PollForCompletionAsync(
        string serverUrl,
        string serverName,
        string requestId,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(serverUrl, $"/v1/mcp-servers/{Uri.EscapeDataString(serverName)}/requests/{Uri.EscapeDataString(requestId)}");
        var startTime = DateTime.UtcNow;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (DateTime.UtcNow - startTime > MaxWaitTime)
            {
                return ToolInvocationServiceResult<RequestResponseDto>.Failure("Request timed out");
            }

            var response = await _httpClient.GetFromJsonAsync<RequestResponseDto>(url, cancellationToken);
            if (response == null)
            {
                return ToolInvocationServiceResult<RequestResponseDto>.Failure("Invalid response from server");
            }

            switch (response.Status.ToLowerInvariant())
            {
                case "completed":
                    return ToolInvocationServiceResult<RequestResponseDto>.Success(response);

                case "failed":
                    var errorMessage = response.Errors?.FirstOrDefault()?.Message ?? "Request failed";
                    return ToolInvocationServiceResult<RequestResponseDto>.Failure(errorMessage);

                case "pending":
                case "running":
                    await Task.Delay(PollInterval, cancellationToken);
                    break;

                default:
                    return ToolInvocationServiceResult<RequestResponseDto>.Failure($"Unknown status: {response.Status}");
            }
        }
    }

    private static string BuildUrl(string serverUrl, string path)
    {
        return serverUrl.TrimEnd('/') + path;
    }
}
