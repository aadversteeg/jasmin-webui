using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.McpServers;
using Core.Domain.Events;
using Core.Infrastructure.JasminClient.Dtos;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Service for fetching MCP server details (configuration, tools, prompts, resources).
/// </summary>
public class McpServerDetailService : IMcpServerDetailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpServerDetailService> _logger;

    public McpServerDetailService(HttpClient httpClient, ILogger<McpServerDetailService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public event Action<string>? DataChanged;

    /// <inheritdoc />
    public async Task<McpServerConfiguration?> GetConfigurationAsync(string serverUrl, string serverName)
    {
        try
        {
            var escapedName = Uri.EscapeDataString(serverName);
            var url = BuildUrl(serverUrl, $"/v1/mcp-servers/{escapedName}/configuration");
            var dto = await _httpClient.GetFromJsonAsync<McpServerConfigurationDto>(url);

            if (dto == null)
            {
                return null;
            }

            return new McpServerConfiguration(dto.Command, dto.Args, dto.Env);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch configuration for server {ServerName}", serverName);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<McpServerMetadataResult<McpServerTool>> GetToolsAsync(string serverUrl, string serverName)
    {
        try
        {
            var escapedName = Uri.EscapeDataString(serverName);
            var url = BuildUrl(serverUrl, $"/v1/mcp-servers/{escapedName}/tools");
            var dto = await _httpClient.GetFromJsonAsync<McpServerToolsListDto>(url);

            var items = dto?.Items?.Select(t => new McpServerTool(
                t.Name,
                t.Title,
                t.Description,
                t.InputSchema != null
                    ? ToolInputSchemaParser.Parse(JsonSerializer.Serialize(t.InputSchema))
                    : null
            )).ToList() ?? new List<McpServerTool>();

            DateTimeOffset? retrievedAt = null;
            if (!string.IsNullOrEmpty(dto?.RetrievedAt) && DateTimeOffset.TryParse(dto.RetrievedAt, out var parsed))
            {
                retrievedAt = parsed;
            }

            var errors = dto?.Errors?.Select(e => new McpServerMetadataError(e.Code, e.Message)).ToList();

            return new McpServerMetadataResult<McpServerTool>(items, retrievedAt, errors);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch tools for server {ServerName}", serverName);
            return new McpServerMetadataResult<McpServerTool>(
                new List<McpServerTool>(),
                null,
                new List<McpServerMetadataError> { new("FETCH_ERROR", ex.Message) });
        }
    }

    /// <inheritdoc />
    public async Task<McpServerMetadataResult<McpServerPrompt>> GetPromptsAsync(string serverUrl, string serverName)
    {
        try
        {
            var escapedName = Uri.EscapeDataString(serverName);
            var url = BuildUrl(serverUrl, $"/v1/mcp-servers/{escapedName}/prompts");
            var dto = await _httpClient.GetFromJsonAsync<McpServerPromptsListDto>(url);

            var items = dto?.Items?.Select(p => new McpServerPrompt(
                p.Name,
                p.Title,
                p.Description,
                p.Arguments?.Select(a => new McpServerPromptArgument(a.Name, a.Description, a.Required)).ToList()
                    ?? new List<McpServerPromptArgument>()
            )).ToList() ?? new List<McpServerPrompt>();

            DateTimeOffset? retrievedAt = null;
            if (!string.IsNullOrEmpty(dto?.RetrievedAt) && DateTimeOffset.TryParse(dto.RetrievedAt, out var parsed))
            {
                retrievedAt = parsed;
            }

            var errors = dto?.Errors?.Select(e => new McpServerMetadataError(e.Code, e.Message)).ToList();

            return new McpServerMetadataResult<McpServerPrompt>(items, retrievedAt, errors);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch prompts for server {ServerName}", serverName);
            return new McpServerMetadataResult<McpServerPrompt>(
                new List<McpServerPrompt>(),
                null,
                new List<McpServerMetadataError> { new("FETCH_ERROR", ex.Message) });
        }
    }

    /// <inheritdoc />
    public async Task<McpServerMetadataResult<McpServerResource>> GetResourcesAsync(string serverUrl, string serverName)
    {
        try
        {
            var escapedName = Uri.EscapeDataString(serverName);
            var url = BuildUrl(serverUrl, $"/v1/mcp-servers/{escapedName}/resources");
            var dto = await _httpClient.GetFromJsonAsync<McpServerResourcesListDto>(url);

            var items = dto?.Items?.Select(r => new McpServerResource(
                r.Name,
                r.Uri,
                r.Title,
                r.Description,
                r.MimeType
            )).ToList() ?? new List<McpServerResource>();

            DateTimeOffset? retrievedAt = null;
            if (!string.IsNullOrEmpty(dto?.RetrievedAt) && DateTimeOffset.TryParse(dto.RetrievedAt, out var parsed))
            {
                retrievedAt = parsed;
            }

            var errors = dto?.Errors?.Select(e => new McpServerMetadataError(e.Code, e.Message)).ToList();

            return new McpServerMetadataResult<McpServerResource>(items, retrievedAt, errors);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch resources for server {ServerName}", serverName);
            return new McpServerMetadataResult<McpServerResource>(
                new List<McpServerResource>(),
                null,
                new List<McpServerMetadataError> { new("FETCH_ERROR", ex.Message) });
        }
    }

    /// <inheritdoc />
    public void HandleEvent(McpServerEvent evt)
    {
        switch (evt.EventType)
        {
            case McpServerEventType.ConfigurationCreated:
            case McpServerEventType.ConfigurationUpdated:
            case McpServerEventType.ToolsRetrieved:
            case McpServerEventType.PromptsRetrieved:
            case McpServerEventType.ResourcesRetrieved:
                DataChanged?.Invoke(evt.ServerName);
                break;
        }
    }

    private static string BuildUrl(string serverUrl, string path)
    {
        return serverUrl.TrimEnd('/') + path;
    }
}
