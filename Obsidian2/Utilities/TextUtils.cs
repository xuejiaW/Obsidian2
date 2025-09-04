using System.Text;
using TinyPinyin;

namespace Obsidian2.Utilities;

/// <summary>
/// Provides utilities for text processing and conversion operations.
/// </summary>
public static class TextUtils
{
    #region Character Conversion Methods
    /// <summary>
    /// Converts Chinese characters in text to Pinyin representation for URL-friendly paths.
    /// </summary>
    /// <param name="text">Text containing Chinese characters</param>
    /// <returns>Text with Chinese characters converted to Pinyin, separated by underscores</returns>
    /// <example>
    /// <code>
    /// // Simple Chinese text conversion
    /// var result1 = TextUtils.ConvertChineseToPinyin("我的文章");
    /// // Returns: "wo_de_wen_zhang"
    /// 
    /// // Mixed content conversion
    /// var result2 = TextUtils.ConvertChineseToPinyin("Hello 世界 World");
    /// // Returns: "Hello shi_jie World"
    /// 
    /// // Complex text with punctuation
    /// var result3 = TextUtils.ConvertChineseToPinyin("第1章：介绍");
    /// // Returns: "di_1zhang：jie_shao"
    /// </code>
    /// </example>
    public static string ConvertChineseToPinyin(string text)
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

    /// <summary>
    /// Converts spaces and URL-encoded spaces to underscores for path normalization.
    /// </summary>
    /// <param name="text">Text containing spaces or %20 encoded spaces</param>
    /// <returns>Text with spaces converted to underscores</returns>
    /// <example>
    /// <code>
    /// // Basic space conversion
    /// var result1 = TextUtils.ConvertSpaceToUnderscore("My File Name");
    /// // Returns: "My_File_Name"
    /// 
    /// // URL-encoded space conversion
    /// var result2 = TextUtils.ConvertSpaceToUnderscore("File%20Name%20Here");
    /// // Returns: "File_Name_Here"
    /// 
    /// // Mixed spaces conversion
    /// var result3 = TextUtils.ConvertSpaceToUnderscore("Some File%20Mixed Spaces");
    /// // Returns: "Some_File_Mixed_Spaces"
    /// </code>
    /// </example>
    public static string ConvertSpaceToUnderscore(string text)
    {
        string result = text.Replace(" ", "_");
        result = result.Replace("%20", "_");
        return result;
    }

    /// <summary>
    /// Converts dots to underscores for identifiers that need to be URL-safe or programming-safe.
    /// </summary>
    /// <param name="text">Text containing dots</param>
    /// <returns>Text with dots converted to underscores</returns>
    /// <example>
    /// <code>
    /// // Version number conversion
    /// var result1 = TextUtils.ConvertDotToUnderscore("version.1.2.3");
    /// // Returns: "version_1_2_3"
    /// 
    /// // File extension handling
    /// var result2 = TextUtils.ConvertDotToUnderscore("file.name.txt");
    /// // Returns: "file_name_txt"
    /// 
    /// // Decimal number conversion
    /// var result3 = TextUtils.ConvertDotToUnderscore("section.2.1");
    /// // Returns: "section_2_1"
    /// </code>
    /// </example>
    public static string ConvertDotToUnderscore(string text)
    {
        return text.Replace(".", "_");
    }
    #endregion

    #region Combined Conversion Methods
    /// <summary>
    /// Converts text to URL-safe format by handling Chinese characters, spaces, and dots.
    /// This is a convenience method that combines multiple text conversion operations.
    /// </summary>
    /// <param name="text">Original text to convert</param>
    /// <returns>URL-safe text with Chinese converted to Pinyin, spaces and dots to underscores</returns>
    /// <example>
    /// <code>
    /// // Complete text conversion
    /// var result1 = TextUtils.ConvertToUrlSafeText("我的文章 v1.0");
    /// // Returns: "wo_de_wen_zhang_v1_0"
    /// 
    /// // Complex text with various characters
    /// var result2 = TextUtils.ConvertToUrlSafeText("第2.1章 项目设置");
    /// // Returns: "di_2_1zhang_xiang_mu_she_zhi"
    /// </code>
    /// </example>
    public static string ConvertToUrlSafeText(string text)
    {
        string result = ConvertChineseToPinyin(text);
        result = ConvertSpaceToUnderscore(result);
        result = ConvertDotToUnderscore(result);
        return result;
    }
    #endregion
}
