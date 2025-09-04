using System.Text;
using TinyPinyin;
using Obsidian2.Utilities;
using Obsidian2.Utilities.Obsidian;

namespace Obsidian2.Utilities.Hexo;

/// <summary>
/// Provides utilities for converting Obsidian content to Hexo blog format, including path adaptation, link conversion, and content formatting.
/// </summary>
public static class HexoUtils
{
    #region Path Conversion Methods
    /// <summary>
    /// Converts a file path to Hexo-compatible post path format by converting Chinese to Pinyin and normalizing characters.
    /// </summary>
    /// <param name="path">Original file path or name</param>
    /// <returns>Hexo-compatible path with Chinese converted to Pinyin, spaces to underscores, and lowercased</returns>
    /// <example>
    /// <code>
    /// // Chinese characters to Pinyin conversion
    /// var result1 = HexoUtils.ConvertPathForHexoPost("我的文章.md");
    /// // Returns: "wo_de_wen_zhang.md"
    /// 
    /// // Mixed content conversion
    /// var result2 = HexoUtils.ConvertPathForHexoPost("My 中文 Article.md");
    /// // Returns: "my_zhong_wen_article.md"
    /// 
    /// // Special characters handling
    /// var result3 = HexoUtils.ConvertPathForHexoPost("Test Article (Draft).md");
    /// // Returns: "test_article_(draft).md"
    /// </code>
    /// </example>
    public static string ConvertPathForHexoPost(string path)
    {
        string result = TextUtils.ConvertChineseToPinyin(path);
        result = TextUtils.ConvertSpaceToUnderscore(result);
        return result.ToLower();
    }

    /// <summary>
    /// Converts a file path to Hexo-compatible asset path format by normalizing spaces and casing.
    /// </summary>
    /// <param name="path">Original asset file path</param>
    /// <returns>Hexo-compatible asset path with spaces converted to underscores and lowercased</returns>
    /// <example>
    /// <code>
    /// // Basic asset path conversion
    /// var result1 = HexoUtils.ConvertPathForHexoAsset("My Image.jpg");
    /// // Returns: "my_image.jpg"
    /// 
    /// // Path with URL encoding
    /// var result2 = HexoUtils.ConvertPathForHexoAsset("File%20Name.png");
    /// // Returns: "file_name.png"
    /// 
    /// // Complex path conversion
    /// var result3 = HexoUtils.ConvertPathForHexoAsset("assets/images/Screen Shot.png");
    /// // Returns: "assets/images/screen_shot.png"
    /// </code>
    /// </example>
    public static string ConvertPathForHexoAsset(string path)
    {
        string result = TextUtils.ConvertSpaceToUnderscore(path);
        return result.ToLower();
    }

    /// <summary>
    /// Converts a markdown file path to a relative Hexo post link format.
    /// </summary>
    /// <param name="filePath">Absolute path to the markdown file</param>
    /// <returns>Relative link path starting with '/' and using only the filename without extension</returns>
    /// <example>
    /// <code>
    /// // Convert absolute path to relative link
    /// var result1 = HexoUtils.ConvertMdLinkToRelative(@"C:\Blog\Posts\my-article.md");
    /// // Returns: "/my-article"
    /// 
    /// // Handle complex filenames
    /// var result2 = HexoUtils.ConvertMdLinkToRelative(@"D:\Notes\文章标题.md");
    /// // Returns: "/文章标题"
    /// 
    /// // Nested path handling
    /// var result3 = HexoUtils.ConvertMdLinkToRelative(@"C:\Projects\blog\posts\tech\guide.md");
    /// // Returns: "/guide"
    /// </code>
    /// </example>
    public static string ConvertMdLinkToRelative(string filePath)
    {
        return "/" + Path.GetFileNameWithoutExtension(filePath);
    }
    #endregion

