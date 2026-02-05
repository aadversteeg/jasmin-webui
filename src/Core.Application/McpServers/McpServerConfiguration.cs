namespace Core.Application.McpServers;

/// <summary>
/// Represents the configuration of an MCP server.
/// </summary>
public record McpServerConfiguration(
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);
