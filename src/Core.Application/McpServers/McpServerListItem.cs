namespace Core.Application.McpServers;

/// <summary>
/// Represents an MCP server in the server list with its current state.
/// </summary>
public record McpServerListItem(
    string Name,
    McpServerStatus Status,
    int InstanceCount,
    DateTimeOffset? LastVerifiedAt = null,
    DateTimeOffset? LastMetadataUpdateAt = null);
