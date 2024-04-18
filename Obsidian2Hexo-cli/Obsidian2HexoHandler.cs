using Obsidian2Hexo.ConsoleUI;
using Progress = Obsidian2Hexo.ConsoleUI.Progress;

namespace Obsidian2Hexo;

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
            obsidianVaultDir.DeepCopy(obsidianTempDir.FullName, new List<string> {".obsidian", ".git", ".trash"})
                            .Wait();
        });

        await ConvertObsidianNoteToHexoPostBundles();

        obsidianTempDir.Delete(true);
        Console.WriteLine("Convert Completed");
    }

    private async Task ConvertObsidianNoteToHexoPostBundles()
    {
        List<string> files = Directory.GetFiles(obsidianTempDir.FullName, "*.md", SearchOption.AllDirectories)
                                      .Where(file => file.EndsWith(".md")).ToList();

        await Progress.CreateProgress("Converting Obsidian Notes to Hexo Posts", async progressTask =>
        {
            progressTask.MaxValue = files.Count;

            Task<HexoPostFormatter>[] hexoPostsPath = files.Select(notePath => Task.Run(async () =>
            {
                var generator = new HexoPostGenerator(hexoPostsDir);
                if (!ObsidianNoteParser.IsRequiredToBePublished(notePath))
                    return null;

                string postPath = await generator.CreateHexoPostBundle(notePath);
                return new HexoPostFormatter(notePath, postPath);
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

        async Task CopyAssetIfExist(string notePath)
        {
            string noteName = Path.GetFileNameWithoutExtension(notePath);
            string noteAssetsDir = Path.GetDirectoryName(notePath) + @"\assets\" + noteName;

            if (!Path.Exists(noteAssetsDir)) return;

            string postAssetsDir = HexoPostStyleAdapter.AdaptPostPath(hexoPostsDir + "\\" + noteName);
            if (Directory.Exists(postAssetsDir)) Directory.Delete(postAssetsDir, true);
            await new DirectoryInfo(noteAssetsDir).DeepCopy(postAssetsDir);
        }
    }
}