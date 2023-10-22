using System.Net;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Obsidian2Hexo;

internal static class ObsidianNoteParser
{
    public static bool IsRequiredToBePublished(string notePath)
    {
        if (!File.Exists(notePath)) return false;
        string mdFileContent = File.ReadAllText(notePath);

        int yamlEnd = mdFileContent.IndexOf("---", 1, StringComparison.Ordinal);
        if (yamlEnd == -1) return false;

        var result = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build()
                    .Deserialize<Dictionary<string, object>>(mdFileContent[..yamlEnd]);

        if (result == null || !result.TryGetValue("published", out object value)) return false;

        return string.Equals(value.ToString(), "true", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetAbsoluteLinkPath(string notePath, string relativeLink)
    {
        string decodedRelativeLink = relativeLink.Replace("%20", " ");
        string noteDir = Path.GetDirectoryName(notePath);
        return Path.GetFullPath(Path.Combine(noteDir!, decodedRelativeLink));
    }
}