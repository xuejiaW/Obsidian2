using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Obsidian2.Utilities;

/// <summary>
/// Provides utilities for YAML metadata operations including reading and updating YAML front matter in markdown files.
/// </summary>
public static class YAMLUtils
{
    /// <summary>
    /// Updates a specific key-value pair in the YAML front matter of a markdown file.
    /// </summary>
    /// <param name="filePath">Absolute path to the markdown file</param>
    /// <param name="key">YAML property key to update</param>
    /// <param name="value">New value for the property</param>
    /// <example>
    /// <code>
    /// // Update publishStatus in a markdown file
    /// YAMLUtils.UpdateYamlMetadata(@"C:\Notes\article.md", "publishStatus", "published");
    /// 
    /// // Before: publishStatus: ready
    /// // After:  publishStatus: published
    /// </code>
    /// </example>
    public static void UpdateYamlMetadata(string filePath, string key, string value)
    {
        if (!File.Exists(filePath)) return;

        string content = File.ReadAllText(filePath);
        if (content.Length < 3) return;

        if (!content.StartsWith("---")) return;

        int yamlEnd = content.IndexOf("---", 3, StringComparison.Ordinal);
        if (yamlEnd == -1) return;

        string yamlContent = content[3..yamlEnd].Trim();
        string remainingContent = content[(yamlEnd + 3)..];

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var metadata = deserializer.Deserialize<Dictionary<string, object>>(yamlContent) ?? new Dictionary<string, object>();
        metadata[key] = value;

        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        string updatedYaml = serializer.Serialize(metadata);
        string updatedContent = "---\n" + updatedYaml + "---" + remainingContent;

        File.WriteAllText(filePath, updatedContent);
    }

    /// <summary>
    /// Extracts YAML metadata from the front matter of a markdown file.
    /// </summary>
    /// <param name="notePath">Absolute path to the markdown file</param>
    /// <returns>Dictionary containing YAML metadata, or null if no valid YAML found</returns>
    /// <example>
    /// <code>
    /// // Extract metadata from markdown file
    /// var metadata = YAMLUtils.GetYAMLMetadata(@"C:\Notes\article.md");
    /// 
    /// // File content:
    /// // ---
    /// // title: My Article
    /// // publishStatus: ready
    /// // ---
    /// // # Content here...
    /// 
    /// // Returns: {"title": "My Article", "publishStatus": "ready"}
    /// </code>
    /// </example>
    public static Dictionary<string, object> GetYAMLMetadata(string notePath)
    {
        string mdFileContent = File.ReadAllText(notePath);
        if (mdFileContent.Length < 3) return null;

        if (!mdFileContent.StartsWith("---")) return null;

        int yamlEnd = mdFileContent.IndexOf("---", 3, StringComparison.Ordinal);
        if (yamlEnd == -1) return null;

        string yamlContent = mdFileContent[3..yamlEnd].Trim();

        var result = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build()
                    .Deserialize<Dictionary<string, object>>(yamlContent);

        return result;
    }
}
