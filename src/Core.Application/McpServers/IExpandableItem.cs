namespace Core.Application.McpServers;

/// <summary>
/// Interface for items that can be displayed in expandable cards.
/// </summary>
public interface IExpandableItem
{
    string Name { get; }
    string? Title { get; }
    string? Description { get; }
}
