using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using TinyPinyin;

namespace Obsidian2;

internal static class HexoPostStyleAdapter
{
    public static string AdaptPostPath(string path)
    {
        string ret = MakeChineseToPinyin(path);
        ret = ConvertSpaceToUnderScore(ret);
        return ret.ToLower();

        string MakeChineseToPinyin(string text)
        {
            var pinyinResult = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (PinyinHelper.IsChinese(c))
                {
                    pinyinResult.Append(PinyinHelper.GetPinyin(c));
                    if (i < text.Length - 1 && !PinyinHelper.IsChinese(text[i + 1])) continue;
                    pinyinResult.Append('_');
                }
                else
                    pinyinResult.Append(c);
            }

            if (pinyinResult.Length == 0) return string.Empty;
            if (pinyinResult[^1] == '_')
                pinyinResult.Remove(pinyinResult.Length - 1, 1);

            return pinyinResult.ToString();
        }
    }

    public static string AdaptAssetPath(string path)
    {
        string ret = ConvertSpaceToUnderScore(path);
        return ret.ToLower();
    }

    public static string AdaptLink(string linkText, string linkRelativePath, string notePath, string postPath)
    {
        string fragment = "";

        if (linkRelativePath.Contains(".md"))
        {
            // If link contains title fragment, handle it firstly.
            if (linkRelativePath.Contains('#'))
            {
                string[] splitLink = linkRelativePath.Split("#", 2);
                linkRelativePath = splitLink[0];
                fragment = "/#" + AdaptTitleFragment(splitLink[1]);
            }

            string absLinkPath = ObsidianNoteParser.GetAbsoluteLinkPath(notePath, linkRelativePath);
            if (!ObsidianNoteParser.IsRequiredToBePublished(absLinkPath)) return linkText;

            // As all posts are in the same directory, we can just use the file name as the link path.
            linkRelativePath = ConvertMdLink2Relative(absLinkPath);
        }

        if (linkRelativePath.StartsWith("#"))
        {
            fragment = "/#" + AdaptTitleFragment(linkRelativePath[1..]);
            linkRelativePath = ConvertMdLink2Relative(postPath);
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
            var regex = new Regex(@"\|\d+$");
            linkText = regex.Replace(linkText, "");
        }

        linkRelativePath = AdaptPostPath(linkRelativePath);
        return $"[{linkText}]({linkRelativePath}{fragment})";

        string AdaptTitleFragment(string path) { return ConvertDotToUnderScore(ConvertSpaceToUnderScore(path)); }
    }

    public static string AdaptAdmonition(string calloutContent, string type)
    {
        return $"{{% note {type} %}}\n{calloutContent}\n{{% endnote %}}";
    }

    public static string ConvertMdLink2Relative(string filePath)
    {
        return "/" + Path.GetFileNameWithoutExtension(filePath);
    }

    private static string ConvertSpaceToUnderScore(string path)
    {
        string ret = path.Replace(" ", "_");
        ret = ret.Replace("%20", "_");

        return ret;
    }

    private static string ConvertDotToUnderScore(string path) { return path.Replace(".", "_"); }
}