using System.Text;
using System.Text.RegularExpressions;

namespace Obsidian2Hexo;

internal static class AdmonitionsFormatter
{
    public static string FormatCodeBlockStyle2ButterflyStyle(string content)
    {
        var codeBlockAdMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"ad-note", "info"},
            {"ad-tip", "primary"},
            {"ad-warning", "warning"},
            {"ad-quote", "'fas fa-quote-left'"},
            {"ad-fail", "danger"}
        };

        string pattern = "```(ad-(?:note|tip|warning|quote|fail))\\s*([\\s\\S]*?)```";

        string ReplaceAdmonition(Match match)
        {
            string calloutType = codeBlockAdMap.GetValueOrDefault(match.Groups[1].Value, "info");
            string callout = match.Groups[2].Value.Trim();
            return HexoPostStyleAdapter.AdaptAdmonition(callout, calloutType);
        }

        return Regex.Replace(content, pattern, ReplaceAdmonition);
    }

    public static string FormatMkDocsStyle2ButterflyStyle(string content)
    {
        var mkDocsAdMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"note", "info"},
            {"tip", "primary"},
            {"warning", "warning"},
            {"caution", "warning"},
            {"fail", "danger"},
            {"quote", "'fas fa-quote-left'"},
            {"cite", "'fas fa-quote-left'"},
            {"example", "'fas fa-list'"},
            {"tldr", "'fas fa-clipboard-list'"},
        };

        List<string> lines = content.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None).ToList();
        var result = new StringBuilder();
        var admonitionStack = new Stack<(string, List<string>)>(); // (admonitionType, capturedLines)

        lines.ForEach(line =>
        {
            if (IsAdmonitionBeginning(line, out string type))
            {
                // If the line is the beginning of an admonition block
                // Push a new block to the stack
                string calloutType = mkDocsAdMap.GetValueOrDefault(type, "info");
                admonitionStack.Push((calloutType, new List<string>()));
            }
            else if (IsAdmonitionLine(line, out int level) && admonitionStack.Count != 0)
            {
                // if the level changed, which means the current block is finished
                // Thus, we should pop the block and format it
                if (level != admonitionStack.Count && admonitionStack.Count > 1)
                {
                    (string admonitionType, List<string> capturedLines) = admonitionStack.Pop();
                    string admonition = FormatCapturedLinesToAdmonition(admonitionType, capturedLines);
                    admonitionStack.Peek().Item2.Add(admonition);
                }

                // If the line is in an admonition block, add it to the current block
                string clearedLine = ClearAdmonitionMark(line);
                if (!string.IsNullOrEmpty(clearedLine)) admonitionStack.Peek().Item2.Add(clearedLine);
            }
            else
            {
                // If the line is not in an admonition block, which means all the blocks are finished
                // Thus, we should format all the blocks in the stack
                if (admonitionStack.Count != 0)
                    FormatAdmonitionStack();

                // This line is not in any block, just add it to the result
                result.AppendLine(line);
            }
        });

        // If there are still blocks in the stack and we have finished reading all the lines
        // We should format all the blocks in the stack
        FormatAdmonitionStack();

        return result.ToString();

        void FormatAdmonitionStack()
        {
            while (admonitionStack.Count > 0)
            {
                (string admonitionType, List<string> capturedLines) = admonitionStack.Pop();
                string admonition = FormatCapturedLinesToAdmonition(admonitionType, capturedLines);
                if (admonitionStack.Count != 0)
                    admonitionStack.Peek().Item2.Add(admonition);
                else
                    result.AppendLine(FormatCapturedLinesToAdmonition(admonitionType, capturedLines));
            }
        }

        string FormatCapturedLinesToAdmonition(string admonitionType, List<string> capturedLines)
        {
            return HexoPostStyleAdapter.AdaptAdmonition(string.Join("\n", capturedLines), admonitionType);
        }

        string ClearAdmonitionMark(string line)
        {
            string pattern = @"^(\s*>\s*)+";
            return Regex.Replace(line, pattern, "");
        }

        bool IsAdmonitionBeginning(string line, out string type)
        {
            string pattern = @"^(\s*>\s*)+\[!([a-zA-Z]+)]";
            Match match = Regex.Match(line, pattern);
            type = match.Success ? match.Groups[2].Value : null;
            return match.Success;
        }

        bool IsAdmonitionLine(string line, out int level)
        {
            string pattern = @"^(\s*>\s*)+";
            Match match = Regex.Match(line, pattern);
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
}