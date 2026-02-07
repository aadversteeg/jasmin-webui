using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.McpServers;
using Core.Infrastructure.JasminClient.Dtos;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Service for invoking prompts on MCP servers via the jasmin-server API.
/// </summary>
public class PromptInvocationService : IPromptInvocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PromptInvocationService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaxWaitTime = TimeSpan.FromMinutes(5);

    public PromptInvocationService(HttpClient httpClient, ILogger<PromptInvocationService> logger)
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
            var request = new CreateRequestDto("start", null, null, null, null, null);
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
    public async Task<ToolInvocationServiceResult<PromptInvocationResult>> GetPromptAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        string promptName,
        Dictionary<string, string?>? arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            JsonElement? argumentsElement = null;
            if (arguments != null && arguments.Count > 0)
            {
                var json = JsonSerializer.Serialize(arguments);
                argumentsElement = JsonDocument.Parse(json).RootElement;
            }

            var request = new CreateRequestDto("getPrompt", instanceId, null, null, promptName, argumentsElement);
            var result = await ExecuteRequestAsync(serverUrl, serverName, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return ToolInvocationServiceResult<PromptInvocationResult>.Failure(result.Error!);
            }

            var response = result.Value!;
            if (response.PromptOutput == null)
            {
                return ToolInvocationServiceResult<PromptInvocationResult>.Failure("No prompt output in response");
            }

            var invocationResult = new PromptInvocationResult(
                response.PromptOutput.Messages.Select(m => new PromptMessage(
                    m.Role,
                    new PromptMessageContent(
                        m.Content.Type,
                        m.Content.Text,
                        m.Content.MimeType,
                        m.Content.Data,
                        m.Content.Uri))).ToList(),
                response.PromptOutput.Description);

            return ToolInvocationServiceResult<PromptInvocationResult>.Success(invocationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get prompt {PromptName} on server {ServerName}", promptName, serverName);
            return ToolInvocationServiceResult<PromptInvocationResult>.Failure(ex.Message);
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
            var request = new CreateRequestDto("stop", instanceId, null, null, null, null);
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
