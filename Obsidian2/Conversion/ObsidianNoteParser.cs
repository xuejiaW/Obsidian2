using Obsidian2.Utilities;

namespace Obsidian2;

internal static class ObsidianNoteParser
{
    public static bool IsRequiredToBePublished(string notePath)
    {
        if (!File.Exists(notePath)) return false;

        var result = YAMLUtils.GetYAMLMetadata(notePath);
        if (result == null || !result.TryGetValue("publishStatus", out object statusValue)) 
            return false;

        string status = statusValue.ToString().ToLowerInvariant();
        return status == "ready" || status == "published";
    }

    public static bool IsReadyToPublish(string notePath)
    {
        if (!File.Exists(notePath)) return false;

        var result = YAMLUtils.GetYAMLMetadata(notePath);
        if (result == null) return false;

        if (result.TryGetValue("publishStatus", out object statusValue))
        {
            return statusValue.ToString().ToLowerInvariant() == "ready";
        }

        return false;
    }

    public static string GetTitle(string notePath)
    {
        if (!File.Exists(notePath)) return Path.GetFileName(notePath);

        var result = YAMLUtils.GetYAMLMetadata(notePath);

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
}