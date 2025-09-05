using Obsidian2.ConsoleUI;
using Obsidian2.Utilities;
using Obsidian2.Utilities.Obsidian;
using Obsidian2.Utilities.Hexo;
using Progress = Obsidian2.ConsoleUI.Progress;

namespace Obsidian2;

internal class Obsidian2HexoHandler
{
    public static DirectoryInfo obsidianVaultDir { get; private set; }
    public static DirectoryInfo hexoPostsDir { get; private set; }
    public static DirectoryInfo obsidianTempDir { get; private set; }

    public Obsidian2HexoHandler(DirectoryInfo obsidianVaultDir, DirectoryInfo hexoPostsDir)
    {
        Obsidian2HexoHandler.obsidianVaultDir = obsidianVaultDir;
        Obsidian2HexoHandler.hexoPostsDir = hexoPostsDir;
        obsidianTempDir = new DirectoryInfo(Obsidian2HexoHandler.hexoPostsDir + "\\temp\\");
    }

    public async Task Process()
    {
        if (obsidianTempDir.Exists)
            obsidianTempDir.Delete(true);

        Status.CreateStatus("Making backup", () =>
        {
            FileSystemUtils.DeepCopyDirectory(obsidianVaultDir, obsidianTempDir.FullName, ConfigurationMgr.configuration.ignoresPaths.ToList())
                            .Wait();
        });

        await ConvertObsidianNoteToHexoPostBundles();

        obsidianTempDir.Delete(true);
    }

    private async Task ConvertObsidianNoteToHexoPostBundles()
    {
        var markdownFiles = GetMarkdownFiles();
        var readyToPublishFiles = new List<string>();

        await Progress.CreateProgress("Converting Obsidian Notes to Hexo Posts", async progressTask =>
        {
            progressTask.MaxValue = markdownFiles.Count;

            var postFormatters = await CreateHexoPostFormatters(markdownFiles, readyToPublishFiles);

            await ProcessPostFormatters(postFormatters, progressTask);
        });

        // Step 3: Update status for successfully processed files
        UpdateReadyFilesToPublished(readyToPublishFiles);
    }

    private List<string> GetMarkdownFiles()
    {
        return Directory.GetFiles(obsidianTempDir.FullName, "*.md", SearchOption.AllDirectories)
                       .Where(file => file.EndsWith(".md"))
                       .ToList();
    }

    /// <summary>
    /// Creates HexoPostFormatter instances for all valid markdown files.
    /// </summary>
    /// <param name="files">List of markdown file paths to process</param>
    /// <param name="readyToPublishFiles">List to track files ready for publishing</param>
    /// <returns>List of HexoPostFormatter instances for files that should be published</returns>
    private async Task<List<HexoPostFormatter>> CreateHexoPostFormatters(List<string> files, List<string> readyToPublishFiles)
    {
        var tasks = files.Select(notePath => CreateHexoPostFormatter(notePath, readyToPublishFiles)).ToArray();
        var results = await Task.WhenAll(tasks);
        return results.Where(formatter => formatter != null).ToList();
    }

    /// <summary>
    /// Creates a Hexo post bundle and returns a formatter for the created post.
    /// </summary>
    /// <param name="notePath">Path to the Obsidian note file</param>
    /// <param name="readyToPublishFiles">List to track files ready for publishing</param>
    /// <returns>HexoPostFormatter for the created post, or null if the note should not be published</returns>
    private async Task<HexoPostFormatter> CreateHexoPostFormatter(string notePath, List<string> readyToPublishFiles)
    {
        try
        {
            if (!ObsidianNoteUtils.IsRequiredToBePublished(notePath))
                return null;

            // Track files that are ready to publish for status update
            if (ObsidianNoteUtils.IsReadyToPublish(notePath))
            {
                string originalPath = GetOriginalFilePath(notePath);
                lock (readyToPublishFiles)
                {
                    readyToPublishFiles.Add(originalPath);
                }
            }

            string postPath = await HexoUtils.CreateHexoPostBundle(notePath, hexoPostsDir);
            return new HexoPostFormatter(notePath, postPath);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error creating post bundle for {notePath}: {e.Message}");
            return null;
        }
    }

    private async Task ProcessPostFormatters(List<HexoPostFormatter> formatters, Spectre.Console.ProgressTask progressTask)
    {
        var tasks = formatters.Select(formatter => ProcessSingleFormatter(formatter, progressTask)).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task ProcessSingleFormatter(HexoPostFormatter formatter, Spectre.Console.ProgressTask progressTask)
    {
        try
        {
            await formatter.Format();
            await CopyAssetIfExist(formatter.postPath);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error processing formatter for {formatter.postPath}: {e.Message}");
            Console.WriteLine($"Stack trace: {e.StackTrace}");
        }
        finally
        {
            progressTask.Increment(1);
        }
    }

    private async Task CopyAssetIfExist(string notePath)
    {
        string noteName = Path.GetFileNameWithoutExtension(notePath);
        string noteAssetsDir = Path.GetDirectoryName(notePath) + @"\assets\" + noteName;

        if (!Path.Exists(noteAssetsDir)) return;

        string postAssetsDir = HexoUtils.ConvertPathForHexoPost(hexoPostsDir + "\\" + noteName);
        if (Directory.Exists(postAssetsDir)) 
        {
            Directory.Delete(postAssetsDir, true);
        }
        
        await FileSystemUtils.DeepCopyDirectory(new DirectoryInfo(noteAssetsDir), postAssetsDir);
    }

    private string GetOriginalFilePath(string tempFilePath)
    {
        string relativePath = Path.GetRelativePath(obsidianTempDir.FullName, tempFilePath);
        return Path.Combine(obsidianVaultDir.FullName, relativePath);
    }

    private void UpdateReadyFilesToPublished(List<string> filePaths)
    {
        foreach (string filePath in filePaths)
        {
            try
            {
                YAMLUtils.UpdateYamlMetadata(filePath, "publishStatus", "published");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to update status for {filePath}: {ex.Message}");
            }
        }
    }
}