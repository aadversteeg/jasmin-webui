namespace Core.Infrastructure.BlazorApp.Services;

/// <summary>
/// Service for rendering markdown content to HTML.
/// </summary>
public interface IMarkdownRenderer
{
    /// <summary>
    /// Renders markdown content to HTML.
    /// </summary>
    /// <param name="markdown">The markdown content to render.</param>
    /// <returns>The rendered HTML.</returns>
    string RenderToHtml(string markdown);
}
