using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Obsidian2;

internal static class ObsidianNoteParser
{
    public static bool IsRequiredToBePublished(string notePath)
    {
        if (!File.Exists(notePath)) return false;

        var result = GetYAMLMetaData(notePath);

        if (result == null || !result.TryGetValue("published", out object value)) return false;

        return string.Equals(value.ToString(), "true", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetTitle(string notePath)
    {
        if (!File.Exists(notePath)) return Path.GetFileName(notePath);

        var result = GetYAMLMetaData(notePath);

        if (result == null || !result.TryGetValue("title", out object value))
            return Path.GetFileNameWithoutExtension(notePath);

        return value.ToString();
    }

    public static string GetAbsoluteLinkPath(string notePath, string relativeLink)
    {
        string decodedRelativeLink = relativeLink.Replace("%20", " ");
        string noteDir = Path.GetDirectoryName(notePath);
        return Path.GetFullPath(Path.Combine(noteDir!, decodedRelativeLink));
    }

    public static string GetNotePathBasedOnFolder(string noteFolderPath, string notePath)
    {
        return $"{noteFolderPath}{notePath}".Replace("%20", " ");
    }

    private static Dictionary<string, object> GetYAMLMetaData(string notePath)
    {
        string mdFileContent = File.ReadAllText(notePath);
        int yamlEnd = mdFileContent.IndexOf("---", 1, StringComparison.Ordinal);
        if (yamlEnd == -1) return null;

        var result = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build()
                    .Deserialize<Dictionary<string, object>>(mdFileContent[..yamlEnd]);

        return result;
    }
}