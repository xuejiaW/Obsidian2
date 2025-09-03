using System.Text;
using System.Text.RegularExpressions;
using Obsidian2.Utilities.Admonition;

namespace Obsidian2;

internal static class AdmonitionsFormatter
{
    public static string FormatCodeBlockStyle2ButterflyStyle(string content)
    {
        var mapping = AdmonitionMappingUtils.GetCodeBlockToButterflyMapping();
        return AdmonitionStyleUtils.ConvertCodeBlockToButterflyStyle(content, mapping);
    }

    public static string FormatMkDocsStyle2ButterflyStyle(string content)
    {
        var mapping = AdmonitionMappingUtils.GetMkDocsToButterflyMapping();
        return AdmonitionStyleUtils.ConvertMkDocsToButterflyStyle(content, mapping);
    }

    public static string FormatMkDocsCalloutToQuote(string content)
    {
        return AdmonitionStyleUtils.ConvertMkDocsCalloutToQuote(content);
    }
}