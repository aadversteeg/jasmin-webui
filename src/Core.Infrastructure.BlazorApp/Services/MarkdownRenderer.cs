using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace Core.Infrastructure.BlazorApp.Services;

/// <summary>
/// Markdown renderer implementation using Markdig.
/// </summary>
public class MarkdownRenderer : IMarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownRenderer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();
    }

    /// <inheritdoc />
    public string RenderToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        return Markdown.ToHtml(markdown, _pipeline);
    }

    /// <inheritdoc />
    public string RenderToHtml(string markdown, string currentResourceUri, string pageBasePath)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        var document = Markdown.Parse(markdown, _pipeline);

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        _pipeline.Setup(renderer);

        // Replace the default link renderer with our custom one
        var defaultLinkRenderer = renderer.ObjectRenderers.FindExact<LinkInlineRenderer>();
        if (defaultLinkRenderer != null)
        {
            renderer.ObjectRenderers.Remove(defaultLinkRenderer);
        }
        renderer.ObjectRenderers.Add(new ResourceLinkRenderer(currentResourceUri, pageBasePath));

        renderer.Render(document);
        writer.Flush();

        return writer.ToString();
    }

    /// <summary>
    /// Resolves a relative link against a base resource URI.
    /// </summary>
    public static string? ResolveRelativeUri(string link, string baseResourceUri)
    {
        if (string.IsNullOrEmpty(link) || string.IsNullOrEmpty(baseResourceUri))
        {
            return null;
        }

        // If it's already an absolute URI with a scheme, return as-is
        if (Uri.TryCreate(link, UriKind.Absolute, out var absoluteUri) &&
            !string.IsNullOrEmpty(absoluteUri.Scheme) &&
            absoluteUri.Scheme != "file")
        {
            return link;
        }

        // Parse the base resource URI
        if (!Uri.TryCreate(baseResourceUri, UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        // Get the "directory" part of the base URI (everything before the last segment)
        var baseUriString = baseUri.ToString();
        var lastSlashIndex = baseUriString.LastIndexOf('/');
        if (lastSlashIndex <= baseUri.Scheme.Length + 2) // scheme://
        {
            // No path segments, can't resolve relative
            return $"{baseUri.Scheme}://{baseUri.Authority}/{link}";
        }

        var baseDirectory = baseUriString[..(lastSlashIndex + 1)];

        // Handle relative navigation (../, ./)
        if (Uri.TryCreate(new Uri(baseDirectory), link, out var resolvedUri))
        {
            return resolvedUri.ToString();
        }

        return null;
    }

    /// <summary>
    /// Custom link renderer that transforms internal links to use query param format.
    /// </summary>
    private class ResourceLinkRenderer : HtmlObjectRenderer<LinkInline>
    {
        private readonly string _currentResourceUri;
        private readonly string _pageBasePath;

        public ResourceLinkRenderer(string currentResourceUri, string pageBasePath)
        {
            _currentResourceUri = currentResourceUri;
            _pageBasePath = pageBasePath;
        }

        protected override void Write(HtmlRenderer renderer, LinkInline link)
        {
            var url = link.Url ?? string.Empty;
            var isInternalLink = IsInternalLink(url);

            if (link.IsImage)
            {
                // Render images normally
                RenderImage(renderer, link, url);
            }
            else if (isInternalLink)
            {
                // Transform internal links
                RenderInternalLink(renderer, link, url);
            }
            else
            {
                // Render external links normally
                RenderExternalLink(renderer, link, url);
            }
        }

        private bool IsInternalLink(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            // External protocols
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Anchor links
            if (url.StartsWith('#'))
            {
                return false;
            }

            // Check if it's a resource URI with same scheme as current
            if (Uri.TryCreate(_currentResourceUri, UriKind.Absolute, out var currentUri) &&
                Uri.TryCreate(url, UriKind.Absolute, out var linkUri))
            {
                // Same scheme means it's an internal resource link
                return string.Equals(currentUri.Scheme, linkUri.Scheme, StringComparison.OrdinalIgnoreCase);
            }

            // Relative paths are internal
            return true;
        }

        private void RenderImage(HtmlRenderer renderer, LinkInline link, string url)
        {
            renderer.Write("<img src=\"");
            renderer.WriteEscapeUrl(url);
            renderer.Write("\" alt=\"");
            renderer.WriteEscape(link.FirstChild?.ToString() ?? string.Empty);
            renderer.Write("\"");
            if (!string.IsNullOrEmpty(link.Title))
            {
                renderer.Write(" title=\"");
                renderer.WriteEscape(link.Title);
                renderer.Write("\"");
            }
            renderer.Write(" />");
        }

        private void RenderInternalLink(HtmlRenderer renderer, LinkInline link, string url)
        {
            // Resolve relative URL against current resource
            var resolvedUri = ResolveRelativeUri(url, _currentResourceUri);
            if (resolvedUri == null)
            {
                // Fallback to external link behavior
                RenderExternalLink(renderer, link, url);
                return;
            }

            // Build the transformed URL with query parameter
            var encodedResourceUri = Uri.EscapeDataString(resolvedUri);
            var transformedUrl = $"{_pageBasePath}?resource={encodedResourceUri}";

            renderer.Write("<a href=\"");
            renderer.Write(transformedUrl);
            renderer.Write("\" data-resource-link=\"true\"");
            if (!string.IsNullOrEmpty(link.Title))
            {
                renderer.Write(" title=\"");
                renderer.WriteEscape(link.Title);
                renderer.Write("\"");
            }
            renderer.Write(">");
            renderer.WriteChildren(link);
            renderer.Write("</a>");
        }

        private void RenderExternalLink(HtmlRenderer renderer, LinkInline link, string url)
        {
            renderer.Write("<a href=\"");
            renderer.WriteEscapeUrl(url);
            renderer.Write("\"");
            if (!string.IsNullOrEmpty(link.Title))
            {
                renderer.Write(" title=\"");
                renderer.WriteEscape(link.Title);
                renderer.Write("\"");
            }
            renderer.Write(">");
            renderer.WriteChildren(link);
            renderer.Write("</a>");
        }
    }
}
