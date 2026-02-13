using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.McpServers;
using Core.Infrastructure.JasminClient.Dtos;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Service for reading resources from MCP servers via the jasmin-server API.
/// </summary>
public class ResourceViewerService : IResourceViewerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResourceViewerService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaxWaitTime = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ResourceViewerService(HttpClient httpClient, ILogger<ResourceViewerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResourceViewerServiceResult<McpResourceReadResult>> ReadResourceAsync(
        string serverUrl,
        string serverName,
        string instanceId,
        string resourceUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var target = TargetHelper.BuildInstanceTarget(serverName, instanceId);
            var parameters = new Dictionary<string, object?> { ["resourceUri"] = resourceUri };
            var parametersJson = JsonSerializer.SerializeToElement(parameters);

            var request = new CreateRequestDto("mcp-server.instance.read-resource", target, parametersJson);
            var result = await ExecuteRequestAsync(serverUrl, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return ResourceViewerServiceResult<McpResourceReadResult>.Failure(result.Error!);
            }

            var response = result.Value!;
            if (!response.Output.HasValue)
            {
                return ResourceViewerServiceResult<McpResourceReadResult>.Failure("No resource output in response");
            }

            var output = JsonSerializer.Deserialize<ResourceReadOutputDto>(
                response.Output.Value.GetRawText(), JsonOptions);
            if (output == null)
            {
                return ResourceViewerServiceResult<McpResourceReadResult>.Failure("Failed to parse output");
            }

            var readResult = new McpResourceReadResult(
                output.Contents
                    .Select(c => new McpResourceContent(c.Uri, c.MimeType, c.Text, c.Blob))
                    .ToList());

            return ResourceViewerServiceResult<McpResourceReadResult>.Success(readResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read resource {ResourceUri} from server {ServerName}", resourceUri, serverName);
            return ResourceViewerServiceResult<McpResourceReadResult>.Failure(ex.Message);
        }
    }

    private async Task<ResourceViewerServiceResult<RequestResponseDto>> ExecuteRequestAsync(
        string serverUrl,
        CreateRequestDto request,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(serverUrl, "/v1/requests");

        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Request failed with status {Status}: {Content}", response.StatusCode, errorContent);
            return ResourceViewerServiceResult<RequestResponseDto>.Failure($"Request failed: {response.StatusCode}");
        }

        var requestResponse = await response.Content.ReadFromJsonAsync<RequestResponseDto>(cancellationToken);
        if (requestResponse == null)
        {
            return ResourceViewerServiceResult<RequestResponseDto>.Failure("Invalid response from server");
        }

        return await PollForCompletionAsync(serverUrl, requestResponse.Id, cancellationToken);
    }

    private async Task<ResourceViewerServiceResult<RequestResponseDto>> PollForCompletionAsync(
        string serverUrl,
        string requestId,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(serverUrl, $"/v1/requests/{Uri.EscapeDataString(requestId)}");
        var startTime = DateTime.UtcNow;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (DateTime.UtcNow - startTime > MaxWaitTime)
            {
                return ResourceViewerServiceResult<RequestResponseDto>.Failure("Request timed out");
            }

            var response = await _httpClient.GetFromJsonAsync<RequestResponseDto>(url, cancellationToken);
            if (response == null)
            {
                return ResourceViewerServiceResult<RequestResponseDto>.Failure("Invalid response from server");
            }

            switch (response.Status)
            {
                case "completed":
                    return ResourceViewerServiceResult<RequestResponseDto>.Success(response);

                case "failed":
                    var errorMessage = response.Errors?.FirstOrDefault()?.Message ?? "Request failed";
                    return ResourceViewerServiceResult<RequestResponseDto>.Failure(errorMessage);

                case "pending":
                case "running":
                    await Task.Delay(PollInterval, cancellationToken);
                    break;

                default:
                    return ResourceViewerServiceResult<RequestResponseDto>.Failure($"Unknown status: {response.Status}");
            }
        }
    }

    private static string BuildUrl(string serverUrl, string path)
    {
        return serverUrl.TrimEnd('/') + path;
    }
}
