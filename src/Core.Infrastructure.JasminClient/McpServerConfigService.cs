using System.Net;
using System.Net.Http.Json;
using Core.Application.McpServers;
using Core.Infrastructure.JasminClient.Dtos;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Service for managing MCP server configurations via the jasmin-server API.
/// </summary>
public class McpServerConfigService : IMcpServerConfigService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpServerConfigService> _logger;

    public McpServerConfigService(HttpClient httpClient, ILogger<McpServerConfigService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<McpServerConfigServiceResult> CreateServerAsync(
        string serverUrl,
        string name,
        string command,
        IReadOnlyList<string>? args,
        IReadOnlyDictionary<string, string>? env,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = BuildUrl(serverUrl, "/v1/mcp-servers");
            var request = new McpServerCreateDto(
                name,
                new McpServerConfigurationRequestDto(
                    command,
                    args?.ToList(),
                    env?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)));

            var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return McpServerConfigServiceResult.Success();
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return McpServerConfigServiceResult.Failure($"A server named '{name}' already exists");
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Create server failed with status {Status}: {Content}", response.StatusCode, errorContent);
            return McpServerConfigServiceResult.Failure($"Failed to create server: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create server {ServerName}", name);
            return McpServerConfigServiceResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<McpServerConfigServiceResult> UpdateConfigurationAsync(
        string serverUrl,
        string name,
        string command,
        IReadOnlyList<string>? args,
        IReadOnlyDictionary<string, string>? env,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = BuildUrl(serverUrl, $"/v1/mcp-servers/{Uri.EscapeDataString(name)}/configuration");
            var request = new McpServerConfigurationRequestDto(
                command,
                args?.ToList(),
                env?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            var response = await _httpClient.PutAsJsonAsync(url, request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return McpServerConfigServiceResult.Success();
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return McpServerConfigServiceResult.Failure($"Server '{name}' not found");
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Update configuration failed with status {Status}: {Content}", response.StatusCode, errorContent);
            return McpServerConfigServiceResult.Failure($"Failed to update configuration: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration for server {ServerName}", name);
            return McpServerConfigServiceResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<McpServerConfigServiceResult<McpServerConfiguration>> GetConfigurationAsync(
        string serverUrl,
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = BuildUrl(serverUrl, $"/v1/mcp-servers/{Uri.EscapeDataString(name)}/configuration");
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return McpServerConfigServiceResult<McpServerConfiguration>.Failure($"Server '{name}' not found or has no configuration");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Get configuration failed with status {Status}: {Content}", response.StatusCode, errorContent);
                return McpServerConfigServiceResult<McpServerConfiguration>.Failure($"Failed to get configuration: {response.StatusCode}");
            }

            var dto = await response.Content.ReadFromJsonAsync<McpServerConfigurationDto>(cancellationToken);
            if (dto == null)
            {
                return McpServerConfigServiceResult<McpServerConfiguration>.Failure("Invalid response from server");
            }

            var config = new McpServerConfiguration(
                dto.Command,
                dto.Args,
                dto.Env);

            return McpServerConfigServiceResult<McpServerConfiguration>.Success(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration for server {ServerName}", name);
            return McpServerConfigServiceResult<McpServerConfiguration>.Failure(ex.Message);
        }
    }

    private static string BuildUrl(string serverUrl, string path)
    {
        return serverUrl.TrimEnd('/') + path;
    }
}
