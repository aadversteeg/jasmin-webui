using System.Net.Http.Json;
using Core.Application.McpServers;
using Core.Domain.Events;
using Core.Infrastructure.JasminClient.Dtos;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Service for managing the MCP server list with real-time updates.
/// </summary>
public class McpServerListService : IMcpServerListService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpServerListService> _logger;
    private readonly Dictionary<string, McpServerListItem> _servers = new();

    public McpServerListService(HttpClient httpClient, ILogger<McpServerListService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<McpServerListItem> Servers => _servers.Values.OrderBy(s => s.Name).ToList();

    /// <inheritdoc />
    public event Action? ServersChanged;

    /// <inheritdoc />
    public async Task LoadAsync(string serverUrl)
    {
        try
        {
            var listUrl = BuildUrl(serverUrl, "/v1/mcp-servers");
            var serverList = await _httpClient.GetFromJsonAsync<List<McpServerListDto>>(listUrl);

            if (serverList == null)
            {
                return;
            }

            _servers.Clear();

            foreach (var server in serverList)
            {
                var escapedName = Uri.EscapeDataString(server.Name);
                var detailsUrl = BuildUrl(serverUrl, $"/v1/mcp-servers/{escapedName}?include=instances");
                var toolsUrl = BuildUrl(serverUrl, $"/v1/mcp-servers/{escapedName}/tools");

                var instanceCount = 0;
                DateTimeOffset? lastVerifiedAt = null;
                DateTimeOffset? lastMetadataUpdateAt = null;

                // Parse verification timestamp from the list response
                if (!string.IsNullOrEmpty(server.UpdatedAt))
                {
                    if (DateTimeOffset.TryParse(server.UpdatedAt, out var parsed))
                    {
                        lastVerifiedAt = parsed;
                    }
                }

                try
                {
                    var details = await _httpClient.GetFromJsonAsync<McpServerDetailsDto>(detailsUrl);
                    instanceCount = details?.Instances?.Count ?? 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch details for server {ServerName}", server.Name);
                }

                // Fetch metadata retrieval timestamp from tools endpoint
                try
                {
                    var toolsList = await _httpClient.GetFromJsonAsync<McpServerToolsListDto>(toolsUrl);
                    if (!string.IsNullOrEmpty(toolsList?.RetrievedAt))
                    {
                        if (DateTimeOffset.TryParse(toolsList.RetrievedAt, out var parsed))
                        {
                            lastMetadataUpdateAt = parsed;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch tools for server {ServerName}", server.Name);
                }

                var status = MapStatus(server.Status, instanceCount);
                _servers[server.Name] = new McpServerListItem(
                    server.Name, status, instanceCount, lastVerifiedAt, lastMetadataUpdateAt);
            }

            ServersChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load MCP servers from {Url}", serverUrl);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        _servers.Clear();
        ServersChanged?.Invoke();
    }

    /// <inheritdoc />
    public void HandleEvent(McpServerEvent evt)
    {
        switch (evt.EventType)
        {
            case McpServerEventType.ServerCreated:
                AddServer(evt.ServerName);
                break;

            case McpServerEventType.ServerDeleted:
                RemoveServer(evt.ServerName);
                break;

            case McpServerEventType.Started:
                IncrementInstances(evt.ServerName);
                SetStatus(evt.ServerName, McpServerStatus.Verified);
                UpdateVerifiedAt(evt.ServerName, evt.Timestamp);
                break;

            case McpServerEventType.Stopped:
                DecrementInstances(evt.ServerName);
                break;

            case McpServerEventType.StartFailed:
                SetStatus(evt.ServerName, McpServerStatus.Failed);
                break;

            case McpServerEventType.ToolsRetrieved:
            case McpServerEventType.PromptsRetrieved:
            case McpServerEventType.ResourcesRetrieved:
                UpdateMetadataAt(evt.ServerName, evt.Timestamp);
                break;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string serverUrl, string serverName)
    {
        try
        {
            var url = BuildUrl(serverUrl, $"/v1/mcp-servers/{Uri.EscapeDataString(serverName)}");
            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete server {ServerName}", serverName);
            throw;
        }
    }

    private void AddServer(string serverName)
    {
        if (!_servers.ContainsKey(serverName))
        {
            _servers[serverName] = new McpServerListItem(serverName, McpServerStatus.Unknown, 0);
            ServersChanged?.Invoke();
        }
    }

    private void RemoveServer(string serverName)
    {
        if (_servers.Remove(serverName))
        {
            ServersChanged?.Invoke();
        }
    }

    private void IncrementInstances(string serverName)
    {
        if (_servers.TryGetValue(serverName, out var server))
        {
            _servers[serverName] = server with { InstanceCount = server.InstanceCount + 1 };
            ServersChanged?.Invoke();
        }
        else
        {
            _servers[serverName] = new McpServerListItem(serverName, McpServerStatus.Verified, 1);
            ServersChanged?.Invoke();
        }
    }

    private void DecrementInstances(string serverName)
    {
        if (_servers.TryGetValue(serverName, out var server))
        {
            var newCount = Math.Max(0, server.InstanceCount - 1);
            _servers[serverName] = server with { InstanceCount = newCount };
            ServersChanged?.Invoke();
        }
    }

    private void SetStatus(string serverName, McpServerStatus status)
    {
        if (_servers.TryGetValue(serverName, out var server))
        {
            _servers[serverName] = server with { Status = status };
            ServersChanged?.Invoke();
        }
    }

    private void UpdateVerifiedAt(string serverName, DateTimeOffset timestamp)
    {
        if (_servers.TryGetValue(serverName, out var server))
        {
            _servers[serverName] = server with { LastVerifiedAt = timestamp };
            // No separate ServersChanged - already fired by SetStatus/IncrementInstances
        }
    }

    private void UpdateMetadataAt(string serverName, DateTimeOffset timestamp)
    {
        if (_servers.TryGetValue(serverName, out var server))
        {
            _servers[serverName] = server with { LastMetadataUpdateAt = timestamp };
            ServersChanged?.Invoke();
        }
    }

    private static McpServerStatus MapStatus(string status, int instanceCount)
    {
        return status.ToLowerInvariant() switch
        {
            "verified" => McpServerStatus.Verified,
            "failed" => McpServerStatus.Failed,
            _ => instanceCount > 0 ? McpServerStatus.Verified : McpServerStatus.Unknown
        };
    }

    private static string BuildUrl(string serverUrl, string path)
    {
        return serverUrl.TrimEnd('/') + path;
    }
}
