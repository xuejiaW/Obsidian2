using System.Text.RegularExpressions;

namespace Obsidian2.Utilities;

/// <summary>
/// Provides centralized regular expression patterns and utilities used throughout the Obsidian2 application.
/// </summary>
public static class RegexUtils
{
    #region Obsidian Link Patterns
    /// <summary>
    /// Matches Obsidian header embed links like: ![text](file.md#header-name)
    /// Groups: [1]=display_text, [2]=file_path, [3]=header_name
    /// </summary>
    public const string obsidianHeaderLink = @"!\[(.*?)\]\((.*?.md)#(?!\^)(.*?)\)";

    /// <summary>
    /// Matches Obsidian block embed links like: ![text](file.md#^abc123)
    /// Groups: [1]=display_text, [2]=file_path, [3]=block_id_with_caret
    /// </summary>
    public const string obsidianBlockLink = @"!\[(.*?)\]\((.*?.md)#(\^[a-zA-Z0-9]{6})\)";

    /// <summary>
    /// Matches Obsidian block ID markers like: ^abc123 (at end of lines)
    /// Groups: [1]=complete_block_id_with_caret
    /// </summary>
    public const string obsidianBlockId = @"(\^[a-zA-Z0-9]{6})";

    /// <summary>
    /// Matches Markdown links excluding HTTP URLs like: [link text](relative/path)
    /// Groups: [1]=link_text, [2]=relative_path
    /// </summary>
    public const string markdownLink = @"\[(.*?)\]\((?!http)(.*?)\)";
    #endregion

    #region HTML Patterns
    /// <summary>
    /// Matches HTML img tags with src attribute like: &lt;img class="..." src="path/image.jpg" alt="..."&gt;
    /// Groups: [1]=attributes_before_src, [2]=src_path, [3]=attributes_after_src
    /// </summary>
    public const string htmlImage = @"<img\s+([^>]*?)src=""([^""]*?)""([^>]*?)>";

    /// <summary>
    /// Matches HTML img tags with optional src and alt like: &lt;img src="path" alt="text"&gt; or &lt;img alt="text"&gt;
    /// Groups: [1]=src_path_if_present, [2]=alt_text_if_present
    /// </summary>
    public const string htmlImageWithAttributes = @"<img\s+(?:src=""(.*?)"")?(?:\s+alt=""(.*?)"")?[^>]*>";
    #endregion

    #region Markdown Patterns
    /// <summary>
    /// Matches Markdown image syntax like: ![alt text](path/to/image.jpg)
    /// Groups: [1]=alt_text, [2]=image_path
    /// </summary>
    public const string markdownImage = @"!\[(.*?)\]\((.*?)\)";

    /// <summary>
    /// Matches Markdown table row endings like: | cell1 | cell2 |\n
    /// Used to add proper spacing after table rows
    /// </summary>
    public const string markdownTableRow = @"\|\n";
    #endregion

    #region Admonition Patterns
    /// <summary>
    /// Matches quote markers at line start like: "  >  > " or "> "
    /// Groups: [1]=complete_quote_marker_sequence
    /// Used to remove admonition indentation
    /// </summary>
    public const string admonitionQuoteMarkers = @"^(\s*>\s*)+";

    #endregion

    #region LaTeX Patterns
    /// <summary>
    /// Matches inline LaTeX expressions like: $E=mc^2$ or $\alpha + \beta$
    /// Groups: [1]=latex_expression_content (without dollar signs)
    /// </summary>
    public const string inlineLaTeX = @"(?<!\$)\$(?!\$)(.+?)(?<!\$)\$(?!\$)";

    /// <summary>
    /// Matches block LaTeX expressions like: $$\int_0^1 x dx$$ (can span multiple lines)
    /// Groups: [1]=latex_expression_content (without double dollar signs)
    /// </summary>
    public const string blockLaTeX = @"\$\$([\s\S]*?)\$\$";
    #endregion

    #region Utility Patterns
    /// <summary>
    /// Matches trailing pipe with numbers like: "image.jpg|500" or "link text|300"
    /// Groups: matches "|123" part (Obsidian image size specification)
    /// </summary>
    public const string trailingPipeNumbers = @"\|\d+$";

    /// <summary>
    /// Matches filename-unsafe characters like: spaces, hyphens, dots, brackets, parentheses
    /// Used for sanitizing filenames: "my file (1).txt" → "my_file__1___txt"
    /// </summary>
    public const string filenameSanitization = @"[\s\-\.\[\]\(\)]";
    #endregion

