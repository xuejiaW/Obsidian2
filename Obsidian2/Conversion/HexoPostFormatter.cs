using System.Text.RegularExpressions;
using Obsidian2.Utilities;
using Obsidian2.Utilities.Obsidian;

namespace Obsidian2;

using Adapter = HexoPostStyleAdapter;

internal class HexoPostFormatter
{
    #region Constants
    private const string QuoteIcon = "'fas fa-quote-left'";
    private const string ReferenceFooter = "———— {0}";
    private const string AssetsPrefix = "assets/";
    #endregion

    #region Fields
    private string m_SrcNotePath = null;
    private string m_DstPostPath = null;
    #endregion

    #region Properties
    public string postPath => m_DstPostPath;
    #endregion

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
        return Regex.Replace(content, RegexUtils.obsidianHeaderLink, match => 
            ProcessObsidianLink(match, srcNotePath, LinkType.Header));
    }

    private string FormatBlockLink(string content, string srcNotePath)
    {
        return Regex.Replace(content, RegexUtils.obsidianBlockLink, match => 
            ProcessObsidianLink(match, srcNotePath, LinkType.Block));
    }

    #region Link Processing Helpers
    private enum LinkType { Header, Block }

    private string ProcessObsidianLink(Match match, string srcNotePath, LinkType linkType)
    {
        // Extract link components directly from regex match
        string relativePath = match.Groups[2].Value;
        string fragment = match.Groups[3].Value.Replace("%20", " ");
        
        var targetNotePath = ObsidianNoteUtils.ResolveTargetPath(
            relativePath, srcNotePath, Obsidian2HexoHandler.obsidianTempDir.FullName);
        
        if (!File.Exists(targetNotePath))
        {
            LogFileNotFound(relativePath, targetNotePath);
            return string.Empty;
        }

        var extractedContent = linkType == LinkType.Header 
            ? ObsidianNoteUtils.ExtractHeaderContent(targetNotePath, fragment)
            : ObsidianNoteUtils.ExtractBlockContent(targetNotePath, fragment);

        if (string.IsNullOrEmpty(extractedContent))
            return string.Empty;

        return CreateQuoteAdmonition(extractedContent, targetNotePath);
    }

    private string CreateQuoteAdmonition(string content, string targetPath)
    {
        var referenceLink = CreateReferenceLink(targetPath);
        var quoteContent = $"{content}\n{string.Format(ReferenceFooter, referenceLink)}";
        
        return Adapter.AdaptAdmonition(quoteContent, QuoteIcon);
    }

    private string CreateReferenceLink(string targetPath)
    {
        var title = ObsidianNoteUtils.GetTitle(targetPath);
        
        if (!ObsidianNoteUtils.IsRequiredToBePublished(targetPath))
            return title;
        
        var referencedPostPath = Adapter.AdaptPostPath(Adapter.ConvertMdLink2Relative(targetPath));
        return $"[{title}]({referencedPostPath})";
    }

    private void LogFileNotFound(string relativePath, string absolutePath)
    {
        Console.WriteLine($"Not Found for relative path {relativePath}");
        Console.WriteLine($"Not Found for absolute path {absolutePath}");
    }
    #endregion

    private string CleanBlockLinkMark(string content)
    {
        return RegexUtils.ReplacePattern(content, RegexUtils.obsidianBlockId, string.Empty);
    }

    private string FormatMdLinkToHexoStyle(string content)
    {
        return RegexUtils.ReplacePattern(content, RegexUtils.markdownLink, match =>
        {
            string linkText = match.Groups[1].Value;
            string linkRelativePath = match.Groups[2].Value;
            return Adapter.AdaptLink(linkText, linkRelativePath, m_SrcNotePath, m_DstPostPath);
        });
    }

    private string FormatHtmlImagePaths(string content)
    {
        return RegexUtils.ReplacePattern(content, RegexUtils.htmlImage, match =>
        {
            string beforeSrc = match.Groups[1].Value;
            string srcPath = match.Groups[2].Value;
            string afterSrc = match.Groups[3].Value;
            
            if (srcPath.StartsWith(AssetsPrefix))
            {
                srcPath = srcPath.Substring(AssetsPrefix.Length);
            }
            
            string processedPath = "/" + Adapter.AdaptAssetPath(srcPath);
            
            return $"<img {beforeSrc}src=\"{processedPath}\"{afterSrc}>";
        });
    }
}