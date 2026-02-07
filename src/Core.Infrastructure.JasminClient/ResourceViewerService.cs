using System.Net.Http.Json;
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
            var request = new CreateRequestDto(
                Action: "readResource",
                InstanceId: instanceId,
                ResourceUri: resourceUri);

            var result = await ExecuteRequestAsync(serverUrl, serverName, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return ResourceViewerServiceResult<McpResourceReadResult>.Failure(result.Error!);
            }

            var response = result.Value!;
            if (response.ResourceOutput == null)
            {
                return ResourceViewerServiceResult<McpResourceReadResult>.Failure("No resource output in response");
            }

            var readResult = new McpResourceReadResult(
                response.ResourceOutput.Contents
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
            return ResourceViewerServiceResult<RequestResponseDto>.Failure($"Request failed: {response.StatusCode}");
        }

        var requestResponse = await response.Content.ReadFromJsonAsync<RequestResponseDto>(cancellationToken);
        if (requestResponse == null)
        {
            return ResourceViewerServiceResult<RequestResponseDto>.Failure("Invalid response from server");
        }

        // Poll for completion
        return await PollForCompletionAsync(serverUrl, serverName, requestResponse.RequestId, cancellationToken);
    }

    private async Task<ResourceViewerServiceResult<RequestResponseDto>> PollForCompletionAsync(
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
                return ResourceViewerServiceResult<RequestResponseDto>.Failure("Request timed out");
            }

            var response = await _httpClient.GetFromJsonAsync<RequestResponseDto>(url, cancellationToken);
            if (response == null)
            {
                return ResourceViewerServiceResult<RequestResponseDto>.Failure("Invalid response from server");
            }

            switch (response.Status.ToLowerInvariant())
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
