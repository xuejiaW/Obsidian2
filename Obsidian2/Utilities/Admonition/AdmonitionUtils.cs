using System.Text.RegularExpressions;

namespace Obsidian2.Utilities;

/// <summary>
/// Provides utilities for parsing and processing Obsidian admonition blocks.
/// </summary>
public static class AdmonitionUtils
{
    /// <summary>
    /// Removes admonition quote markers from a line, leaving only the content.
    /// </summary>
    /// <param name="line">The line containing admonition markers</param>
    /// <returns>The line with admonition quote markers removed</returns>
    /// <example>
    /// <code>
    /// // Remove quote markers from admonition content
    /// var cleaned = AdmonitionUtils.ClearAdmonitionMark("> > Some content here");
    /// // Returns: "Some content here"
    /// 
    /// var cleanedNested = AdmonitionUtils.ClearAdmonitionMark("  >  >  Nested content");
    /// // Returns: "Nested content"
    /// </code>
    /// </example>
    public static string ClearAdmonitionMark(string line)
    {
        return RegexUtils.ReplacePattern(line, RegexUtils.admonitionQuoteMarkers, string.Empty);
    }

    /// <summary>
    /// Checks if a line is the beginning of an admonition block and extracts the type.
    /// </summary>
    /// <param name="line">The line to check</param>
    /// <param name="type">When this method returns, contains the admonition type if found; otherwise, null</param>
    /// <returns>True if the line starts an admonition block; otherwise, false</returns>
    /// <example>
    /// <code>
    /// // Check for admonition beginning
    /// if (AdmonitionUtils.IsAdmonitionBeginning("> [!note]", out string type))
    /// {
    ///     // type will be "note"
    /// }
    /// 
    /// if (AdmonitionUtils.IsAdmonitionBeginning("  > [!warning]", out string warningType))
    /// {
    ///     // warningType will be "warning"
    /// }
    /// </code>
    /// </example>
    public static bool IsAdmonitionBeginning(string line, out string type)
    {
        var match = RegexUtils.GetMatch(line, @"^(\s*>\s*)+\[!([a-zA-Z]+)]");
        type = match.Success ? match.Groups[2].Value : null;
        return match.Success;
    }

    /// <summary>
    /// Determines if a line is part of an admonition block and calculates its nesting level.
    /// </summary>
    /// <param name="line">The line to check</param>
    /// <param name="level">When this method returns, contains the nesting level (number of '>' characters)</param>
    /// <returns>True if the line is part of an admonition block; otherwise, false</returns>
    /// <example>
    /// <code>
    /// // Check single level admonition
    /// if (AdmonitionUtils.IsAdmonitionLine("> Some content", out int level))
    /// {
    ///     // level will be 1
    /// }
    /// 
    /// // Check nested admonition
    /// if (AdmonitionUtils.IsAdmonitionLine("  > > Nested content", out int nestedLevel))
    /// {
    ///     // nestedLevel will be 2
    /// }
    /// </code>
    /// </example>
    public static bool IsAdmonitionLine(string line, out int level)
    {
        var match = RegexUtils.GetMatch(line, RegexUtils.admonitionQuoteMarkers);
        level = 0;
        
        for (int i = 0; i != line.Length; ++i)
        {
            char c = line[i];
            if (c != ' ' && c != '>') break;
            if (c == '>') level++;
        }

        return match.Success;
    }
}
