using System.Text.RegularExpressions;

namespace Obsidian2Hexo;

using Adapter = HexoPostStyleAdapter;

internal class HexoPostFormatter
{
    private string m_SrcNotePath = null;
    private string m_DstPostPath = null;

    public HexoPostFormatter(string srcNotePath, string dstPostPath)
    {
        m_SrcNotePath = srcNotePath;
        m_DstPostPath = dstPostPath;
    }

    public void Format()
    {
        string content = File.ReadAllText(m_SrcNotePath);
        string ret = FormatBlockLink(content);
        ret = CleanBlockLinkMark(ret);
        ret = FormatMdLinkToHexoStyle(ret);
        ret = FormatCodeBlockAdmonitionsToButterflyStyle(ret);
        ret = FormatMkDocsAdmonitionsToButterflyStyle(ret);
        File.WriteAllText(m_DstPostPath, ret);
    }

    private string FormatBlockLink(string content)
    {
        // Block Link's path is related to the vault path, not the note path.

        string pattern = @"!\[(.*?)\]\((.*?.md)#(\^[a-zA-Z0-9]{6})\)";
        return Regex.Replace(content, pattern, ReplaceBlockLink);

        string ReplaceBlockLink(Match match)
        {
            string _ = match.Groups[1].Value;
            string linkRelativePath = match.Groups[2].Value;
            string blockId = match.Groups[3].Value;
            string targetNotePath
                = ObsidianNoteParser.GetNotePathBasedOnFolder(Obsidian2HexoHandler.obsidianTempDir.FullName,
                                                              linkRelativePath);
            if (File.Exists(targetNotePath))
            {
                string blockContent = File.ReadAllLines(targetNotePath).ToList()
                                          .First(line => line.EndsWith(blockId)).Replace(blockId, "");
                return $"""
                        > {blockContent}
                        > ———— {GetReferenceLink()}
                        """;
            }

            Console.WriteLine($"Not Found for path {linkRelativePath}");
            return "";

            string GetReferenceLink()
            {
                string title = ObsidianNoteParser.GetTitle(targetNotePath);
                if (!ObsidianNoteParser.IsRequiredToBePublished(targetNotePath)) return title;

                string postPath = Adapter.AdaptPostPath(Adapter.ConvertMdLink2Relative(targetNotePath));
                return $"[{title}]({postPath})";
            }
        }
    }

    private string CleanBlockLinkMark(string content)
    {
        string pattern = @"(\^[a-zA-Z0-9]{6})";
        return Regex.Replace(content, pattern, "");
    }

    private string FormatMdLinkToHexoStyle(string content)
    {
        string linkPattern = @"\[(.*?)\]\((.*?)\)";
        string ret = Regex.Replace(content, linkPattern, ReplaceLink);
        return ret;

        string ReplaceLink(Match match)
        {
            string linkText = match.Groups[1].Value;
            string linkRelativePath = match.Groups[2].Value;

            return HexoPostStyleAdapter.AdaptLink(linkText, linkRelativePath, m_SrcNotePath, m_DstPostPath);
        }
    }

    private string FormatCodeBlockAdmonitionsToButterflyStyle(string content)
    {
        var codeBlockAdMap = new Dictionary<string, string>
        {
            {"ad-note", "info"},
            {"ad-tip", "info"},
            {"ad-warning", "warning"},
            {"ad-quote", "info"},
            {"ad-fail", "danger"}
        };

        string pattern = "```(ad-(?:note|tip|warning|quote|fail))\\s*([\\s\\S]*?)```";

        string ReplaceAdmonition(Match match)
        {
            string calloutType = codeBlockAdMap.TryGetValue(match.Groups[1].Value, out string type) ? type : "info";
            string callout = match.Groups[2].Value.Trim();
            return HexoPostStyleAdapter.AdaptAdmonition(callout, calloutType);
        }

        return Regex.Replace(content, pattern, ReplaceAdmonition);
    }

    private string FormatMkDocsAdmonitionsToButterflyStyle(string content)
    {
        var mkDocsAdMap = new Dictionary<string, string>
        {
            {"note", "info"},
            {"tip", "primary"},
            {"warning", "warning"},
            {"fail", "danger"},
            {"quote", "'fas fa-quote-left'"},
            {"cite", "'fas fa-quote-left'"},
        };

        string onlySymbolLinePattern = @"^>\s*$";
        content = Regex.Replace(content, onlySymbolLinePattern, "", RegexOptions.Multiline);

        string supportedAdmonitions = string.Join("|", mkDocsAdMap.Keys);
        string pattern = $@"^> \[!({supportedAdmonitions})][\r?\n]+> ((.*?)(?:\r?\n(?=>)|$))+";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);

        return regex.Replace(content, ReplaceAdmonition);

        string ReplaceAdmonition(Match match)
        {
            string calloutType = mkDocsAdMap.TryGetValue(match.Groups[1].Value, out string type) ? type : "info";
            string callout = string.Join("\n", match.Groups[2].Captures.Select(x => x.Value.Replace("> ", "")));
            return HexoPostStyleAdapter.AdaptAdmonition(callout, calloutType);
        }
    }
}