using System.Text.RegularExpressions;

namespace Obsidian2;

using Adapter = HexoPostStyleAdapter;

internal class HexoPostFormatter
{
    private string m_SrcNotePath = null;
    private string m_DstPostPath = null;

    public string postPath => m_DstPostPath;

    public HexoPostFormatter(string srcNotePath, string dstPostPath)
    {
        m_SrcNotePath = srcNotePath;
        m_DstPostPath = dstPostPath;
    }

    public async Task Format()
    {
        string content = await File.ReadAllTextAsync(m_SrcNotePath);
        string ret = AdmonitionsFormatter.FormatCodeBlockStyle2ButterflyStyle(content);
        ret = AdmonitionsFormatter.FormatMkDocsStyle2ButterflyStyle(ret);
        ret = FormatHeaderLink(ret, m_SrcNotePath);
        ret = FormatBlockLink(ret, m_SrcNotePath);
        ret = CleanBlockLinkMark(ret);
        ret = FormatMdLinkToHexoStyle(ret);
        ret = FormatHtmlImagePaths(ret);
        await File.WriteAllTextAsync(m_DstPostPath, ret);
    }

    private string FormatHeaderLink(string content, string srcNotePath)
    {
        string pattern = @"!\[(.*?)\]\((.*?.md)#(?!\^)(.*?)\)";
        return Regex.Replace(content, pattern, ReplaceHeaderLink);

        string ReplaceHeaderLink(Match match)
        {
            string _ = match.Groups[1].Value;
            string linkRelativePath = match.Groups[2].Value;
            string header = match.Groups[3].Value;

            header = header.Replace("%20", " ");

            string targetNotePath
                = ObsidianNoteParser.GetNotePathBasedOnFolder(Obsidian2HexoHandler.obsidianTempDir.FullName,
                                                              linkRelativePath);

            if (!File.Exists(targetNotePath))
                targetNotePath = ObsidianNoteParser.GetAbsoluteLinkPath(srcNotePath, linkRelativePath);

            if (File.Exists(targetNotePath))
            {
                List<string> lines = File.ReadLines(targetNotePath).ToList();
                int startIndex = lines.FindIndex(line => line.StartsWith("#") &&
                                                         line.EndsWith(header, StringComparison.OrdinalIgnoreCase));
                int endIndex = lines.FindIndex(startIndex + 1, line => line.StartsWith("#"));
                List<string> contentLines = lines.GetRange(startIndex, endIndex - startIndex);

                string quoteContent = $"""
                                       {string.Join("\n", contentLines)}
                                       ———— {GetReferenceLink()}
                                       """;
                return Adapter.AdaptAdmonition(quoteContent, "'fas fa-quote-left'");
            }

            Console.WriteLine($"Not Found for relative path {linkRelativePath}");
            Console.WriteLine($"Not Found for absolute path {targetNotePath}");
            return "";

            string GetReferenceLink()
            {
                string title = ObsidianNoteParser.GetTitle(targetNotePath);
                if (!ObsidianNoteParser.IsRequiredToBePublished(targetNotePath)) return title;

                string referencedPostPath = Adapter.AdaptPostPath(Adapter.ConvertMdLink2Relative(targetNotePath));
                return $"[{title}]({referencedPostPath})";
            }
        }
    }

    private string FormatBlockLink(string content, string srcNotePath)
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

            if (!File.Exists(targetNotePath))
                targetNotePath = ObsidianNoteParser.GetAbsoluteLinkPath(srcNotePath, linkRelativePath);

            if (File.Exists(targetNotePath))
            {
                string blockContent = File.ReadAllLines(targetNotePath).ToList()
                                          .First(line => line.EndsWith(blockId)).Replace(blockId, "");

                string quoteContent = $"""
                                       {blockContent}
                                       ———— {GetReferenceLink()}
                                       """;

                return Adapter.AdaptAdmonition(quoteContent, "'fas fa-quote-left'");
            }

            Console.WriteLine($"Not Found for relative path {linkRelativePath}");
            Console.WriteLine($"Not Found for absolute path {targetNotePath}");
            return "";

            string GetReferenceLink()
            {
                string title = ObsidianNoteParser.GetTitle(targetNotePath);
                if (!ObsidianNoteParser.IsRequiredToBePublished(targetNotePath)) return title;

                string referencedPostPath = Adapter.AdaptPostPath(Adapter.ConvertMdLink2Relative(targetNotePath));
                return $"[{title}]({referencedPostPath})";
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
        string linkPattern = @"\[(.*?)\]\((?!http)(.*?)\)";
        string ret = Regex.Replace(content, linkPattern, ReplaceLink);
        return ret;

        string ReplaceLink(Match match)
        {
            string linkText = match.Groups[1].Value;
            string linkRelativePath = match.Groups[2].Value;

            return Adapter.AdaptLink(linkText, linkRelativePath, m_SrcNotePath, m_DstPostPath);
        }
    }

    private string FormatHtmlImagePaths(string content)
    {
        string pattern = @"<img\s+([^>]*?)src=""([^""]*?)""([^>]*?)>";
        return Regex.Replace(content, pattern, ReplaceImagePath);

        string ReplaceImagePath(Match match)
        {
            string beforeSrc = match.Groups[1].Value;
            string srcPath = match.Groups[2].Value;
            string afterSrc = match.Groups[3].Value;
            
            if (srcPath.StartsWith("assets/"))
            {
                srcPath = srcPath.Substring("assets/".Length);
            }
            
            string processedPath = Adapter.AdaptAssetPath(srcPath);
            
            processedPath = "/" + processedPath;
            
            return $"<img {beforeSrc}src=\"{processedPath}\"{afterSrc}>";
        }
    }
}