using Obsidian2.Utilities;

namespace Obsidian2.Utilities.Obsidian;

/// <summary>
/// Provides utilities for parsing and processing Obsidian note files, including metadata extraction and publishing status management.
/// </summary>
public static class ObsidianNoteUtils
{
    /// <summary>
    /// Determines if an Obsidian note is required to be published based on its publishStatus metadata.
    /// </summary>
    /// <param name="notePath">Absolute path to the Obsidian note file</param>
    /// <returns>True if the note should be published (status is "ready" or "published"); otherwise, false</returns>
    /// <example>
    /// <code>
    /// // Check if note should be published
    /// if (ObsidianNoteUtils.IsRequiredToBePublished(@"C:\Notes\article.md"))
    /// {
    ///     // Process the note for publication
    /// }
    /// 
    /// // Note with publishStatus: ready -> returns true
    /// // Note with publishStatus: published -> returns true  
    /// // Note with publishStatus: draft -> returns false
    /// // Note with publishStatus: private -> returns false
    /// </code>
    /// </example>
    public static bool IsRequiredToBePublished(string notePath)
    {
        if (!File.Exists(notePath)) return false;

        var result = YAMLUtils.GetYAMLMetadata(notePath);
        if (result == null || !result.TryGetValue("publishStatus", out object statusValue)) 
            return false;

        string status = statusValue.ToString().ToLowerInvariant();
        return status == "ready" || status == "published";
    }

    /// <summary>
    /// Checks if an Obsidian note is in "ready" status, indicating it's prepared for publishing but not yet published.
    /// </summary>
    /// <param name="notePath">Absolute path to the Obsidian note file</param>
    /// <returns>True if the note has publishStatus "ready"; otherwise, false</returns>
    /// <example>
    /// <code>
    /// // Check if note is ready for publishing
    /// if (ObsidianNoteUtils.IsReadyToPublish(@"C:\Notes\draft.md"))
    /// {
    ///     // Update status to "published" after processing
    /// }
    /// 
    /// // Note with publishStatus: ready -> returns true
    /// // Note with publishStatus: published -> returns false
    /// // Note with publishStatus: draft -> returns false
    /// </code>
    /// </example>
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

    /// <summary>
    /// Extracts the title from an Obsidian note's YAML front matter, with fallback to filename.
    /// </summary>
    /// <param name="notePath">Absolute path to the Obsidian note file</param>
    /// <returns>Note title from YAML metadata, or filename if title not found</returns>
    /// <example>
    /// <code>
    /// // Extract title from note
    /// var title = ObsidianNoteUtils.GetTitle(@"C:\Notes\my-article.md");
    /// 
    /// // If YAML contains "title: My Great Article"
    /// // Returns: "My Great Article"
    /// 
    /// // If no title in YAML
    /// // Returns: "my-article" (filename without extension)
    /// </code>
    /// </example>
    public static string GetTitle(string notePath)
    {
        if (!File.Exists(notePath)) return Path.GetFileName(notePath);

        var result = YAMLUtils.GetYAMLMetadata(notePath);

        if (result == null || !result.TryGetValue("title", out object value))
            return Path.GetFileNameWithoutExtension(notePath);

        return value.ToString();
    }

    /// <summary>
    /// Converts a relative link path to an absolute path based on the note's directory location.
    /// </summary>
    /// <param name="notePath">Absolute path to the source note file</param>
    /// <param name="relativeLink">Relative link path to convert</param>
    /// <returns>Absolute path to the linked file, with URL-encoded spaces decoded</returns>
    /// <example>
    /// <code>
    /// // Convert relative link to absolute path
    /// var notePath = @"C:\Vault\Notes\article.md";
    /// var relativeLink = "../Images/photo%20with%20spaces.jpg";
    /// var absolutePath = ObsidianNoteUtils.GetAbsoluteLinkPath(notePath, relativeLink);
    /// 
    /// // Returns: "C:\Vault\Images\photo with spaces.jpg"
    /// </code>
    /// </example>
    public static string GetAbsoluteLinkPath(string notePath, string relativeLink)
    {
        string decodedRelativeLink = relativeLink.Replace("%20", " ");
        string noteDir = Path.GetDirectoryName(notePath);
        return Path.GetFullPath(Path.Combine(noteDir!, decodedRelativeLink));
    }

    /// <summary>
    /// Constructs a note path by combining folder path with note path and decoding URL-encoded spaces.
    /// </summary>
    /// <param name="noteFolderPath">Base folder path for notes</param>
    /// <param name="notePath">Relative path to the note</param>
    /// <returns>Combined path with URL-encoded spaces decoded</returns>
    /// <example>
    /// <code>
    /// // Construct full note path
    /// var folderPath = @"C:\Vault\Articles";
    /// var notePath = "/My%20Note.md";
    /// var fullPath = ObsidianNoteUtils.GetNotePathBasedOnFolder(folderPath, notePath);
    /// 
    /// // Returns: "C:\Vault\Articles/My Note.md"
    /// </code>
    /// </example>
    public static string GetNotePathBasedOnFolder(string noteFolderPath, string notePath)
    {
        return $"{noteFolderPath}{notePath}".Replace("%20", " ");
    }
}
