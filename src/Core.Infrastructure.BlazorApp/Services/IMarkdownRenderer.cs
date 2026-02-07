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

    /// <summary>
    /// Renders markdown content to HTML with internal link transformation.
    /// </summary>
    /// <param name="markdown">The markdown content to render.</param>
    /// <param name="currentResourceUri">The URI of the current resource (for resolving relative links).</param>
    /// <param name="pageBasePath">The base path of the page (e.g., /mcp-servers/everything).</param>
    /// <returns>The rendered HTML with internal links transformed to query param format.</returns>
    string RenderToHtml(string markdown, string currentResourceUri, string pageBasePath);
}
