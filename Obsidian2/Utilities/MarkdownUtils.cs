using System.Text.RegularExpressions;

namespace Obsidian2.Utilities;

/// <summary>
/// Provides utilities for processing and converting Markdown content.
/// </summary>
public static class MarkdownUtils
{
    /// <summary>
    /// Converts HTML img tags to Markdown image syntax.
    /// </summary>
    /// <param name="content">HTML content containing img tags</param>
    /// <returns>Content with img tags converted to Markdown format</returns>
    /// <example>
    /// <code>
    /// // Input
    /// var html = "&lt;p&gt;Text &lt;img src=\"image.jpg\" alt=\"My Image\"&gt; more text&lt;/p&gt;";
    ///
    /// // Output
    /// var result = MarkdownUtils.ConvertHtmlImgToMarkdown(html);
    /// // "&lt;p&gt;Text ![My Image](image.jpg) more text&lt;/p&gt;"
    /// </code>
    /// </example>
    /// <example>
    /// <code>
    /// // Input with spaces in path
    /// var html = "&lt;img src=\"path with spaces/image.jpg\" alt=\"Test\"&gt;";
    ///
    /// // Output
    /// var result = MarkdownUtils.ConvertHtmlImgToMarkdown(html);
    /// // "![Test](path%20with%20spaces/image.jpg)"
    /// </code>
    /// </example>
    public static string ConvertHtmlImgToMarkdown(string content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;

        content = RegexUtils.ReplacePattern(content, RegexUtils.htmlImageWithAttributes, match =>
        {
            string src = match.Groups[1].Success ? match.Groups[1].Value : "";
            string alt = match.Groups[2].Success ? match.Groups[2].Value : "";

            if (string.IsNullOrEmpty(src))
                return match.Value;

            src = EncodeSpacesInUrl(src);
            return $"![{alt}]({src})";
        });

        return FixMarkdownImageLinks(content);
    }

    /// <summary>
    /// Fixes Markdown image links by encoding spaces in URLs.
    /// </summary>
    /// <param name="content">Markdown content containing image links</param>
    /// <returns>Content with properly encoded image URLs</returns>
    /// <example>
    /// <code>
    /// // Input
    /// var markdown = "![Alt text](path with spaces/image.jpg)";
    ///
    /// // Output
    /// var result = MarkdownUtils.FixMarkdownImageLinks(markdown);
    /// // "![Alt text](path%20with%20spaces/image.jpg)"
    /// </code>
    /// </example>
    public static string FixMarkdownImageLinks(string content)
    {
        return RegexUtils.ReplacePattern(content, RegexUtils.markdownImage, match =>
        {
            string alt = match.Groups[1].Value;
            string url = match.Groups[2].Value;
            string encodedUrl = EncodeSpacesInUrl(url);
            return $"![{alt}]({encodedUrl})";
        });
    }

    /// <summary>
    /// Formats Markdown tables by adding blank lines after table rows.
    /// </summary>
    /// <param name="content">Markdown content containing tables</param>
    /// <returns>Content with properly formatted tables</returns>
    /// <example>
    /// <code>
    /// // Input
    /// var markdown = @"| Header 1 | Header 2 |
    /// |----------|----------|
    /// | Cell 1   | Cell 2   |
    /// Next paragraph";
    ///
    /// // Output
    /// var result = MarkdownUtils.FormatMarkdownTables(markdown);
    /// // "| Header 1 | Header 2 |\n|----------|----------|\n| Cell 1   | Cell 2   |\n\nNext paragraph"
    /// </code>
    /// </example>
    public static string FormatMarkdownTables(string content)
    {
        return RegexUtils.ReplacePattern(content, RegexUtils.markdownTableRow, "|\n\n");
    }

    /// <summary>
    /// Encodes spaces in URL strings by replacing them with %20.
    /// </summary>
    /// <param name="url">URL string that may contain spaces</param>
    /// <returns>URL with spaces encoded as %20</returns>
    /// <example>
    /// <code>
    /// // Input
    /// var url = "path with spaces/file.jpg";
    ///
    /// // Output
    /// var encoded = EncodeSpacesInUrl(url);
    /// // "path%20with%20spaces/file.jpg"
    /// </code>
    /// </example>
    private static string EncodeSpacesInUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        return url.Replace(" ", "%20");
    }
}
