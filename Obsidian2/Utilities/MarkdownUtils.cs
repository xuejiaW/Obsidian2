using System.Text.RegularExpressions;

namespace Obsidian2.Utilities;

public static class MarkdownUtils
{
    public static string ConvertHtmlImgToMarkdown(string content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;

        string htmlPattern = @"<img\s+(?:src=""(.*?)"")?(?:\s+alt=""(.*?)"")?[^>]*>";
        content = Regex.Replace(content, htmlPattern, match =>
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

    public static string FixMarkdownImageLinks(string content)
    {
        string mdPattern = @"!\[(.*?)\]\((.*?)\)";
        return Regex.Replace(content, mdPattern, match =>
        {
            string alt = match.Groups[1].Value;
            string url = match.Groups[2].Value;
            string encodedUrl = EncodeSpacesInUrl(url);
            return $"![{alt}]({encodedUrl})";
        });
    }

    public static string FormatMarkdownTables(string content)
    {
        return Regex.Replace(content, @"\|\n", "|\n\n");
    }

    private static string EncodeSpacesInUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        return url.Replace(" ", "%20");
    }
}