    #region Compiled Regex Instances
    /// <summary>
    /// Compiled regex for inline LaTeX processing (performance optimized)
    /// Matches: $expression$ and captures the expression content
    /// </summary>
    public static readonly Regex inlineLaTeXRegex = new(inlineLaTeX, RegexOptions.Compiled);

    /// <summary>
    /// Compiled regex for block LaTeX processing (performance optimized)
    /// Matches: $$expression$$ and captures the expression content
    /// </summary>
    public static readonly Regex blockLaTeXRegex = new(blockLaTeX, RegexOptions.Compiled);
    #endregion

    #region Helper Methods
    /// <summary>
    /// Replaces content using a pattern with a simple string replacement.
    /// </summary>
    /// <param name="content">Content to process</param>
    /// <param name="pattern">Regex pattern to match</param>
    /// <param name="replacement">Replacement string</param>
    /// <returns>Content with replacements applied</returns>
    /// <example>
    /// <code>
    /// // Remove block IDs from content
    /// var cleaned = RegexUtils.ReplacePattern(content, RegexUtils.obsidianBlockId, "");
    /// 
    /// // Format table rows
    /// var formatted = RegexUtils.ReplacePattern(content, RegexUtils.markdownTableRow, "|\n\n");
    /// </code>
    /// </example>
    public static string ReplacePattern(string content, string pattern, string replacement)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        return Regex.Replace(content, pattern, replacement);
    }

    /// <summary>
    /// Replaces content using a pattern with a callback function.
    /// </summary>
    /// <param name="content">Content to process</param>
    /// <param name="pattern">Regex pattern to match</param>
    /// <param name="evaluator">Function to generate replacement for each match</param>
    /// <returns>Content with replacements applied</returns>
    /// <example>
    /// <code>
    /// // Process HTML images with custom logic
    /// var result = RegexUtils.ReplacePattern(content, RegexUtils.htmlImage, match => 
    /// {
    ///     string src = match.Groups[2].Value;
    ///     return $"&lt;img src=\"/assets/{src}\"&gt;";
    /// });
    /// </code>
    /// </example>
    public static string ReplacePattern(string content, string pattern, MatchEvaluator evaluator)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        return Regex.Replace(content, pattern, evaluator);
    }

    /// <summary>
    /// Checks if content matches a specific pattern.
    /// </summary>
    /// <param name="content">Content to check</param>
    /// <param name="pattern">Regex pattern to match against</param>
    /// <returns>True if pattern matches; otherwise, false</returns>
    /// <example>
    /// <code>
    /// // Check if line is an admonition beginning
    /// if (RegexUtils.IsMatch(line, RegexUtils.admonitionBeginning))
    /// {
    ///     // Process admonition
    /// }
    /// </code>
    /// </example>
    public static bool IsMatch(string content, string pattern)
    {
        if (string.IsNullOrEmpty(content)) return false;
        return Regex.IsMatch(content, pattern);
    }

    /// <summary>
    /// Gets the first match of a pattern in content.
    /// </summary>
    /// <param name="content">Content to search</param>
    /// <param name="pattern">Regex pattern to match</param>
    /// <returns>Match object if found; otherwise, Match.Empty</returns>
    /// <example>
    /// <code>
    /// // Extract admonition type
    /// var match = RegexUtils.GetMatch(line, RegexUtils.admonitionBeginning);
    /// if (match.Success)
    /// {
    ///     string type = match.Groups[1].Value;
    /// }
    /// </code>
    /// </example>
    public static Match GetMatch(string content, string pattern)
    {
        if (string.IsNullOrEmpty(content)) return Match.Empty;
        return Regex.Match(content, pattern);
    }

    /// <summary>
    /// Gets all matches of a pattern in content.
    /// </summary>
    /// <param name="content">Content to search</param>
    /// <param name="pattern">Regex pattern to match</param>
    /// <returns>Collection of all matches found</returns>
    /// <example>
    /// <code>
    /// // Count all LaTeX blocks
    /// var matches = RegexUtils.GetMatches(content, RegexUtils.blockLaTeX);
    /// int count = matches.Count;
    /// </code>
    /// </example>
    public static MatchCollection GetMatches(string content, string pattern)
    {
        if (string.IsNullOrEmpty(content)) return Regex.Matches("", pattern);
        return Regex.Matches(content, pattern);
    }
    #endregion
}
