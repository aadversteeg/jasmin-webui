using Core.Infrastructure.BlazorApp.Services;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.BlazorApp.Services;

public class MarkdownRendererTests
{
    private readonly MarkdownRenderer _sut;

    public MarkdownRendererTests()
    {
        _sut = new MarkdownRenderer();
    }

    [Fact(DisplayName = "MDR-001: RenderToHtml should return empty string for null input")]
    public void MDR001()
    {
        // Act
        var result = _sut.RenderToHtml(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "MDR-002: RenderToHtml should return empty string for empty input")]
    public void MDR002()
    {
        // Act
        var result = _sut.RenderToHtml(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "MDR-003: RenderToHtml should convert heading level 1")]
    public void MDR003()
    {
        // Arrange
        var markdown = "# Hello World";

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert - Markdig adds IDs to headings with advanced extensions
        result.Should().Contain("<h1");
        result.Should().Contain(">Hello World</h1>");
    }

    [Fact(DisplayName = "MDR-004: RenderToHtml should convert heading level 2")]
    public void MDR004()
    {
        // Arrange
        var markdown = "## Section Title";

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert - Markdig adds IDs to headings with advanced extensions
        result.Should().Contain("<h2");
        result.Should().Contain(">Section Title</h2>");
    }

    [Fact(DisplayName = "MDR-005: RenderToHtml should convert bold text")]
    public void MDR005()
    {
        // Arrange
        var markdown = "This is **bold** text";

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert
        result.Should().Contain("<strong>bold</strong>");
    }

    [Fact(DisplayName = "MDR-006: RenderToHtml should convert italic text")]
    public void MDR006()
    {
        // Arrange
        var markdown = "This is *italic* text";

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert
        result.Should().Contain("<em>italic</em>");
    }

    [Fact(DisplayName = "MDR-007: RenderToHtml should convert inline code")]
    public void MDR007()
    {
        // Arrange
        var markdown = "Use `Console.WriteLine()` to print";

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert
        result.Should().Contain("<code>Console.WriteLine()</code>");
    }

    [Fact(DisplayName = "MDR-008: RenderToHtml should convert code blocks")]
    public void MDR008()
    {
        // Arrange
        var markdown = """
            ```csharp
            var x = 1;
            ```
            """;

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert
        result.Should().Contain("<pre>");
        result.Should().Contain("<code");
        result.Should().Contain("var x = 1;");
    }

    [Fact(DisplayName = "MDR-009: RenderToHtml should convert links")]
    public void MDR009()
    {
        // Arrange
        var markdown = "Visit [Google](https://google.com)";

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert
        result.Should().Contain("<a href=\"https://google.com\"");
        result.Should().Contain(">Google</a>");
    }

    [Fact(DisplayName = "MDR-010: RenderToHtml should convert unordered lists")]
    public void MDR010()
    {
        // Arrange
        var markdown = """
            - Item 1
            - Item 2
            - Item 3
            """;

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert
        result.Should().Contain("<ul>");
        result.Should().Contain("<li>Item 1</li>");
        result.Should().Contain("<li>Item 2</li>");
        result.Should().Contain("<li>Item 3</li>");
        result.Should().Contain("</ul>");
    }

    [Fact(DisplayName = "MDR-011: RenderToHtml should convert ordered lists")]
    public void MDR011()
    {
        // Arrange
        var markdown = """
            1. First
            2. Second
            3. Third
            """;

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert
        result.Should().Contain("<ol>");
        result.Should().Contain("<li>First</li>");
        result.Should().Contain("<li>Second</li>");
        result.Should().Contain("<li>Third</li>");
        result.Should().Contain("</ol>");
    }

    [Fact(DisplayName = "MDR-012: RenderToHtml should convert blockquotes")]
    public void MDR012()
    {
        // Arrange
        var markdown = "> This is a quote";

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert
        result.Should().Contain("<blockquote>");
        result.Should().Contain("This is a quote");
        result.Should().Contain("</blockquote>");
    }

    [Fact(DisplayName = "MDR-013: RenderToHtml should convert paragraphs")]
    public void MDR013()
    {
        // Arrange
        var markdown = "This is a paragraph.";

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert
        result.Should().Contain("<p>This is a paragraph.</p>");
    }

    [Fact(DisplayName = "MDR-014: RenderToHtml should handle complex markdown")]
    public void MDR014()
    {
        // Arrange
        var markdown = """
            # Architecture

            This is a **complex** document with:

            - *Lists*
            - `Code`
            - [Links](https://example.com)

            ## Section 2

            > A quote

            ```js
            const x = 1;
            ```
            """;

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert - Markdig adds IDs to headings with advanced extensions
        result.Should().Contain("<h1");
        result.Should().Contain(">Architecture</h1>");
        result.Should().Contain("<h2");
        result.Should().Contain(">Section 2</h2>");
        result.Should().Contain("<strong>complex</strong>");
        result.Should().Contain("<em>Lists</em>");
        result.Should().Contain("<code>Code</code>");
        result.Should().Contain("<a href=\"https://example.com\"");
        result.Should().Contain("<blockquote>");
        result.Should().Contain("<pre>");
    }

    [Fact(DisplayName = "MDR-015: RenderToHtml should convert line breaks")]
    public void MDR015()
    {
        // Arrange - with soft line break as hard line break enabled
        var markdown = "Line 1\nLine 2";

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert
        result.Should().Contain("<br />");
    }

    // Link transformation tests

    [Fact(DisplayName = "MDR-016: ResolveRelativeUri should resolve simple relative path")]
    public void MDR016()
    {
        // Arrange
        var link = "features.md";
        var baseUri = "demo://resource/architecture.md";

        // Act
        var result = MarkdownRenderer.ResolveRelativeUri(link, baseUri);

        // Assert
        result.Should().Be("demo://resource/features.md");
    }

