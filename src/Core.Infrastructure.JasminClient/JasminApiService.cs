using System.Net.Http.Json;
using Core.Application.McpServers;
using Core.Infrastructure.JasminClient.Dtos;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Service for interacting with the jasmin-server REST API.
/// </summary>
public class JasminApiService : IJasminApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JasminApiService> _logger;

    public JasminApiService(HttpClient httpClient, ILogger<JasminApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<McpServerInfo>> GetMcpServersAsync(string serverUrl)
    {
        try
        {
            var url = BuildUrl(serverUrl, "/v1/mcp-servers");
            var response = await _httpClient.GetFromJsonAsync<List<McpServerListDto>>(url);

            if (response == null)
            {
                return [];
            }

            return response
                .Select(dto => new McpServerInfo(
                    dto.Name,
                    dto.Status,
                    string.IsNullOrEmpty(dto.UpdatedAt) ? null : DateTimeOffset.Parse(dto.UpdatedAt)))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch MCP servers from {Url}", serverUrl);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventTypeInfo>> GetEventTypesAsync(string serverUrl)
    {
        try
        {
            var url = BuildUrl(serverUrl, "/v1/events/types");
            var response = await _httpClient.GetFromJsonAsync<EventTypesResponseDto>(url);

            if (response?.EventTypes == null)
            {
                return [];
            }

            return response.EventTypes
                .Select(dto => new EventTypeInfo(
                    dto.Name,
                    dto.Category,
                    dto.Description))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch event types from {Url}", serverUrl);
            return [];
        }
    }

    private static string BuildUrl(string serverUrl, string path)
    {
        return serverUrl.TrimEnd('/') + path;
    }
}
