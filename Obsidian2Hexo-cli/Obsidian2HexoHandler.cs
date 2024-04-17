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

    public async void Process()
    {
        if (obsidianTempDir.Exists)
            obsidianTempDir.Delete(true);
        obsidianVaultDir.DeepCopy(obsidianTempDir.FullName, new List<string> {".obsidian", ".git", ".trash"});
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

            foreach (string notePath in files)
            {
                try
                {
                    var generator = new HexoPostGenerator(notePath, hexoPostsDir);
                    bool requiredToBePublished = generator.Generate(out string postPath);
                    if (!requiredToBePublished)
                    {
                        progressTask.Increment(1);
                        continue;
                    }

                    var formatter = new HexoPostFormatter(notePath, postPath);
                    formatter.Format();

                    await CopyAssetIfExist(notePath);

                    progressTask.Increment(1);
                } catch (Exception e)
                {
                    Console.WriteLine($"Handling file {notePath}, Exception {e} Happened: \n {e.StackTrace}");
                    progressTask.Increment(1);
                }
            }
        });

        Task CopyAssetIfExist(string notePath)
        {
            string noteName = Path.GetFileNameWithoutExtension(notePath);
            string noteAssetsDir = Path.GetDirectoryName(notePath) + @"\assets\" + noteName;

            if (!Path.Exists(noteAssetsDir)) return Task.CompletedTask;

            string postAssetsDir = HexoPostStyleAdapter.AdaptPostPath(hexoPostsDir + "\\" + noteName);
            if (Directory.Exists(postAssetsDir)) Directory.Delete(postAssetsDir, true);
            new DirectoryInfo(noteAssetsDir).DeepCopy(postAssetsDir);
            return Task.CompletedTask;
        }
    }
}