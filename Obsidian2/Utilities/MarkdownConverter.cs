using System.Text.RegularExpressions;

namespace Obsidian2.Utilities;

/// <summary>
/// 提供 HTML 到 Markdown 的转换工具，以及 Markdown 格式化功能
/// </summary>
public static class MarkdownConverter
{
    /// <summary>
    /// 将 HTML 图片标签转换为 Markdown 格式，并修复图片链接中的空格问题
    /// </summary>
    /// <param name="content">要处理的内容</param>
    /// <returns>转换后的内容</returns>
    public static string ConvertHtmlImgToMarkdown(string content)
    {
        // 首先处理 HTML 图片标签
        string htmlPattern = @"<img\s+(?:src=""(.*?)"")?(?:\s+alt=""(.*?)"")?[^>]*>";
        content = Regex.Replace(content, htmlPattern, match =>
        {
            string src = match.Groups[1].Success ? match.Groups[1].Value : "";
            string alt = match.Groups[2].Success ? match.Groups[2].Value : "";

            if (string.IsNullOrEmpty(src))
                return match.Value;

            src = PathUtils.EncodeSpacesInUrl(src);
            return $"![{alt}]({src})";
        });

        // 然后处理 Markdown 图片链接中的空格问题
        return FixMarkdownImageLinks(content);
    }

    /// <summary>
    /// 修复 Markdown 图片链接中的空格问题
    /// </summary>
    /// <param name="content">要处理的内容</param>
    /// <returns>处理后的内容</returns>
    public static string FixMarkdownImageLinks(string content)
    {
        string mdPattern = @"!\[(.*?)\]\((.*?)\)";
        return Regex.Replace(content, mdPattern, match =>
        {
            string alt = match.Groups[1].Value;
            string url = match.Groups[2].Value;
            string encodedUrl = PathUtils.EncodeSpacesInUrl(url);
            return $"![{alt}]({encodedUrl})";
        });
    }

    /// <summary>
    /// 处理 Markdown 表格格式，为表格行添加额外的换行符
    /// </summary>
    /// <param name="content">要处理的内容</param>
    /// <returns>处理后的内容</returns>
    public static string FormatMarkdownTables(string content)
    {
        return Regex.Replace(content, @"\|\n", "|\n\n");
    }
}