    #region Link and Content Conversion Methods
    /// <summary>
    /// Converts an Obsidian-style link to Hexo-compatible format, handling markdown files, assets, and fragments.
    /// </summary>
    /// <param name="linkText">Display text of the link</param>
    /// <param name="linkRelativePath">Relative path from the original link</param>
    /// <param name="notePath">Absolute path to the source note containing the link</param>
    /// <param name="postPath">Absolute path to the target post file</param>
    /// <returns>Hexo-compatible markdown link in format [text](path#fragment)</returns>
    /// <example>
    /// <code>
    /// // Convert markdown file link
    /// var result1 = HexoUtils.ConvertObsidianLinkToHexo(
    ///     "Reference Article", 
    ///     "../posts/my-guide.md#introduction",
    ///     @"C:\Vault\current.md",
    ///     @"C:\Blog\Posts\current-post.md");
    /// // Returns: "[Reference Article](/my-guide/#introduction)"
    /// 
    /// // Convert asset link with size specification
    /// var result2 = HexoUtils.ConvertObsidianLinkToHexo(
    ///     "Screenshot|500", 
    ///     "assets/images/screenshot.png",
    ///     @"C:\Vault\article.md",
    ///     @"C:\Blog\Posts\article.md");
    /// // Returns: "[Screenshot](/images/screenshot.png)"
    /// 
    /// // Convert header fragment link
    /// var result3 = HexoUtils.ConvertObsidianLinkToHexo(
    ///     "See above", 
    ///     "#conclusion",
    ///     @"C:\Vault\notes.md",
    ///     @"C:\Blog\Posts\notes.md");
    /// // Returns: "[See above](/notes/#conclusion)"
    /// </code>
    /// </example>
    public static string ConvertObsidianLinkToHexo(string linkText, string linkRelativePath, string notePath, string postPath)
    {
        string fragment = "";

        if (linkRelativePath.Contains(".md"))
        {
            // If link contains title fragment, handle it firstly.
            if (linkRelativePath.Contains('#'))
            {
                string[] splitLink = linkRelativePath.Split("#", 2);
                linkRelativePath = splitLink[0];
                fragment = "/#" + ConvertTitleToUrlFragment(splitLink[1]);
            }

            string absLinkPath = ObsidianNoteUtils.GetAbsoluteLinkPath(notePath, linkRelativePath);
            if (!ObsidianNoteUtils.IsRequiredToBePublished(absLinkPath)) return linkText;

            // As all posts are in the same directory, we can just use the file name as the link path.
            linkRelativePath = ConvertMdLinkToRelative(absLinkPath);
        }

        if (linkRelativePath.StartsWith("#"))
        {
            fragment = "/#" + ConvertTitleToUrlFragment(linkRelativePath[1..]);
            linkRelativePath = ConvertMdLinkToRelative(postPath);
        }

        // For Obsidian Notes, if the note is ABC.md, the assets dir would be assets/abc
        // For Hexo Posts, if the post is abc.md, the assets dir would be abc
        // Thus, we only needs path after "assets/".
        if (linkRelativePath.Contains("assets/"))
        {
            int indexOfAssets = linkRelativePath.IndexOf("assets/", StringComparison.Ordinal);
            linkRelativePath = '/' + linkRelativePath[(indexOfAssets + "assets/".Length)..];

            // if link text is end with sth like |500, delete the |500 part
            // Obsidian use this to specify the size of the image, but Hexo doesn't need it.
            linkText = RegexUtils.ReplacePattern(linkText, RegexUtils.trailingPipeNumbers, string.Empty);
        }

        linkRelativePath = ConvertPathForHexoPost(linkRelativePath);
        return $"[{linkText}]({linkRelativePath}{fragment})";
    }

    /// <summary>
    /// Converts content to Hexo's admonition (note) format using Butterfly theme syntax.
    /// </summary>
    /// <param name="calloutContent">Content to be wrapped in the admonition block</param>
    /// <param name="type">Type of admonition (info, warning, danger, success, etc.)</param>
    /// <returns>Hexo-compatible admonition block in Butterfly theme format</returns>
    /// <example>
    /// <code>
    /// // Create info admonition
    /// var result1 = HexoUtils.ConvertToHexoAdmonition("This is important information.", "info");
    /// // Returns: "{% note info %}\nThis is important information.\n{% endnote %}"
    /// 
    /// // Create warning admonition
    /// var result2 = HexoUtils.ConvertToHexoAdmonition("Be careful with this operation!", "warning");
    /// // Returns: "{% note warning %}\nBe careful with this operation!\n{% endnote %}"
    /// 
    /// // Create multi-line admonition
    /// var result3 = HexoUtils.ConvertToHexoAdmonition("Line 1\nLine 2\nLine 3", "success");
    /// // Returns: "{% note success %}\nLine 1\nLine 2\nLine 3\n{% endnote %}"
    /// </code>
    /// </example>
    public static string ConvertToHexoAdmonition(string calloutContent, string type)
    {
        return $"{{% note {type} %}}\n{calloutContent}\n{{% endnote %}}";
    }
    #endregion

    #region Private Helper Methods
    /// <summary>
    /// Converts title text to URL-safe fragment identifier by replacing spaces and dots with underscores.
    /// </summary>
    /// <param name="titleText">Original title text from markdown header</param>
    /// <returns>URL-safe fragment identifier</returns>
    private static string ConvertTitleToUrlFragment(string titleText)
    {
        return TextUtils.ConvertDotToUnderscore(TextUtils.ConvertSpaceToUnderscore(titleText));
    }
    #endregion
}