    [Fact(DisplayName = "MDR-017: ResolveRelativeUri should resolve parent directory navigation")]
    public void MDR017()
    {
        // Arrange
        var link = "../features.md";
        var baseUri = "demo://resource/docs/architecture.md";

        // Act
        var result = MarkdownRenderer.ResolveRelativeUri(link, baseUri);

        // Assert
        result.Should().Be("demo://resource/features.md");
    }

    [Fact(DisplayName = "MDR-018: ResolveRelativeUri should return absolute URI unchanged")]
    public void MDR018()
    {
        // Arrange
        var link = "https://google.com";
        var baseUri = "demo://resource/architecture.md";

        // Act
        var result = MarkdownRenderer.ResolveRelativeUri(link, baseUri);

        // Assert
        result.Should().Be("https://google.com");
    }

    [Fact(DisplayName = "MDR-019: ResolveRelativeUri should return null for empty inputs")]
    public void MDR019()
    {
        // Act & Assert
        MarkdownRenderer.ResolveRelativeUri("", "demo://resource/test.md").Should().BeNull();
        MarkdownRenderer.ResolveRelativeUri("features.md", "").Should().BeNull();
    }

    [Fact(DisplayName = "MDR-020: RenderToHtml with context should transform relative links")]
    public void MDR020()
    {
        // Arrange
        var markdown = "[Features](features.md)";
        var currentUri = "demo://resource/architecture.md";
        var basePath = "/mcp-servers/everything";

        // Act
        var result = _sut.RenderToHtml(markdown, currentUri, basePath);

        // Assert
        result.Should().Contain("href=\"/mcp-servers/everything?resource=demo%3A%2F%2Fresource%2Ffeatures.md\"");
        result.Should().Contain("data-resource-link=\"true\"");
        result.Should().Contain(">Features</a>");
    }

    [Fact(DisplayName = "MDR-021: RenderToHtml with context should not transform external links")]
    public void MDR021()
    {
        // Arrange
        var markdown = "[Google](https://google.com)";
        var currentUri = "demo://resource/architecture.md";
        var basePath = "/mcp-servers/everything";

        // Act
        var result = _sut.RenderToHtml(markdown, currentUri, basePath);

        // Assert
        result.Should().Contain("href=\"https://google.com\"");
        result.Should().NotContain("data-resource-link");
    }

    [Fact(DisplayName = "MDR-022: RenderToHtml with context should not transform anchor links")]
    public void MDR022()
    {
        // Arrange
        var markdown = "[Section](#section)";
        var currentUri = "demo://resource/architecture.md";
        var basePath = "/mcp-servers/everything";

        // Act
        var result = _sut.RenderToHtml(markdown, currentUri, basePath);

        // Assert
        result.Should().Contain("href=\"#section\"");
        result.Should().NotContain("data-resource-link");
    }

    [Fact(DisplayName = "MDR-023: RenderToHtml with context should not transform mailto links")]
    public void MDR023()
    {
        // Arrange
        var markdown = "[Email](mailto:test@example.com)";
        var currentUri = "demo://resource/architecture.md";
        var basePath = "/mcp-servers/everything";

        // Act
        var result = _sut.RenderToHtml(markdown, currentUri, basePath);

        // Assert
        result.Should().Contain("href=\"mailto:test@example.com\"");
        result.Should().NotContain("data-resource-link");
    }

    [Fact(DisplayName = "MDR-024: RenderToHtml with context should transform parent path links")]
    public void MDR024()
    {
        // Arrange
        var markdown = "[Intro](../intro.md)";
        var currentUri = "demo://resource/docs/architecture.md";
        var basePath = "/mcp-servers/everything";

        // Act
        var result = _sut.RenderToHtml(markdown, currentUri, basePath);

        // Assert
        result.Should().Contain("href=\"/mcp-servers/everything?resource=demo%3A%2F%2Fresource%2Fintro.md\"");
        result.Should().Contain("data-resource-link=\"true\"");
    }

    [Fact(DisplayName = "MDR-025: RenderToHtml with context should handle images normally")]
    public void MDR025()
    {
        // Arrange
        var markdown = "![Logo](logo.png)";
        var currentUri = "demo://resource/architecture.md";
        var basePath = "/mcp-servers/everything";

        // Act
        var result = _sut.RenderToHtml(markdown, currentUri, basePath);

        // Assert
        result.Should().Contain("<img");
        result.Should().Contain("src=\"logo.png\"");
        result.Should().NotContain("data-resource-link");
    }

    [Fact(DisplayName = "MDR-026: RenderToHtml without context should not transform links")]
    public void MDR026()
    {
        // Arrange
        var markdown = "[Features](features.md)";

        // Act
        var result = _sut.RenderToHtml(markdown);

        // Assert
        result.Should().Contain("href=\"features.md\"");
        result.Should().NotContain("data-resource-link");
    }

    [Fact(DisplayName = "MDR-027: RenderToHtml with context should return empty for null input")]
    public void MDR027()
    {
        // Act
        var result = _sut.RenderToHtml(null!, "demo://resource/test.md", "/mcp-servers/test");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "MDR-028: RenderToHtml with context should transform same-scheme absolute URIs")]
    public void MDR028()
    {
        // Arrange
        var markdown = "[Other](demo://resource/other.md)";
        var currentUri = "demo://resource/architecture.md";
        var basePath = "/mcp-servers/everything";

        // Act
        var result = _sut.RenderToHtml(markdown, currentUri, basePath);

        // Assert
        result.Should().Contain("href=\"/mcp-servers/everything?resource=demo%3A%2F%2Fresource%2Fother.md\"");
        result.Should().Contain("data-resource-link=\"true\"");
    }
}
