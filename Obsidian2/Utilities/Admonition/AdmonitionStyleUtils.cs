using System.Text;
using System.Text.RegularExpressions;
using Obsidian2.Utilities.Hexo;

namespace Obsidian2.Utilities.Admonition;

/// <summary>
/// Provides style conversion utilities for different admonition formats.
/// </summary>
public static class AdmonitionStyleUtils
{
    /// <summary>
    /// Converts code block style admonitions to Butterfly style using regex replacement.
    /// </summary>
    /// <param name="content">Content containing code block admonitions</param>
    /// <param name="mapping">Mapping dictionary for type conversion</param>
    /// <returns>Content with code block admonitions converted to Butterfly style</returns>
    /// <example>
    /// <code>
    /// var mapping = AdmonitionMappingUtils.GetCodeBlockToButterflyMapping();
    /// var result = AdmonitionStyleUtils.ConvertCodeBlockToButterflyStyle(content, mapping);
    /// 
    /// // Input:  ```ad-note
    /// //         This is a note content
    /// //         with multiple lines
    /// //         ```
    /// // Output: {% note info %}
    /// //         This is a note content
    /// //         with multiple lines
    /// //         {% endnote %}
    /// </code>
    /// </example>
    public static string ConvertCodeBlockToButterflyStyle(string content, Dictionary<string, string> mapping)
    {
        string pattern = "```(ad-(?:note|tip|warning|quote|fail))\\s*([\\s\\S]*?)```";

        string ReplaceAdmonition(Match match)
        {
            string sourceType = match.Groups[1].Value;
            string calloutType = mapping.GetValueOrDefault(sourceType, "info");
            string callout = match.Groups[2].Value.Trim();
            return HexoUtils.AdaptAdmonition(callout, calloutType);
        }

        return Regex.Replace(content, pattern, ReplaceAdmonition);
    }

    /// <summary>
    /// Converts MkDocs style admonitions to Butterfly style using line-by-line processing.
    /// </summary>
    /// <param name="content">Content containing MkDocs admonitions</param>
    /// <param name="mapping">Mapping dictionary for type conversion</param>
    /// <returns>Content with MkDocs admonitions converted to Butterfly style</returns>
    /// <example>
    /// <code>
    /// var mapping = AdmonitionMappingUtils.GetMkDocsToButterflyMapping();
    /// var result = AdmonitionStyleUtils.ConvertMkDocsToButterflyStyle(content, mapping);
    /// 
    /// // Input:  > [!note]
    /// //         > This is a note
    /// //         > with multiple lines
    /// // Output: {% note info %}
    /// //         This is a note
    /// //         with multiple lines
    /// //         {% endnote %}
    /// </code>
    /// </example>
    public static string ConvertMkDocsToButterflyStyle(string content, Dictionary<string, string> mapping)
    {
        List<string> lines = content.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None).ToList();
        var result = new StringBuilder();
        var admonitionStack = new Stack<(string, List<string>)>();

        lines.ForEach(line =>
        {
            if (AdmonitionUtils.IsAdmonitionBeginning(line, out string type))
            {
                string calloutType = mapping.GetValueOrDefault(type, "info");
                admonitionStack.Push((calloutType, new List<string>()));
            }
            else if (AdmonitionUtils.IsAdmonitionLine(line, out int level) && admonitionStack.Count != 0)
            {
                if (level != admonitionStack.Count && admonitionStack.Count > 1)
                {
                    (string admonitionType, List<string> capturedLines) = admonitionStack.Pop();
                    string admonition = FormatLinesToAdmonition(admonitionType, capturedLines);
                    admonitionStack.Peek().Item2.Add(admonition);
                }

                string clearedLine = AdmonitionUtils.ClearAdmonitionMark(line);
                if (!string.IsNullOrEmpty(clearedLine)) admonitionStack.Peek().Item2.Add(clearedLine);
            }
            else
            {
                if (admonitionStack.Count != 0)
                    ProcessAdmonitionStack(admonitionStack, result);

                result.AppendLine(line);
            }
        });

        if (admonitionStack.Count != 0)
            ProcessAdmonitionStack(admonitionStack, result);

        return result.ToString();
    }

    /// <summary>
    /// Converts MkDocs callouts to simple blockquotes.
    /// </summary>
    /// <param name="content">Content containing MkDocs callouts</param>
    /// <returns>Content with callouts converted to blockquotes</returns>
    /// <example>
    /// <code>
    /// var result = AdmonitionStyleUtils.ConvertMkDocsCalloutToQuote(content);
    /// 
    /// // Input:  > [!note] Important Note
    /// //         > This is the content
    /// //         > of the callout
    /// // Output: > This is the content
    /// //         > of the callout
    /// </code>
    /// </example>
    public static string ConvertMkDocsCalloutToQuote(string content)
    {
        string calloutPattern = @">\s*\[!([a-zA-Z]+)\].*?\n((?:>\s*.*(?:\n|$))+)";

        string processedContent = Regex.Replace(content, calloutPattern, match =>
        {
            string calloutBody = match.Groups[2].Value;

            var contentLines = calloutBody.Split('\n')
                                          .Select(line => Regex.Replace(line.Trim(), @"^>\s*", "").Trim())
                                          .Where(line => !string.IsNullOrWhiteSpace(line))
                                          .ToList();

            if (!contentLines.Any())
                return "";

            var newBlockquote = string.Join("\n", contentLines.Select(l => $"> {l}"));
            return newBlockquote + "\n\n";
        });

        processedContent = Regex.Replace(processedContent, @"\n{3,}", "\n\n");
        processedContent = Regex.Replace(processedContent, @"(\n>.*?\n)\n+(?=>)", "$1\n");
        return processedContent.TrimEnd() + "\n";
    }

    private static void ProcessAdmonitionStack(Stack<(string, List<string>)> admonitionStack, StringBuilder result)
    {
        while (admonitionStack.Count > 0)
        {
            (string admonitionType, List<string> capturedLines) = admonitionStack.Pop();
            string admonition = FormatLinesToAdmonition(admonitionType, capturedLines);
            if (admonitionStack.Count != 0)
                admonitionStack.Peek().Item2.Add(admonition);
            else
                result.AppendLine(FormatLinesToAdmonition(admonitionType, capturedLines));
        }
    }

    private static string FormatLinesToAdmonition(string admonitionType, List<string> capturedLines)
    {
        return HexoUtils.AdaptAdmonition(string.Join("\n", capturedLines), admonitionType);
    }
}
