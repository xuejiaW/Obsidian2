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
        List<string> files = Directory.GetFiles(obsidianTempDir.FullName, "*.md", SearchOption.AllDirectories)
                                      .Where(file => file.EndsWith(".md")).ToList();

        var readyToPublishFiles = new List<string>();

        await Progress.CreateProgress("Converting Obsidian Notes to Hexo Posts", async progressTask =>
        {
            progressTask.MaxValue = files.Count;

            Task<HexoPostFormatter>[] hexoPostsPath = files.Select(notePath => Task.Run(async () =>
            {
                try
                {
                    var generator = new HexoPostGenerator(hexoPostsDir);
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

                    string postPath = await generator.CreateHexoPostBundle(notePath);
                    return new HexoPostFormatter(notePath, postPath);
                } catch (Exception e)
                {
                    Console.WriteLine($"Error Happens while handling file {notePath}, Exception {e}");
                    return null;
                }
            })).ToArray();

            IEnumerable<HexoPostFormatter> validFormatters = hexoPostsPath.Select(formatter => formatter.Result);

            Task[] tasks = validFormatters.Select(formatter => Task.Run(async () =>
            {
                if (formatter == null)
                {
                    progressTask.Increment(1);
                    return;
                }

                try
                {
                    await formatter.Format();
                    await CopyAssetIfExist(formatter.postPath);
                } catch (Exception e)
                {
                    Console
                       .WriteLine($"Error handling file {formatter.postPath}, Exception {e} Happened: \n {e.StackTrace}");
                } finally
                {
                    progressTask.Increment(1);
                }
            })).ToArray();

            await Task.WhenAll(tasks);
        });

        // Update status for successfully processed ready files
        UpdateReadyFilesToPublished(readyToPublishFiles);

        async Task CopyAssetIfExist(string notePath)
        {
            string noteName = Path.GetFileNameWithoutExtension(notePath);
            string noteAssetsDir = Path.GetDirectoryName(notePath) + @"\assets\" + noteName;

            if (!Path.Exists(noteAssetsDir)) return;

            string postAssetsDir = HexoUtils.ConvertPathForHexoPost(hexoPostsDir + "\\" + noteName);
            if (Directory.Exists(postAssetsDir)) Directory.Delete(postAssetsDir, true);
            await FileSystemUtils.DeepCopyDirectory(new DirectoryInfo(noteAssetsDir), postAssetsDir);
        }
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