using System.Text.RegularExpressions;

namespace Obsidian2Hexo;

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
        string ret = FormatMdLinkToHexoStyle(content);
        ret = FormatCodeBlockAdmonitionsToButterflyStyle(ret);
        File.WriteAllText(m_DstPostPath, ret);
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

            string link = HexoPostStyleAdapter.AdaptLink(linkText, linkRelativePath, m_SrcNotePath, m_DstPostPath);

            return link;
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

        string convertedText = Regex.Replace(content, pattern, ReplaceAdmonition);
        return convertedText;
    }
}