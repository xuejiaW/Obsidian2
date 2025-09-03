namespace Obsidian2.Utilities.Admonition;

/// <summary>
/// Provides mapping utilities for converting between different admonition formats and styles.
/// </summary>
public static class AdmonitionMappingUtils
{
    /// <summary>
    /// Gets the mapping dictionary for converting CodeBlock style admonitions to Butterfly style.
    /// </summary>
    /// <returns>Dictionary mapping CodeBlock admonition types to Butterfly style types</returns>
    /// <example>
    /// <code>
    /// var mapping = AdmonitionMappingUtils.GetCodeBlockToButterflyMapping();
    /// // mapping["ad-note"] returns "info"
    /// // mapping["ad-warning"] returns "warning"
    /// </code>
    /// </example>
    public static Dictionary<string, string> GetCodeBlockToButterflyMapping()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"ad-note", "info"},
            {"ad-tip", "primary"},
            {"ad-warning", "warning"},
            {"ad-quote", "'fas fa-quote-left'"},
            {"ad-fail", "danger"}
        };
    }

    /// <summary>
    /// Gets the mapping dictionary for converting MkDocs style admonitions to Butterfly style.
    /// </summary>
    /// <returns>Dictionary mapping MkDocs admonition types to Butterfly style types</returns>
    /// <example>
    /// <code>
    /// var mapping = AdmonitionMappingUtils.GetMkDocsToButterflyMapping();
    /// // mapping["note"] returns "info"
    /// // mapping["caution"] returns "warning"
    /// </code>
    /// </example>
    public static Dictionary<string, string> GetMkDocsToButterflyMapping()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
    }
}
