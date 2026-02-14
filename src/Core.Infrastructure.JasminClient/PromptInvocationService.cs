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
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PromptInvocationService(HttpClient httpClient, ILogger<PromptInvocationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StartInstanceResult> StartInstanceAsync(
        string serverUrl,
        string serverName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var target = TargetHelper.BuildServerTarget(serverName);
            var request = new CreateRequestDto("mcp-server.start", target);
            var result = await ExecuteRequestAsync(serverUrl, request, cancellationToken, returnResponseOnFailure: true);

            if (!result.IsSuccess)
            {
                // POST itself failed (HTTP error), no response available
                return new StartInstanceResult(null, Array.Empty<string>(), result.Error!);
            }

            var response = result.Value!;
            var stderrLines = ExtractStderrLines(response);

            if (response.Status == "failed")
            {
                var errorMessage = response.Errors?.FirstOrDefault()?.Message ?? "Failed to start instance";
                return new StartInstanceResult(null, stderrLines, errorMessage);
            }

            if (!response.Output.HasValue)
            {
                return new StartInstanceResult(null, stderrLines, "No instance ID returned");
            }

            var output = response.Output.Value;
            if (output.TryGetProperty("instanceId", out var instanceIdElement))
            {
                var instanceId = instanceIdElement.GetString();
                if (!string.IsNullOrEmpty(instanceId))
                {
                    return new StartInstanceResult(instanceId, stderrLines, null);
                }
            }

            return new StartInstanceResult(null, stderrLines, "No instance ID returned");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start instance for server {ServerName}", serverName);
            return new StartInstanceResult(null, Array.Empty<string>(), ex.Message);
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
            var target = TargetHelper.BuildInstanceTarget(serverName, instanceId);
            var parameters = new Dictionary<string, object?> { ["promptName"] = promptName };
            if (arguments != null && arguments.Count > 0)
            {
                parameters["arguments"] = arguments;
            }
            var parametersJson = JsonSerializer.SerializeToElement(parameters);

            var request = new CreateRequestDto("mcp-server.instance.get-prompt", target, parametersJson);
            var result = await ExecuteRequestAsync(serverUrl, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return ToolInvocationServiceResult<PromptInvocationResult>.Failure(result.Error!);
            }

            var response = result.Value!;
            if (!response.Output.HasValue)
            {
                return ToolInvocationServiceResult<PromptInvocationResult>.Failure("No prompt output in response");
            }

            var output = JsonSerializer.Deserialize<PromptOutputDto>(
                response.Output.Value.GetRawText(), JsonOptions);
            if (output == null)
            {
                return ToolInvocationServiceResult<PromptInvocationResult>.Failure("Failed to parse output");
            }

            var invocationResult = new PromptInvocationResult(
                output.Messages.Select(m => new PromptMessage(
                    m.Role,
                    new PromptMessageContent(
                        m.Content.Type,
                        m.Content.Text,
                        m.Content.MimeType,
                        m.Content.Data,
                        m.Content.Uri))).ToList(),
                output.Description);

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
            var target = TargetHelper.BuildInstanceTarget(serverName, instanceId);
            var request = new CreateRequestDto("mcp-server.instance.stop", target);
            var result = await ExecuteRequestAsync(serverUrl, request, cancellationToken);

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

    private async Task<ToolInvocationServiceResult<RequestResponseDto>> ExecuteRequestAsync(
        string serverUrl,
        CreateRequestDto request,
        CancellationToken cancellationToken,
        bool returnResponseOnFailure = false)
    {
        var url = BuildUrl(serverUrl, "/v1/requests");

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

        return await PollForCompletionAsync(serverUrl, requestResponse.Id, cancellationToken, returnResponseOnFailure);
    }

    private async Task<ToolInvocationServiceResult<RequestResponseDto>> PollForCompletionAsync(
        string serverUrl,
        string requestId,
        CancellationToken cancellationToken,
        bool returnResponseOnFailure = false)
    {
        var url = BuildUrl(serverUrl, $"/v1/requests/{Uri.EscapeDataString(requestId)}");
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

            switch (response.Status)
            {
                case "completed":
                    return ToolInvocationServiceResult<RequestResponseDto>.Success(response);

                case "failed":
                    if (returnResponseOnFailure)
                        return ToolInvocationServiceResult<RequestResponseDto>.Success(response);
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

    private static IReadOnlyList<string> ExtractStderrLines(RequestResponseDto response)
    {
        if (!response.Output.HasValue)
        {
            return Array.Empty<string>();
        }

        if (!response.Output.Value.TryGetProperty("stderr", out var stderr))
        {
            return Array.Empty<string>();
        }

        if (stderr.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return stderr.EnumerateArray()
            .Select(e => e.GetString() ?? "")
            .ToList();
    }

    private static string BuildUrl(string serverUrl, string path)
    {
        return serverUrl.TrimEnd('/') + path;
    }
}
