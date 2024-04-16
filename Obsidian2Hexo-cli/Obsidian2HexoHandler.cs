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

    public void Process()
    {
        if (obsidianTempDir.Exists)
            obsidianTempDir.Delete(true);
        obsidianVaultDir.DeepCopy(obsidianTempDir.FullName, new List<string> {".obsidian", ".git", ".trash"});
        ConvertObsidianNoteToHexoPostBundles();
        obsidianTempDir.Delete(true);
        Console.WriteLine("Convert Completed");
    }

    private void ConvertObsidianNoteToHexoPostBundles()
    {
        Directory.GetFiles(obsidianTempDir.FullName, "*.md", SearchOption.AllDirectories)
                 .Where(file => file.EndsWith(".md")).ToList().ForEach(notePath =>
                  {
                      try
                      {
                          var generator = new HexoPostGenerator(notePath, hexoPostsDir);
                          bool requiredToBePublished = generator.Generate(out string postPath);
                          if (!requiredToBePublished) return;

                          var formatter = new HexoPostFormatter(notePath, postPath);
                          formatter.Format();

                          CopyAssetIfExist(notePath);
                      } catch (Exception e)
                      {
                          Console.WriteLine($"Handling file {notePath}, Exception {e} Happened: \n {e.StackTrace}");
                      }
                  });

        void CopyAssetIfExist(string notePath)
        {
            string noteName = Path.GetFileNameWithoutExtension(notePath);
            string noteAssetsDir = Path.GetDirectoryName(notePath) + @"\assets\" + noteName;

            if (!Path.Exists(noteAssetsDir)) return;

            string postAssetsDir = HexoPostStyleAdapter.AdaptPostPath(hexoPostsDir + "\\" + noteName);
            if (Directory.Exists(postAssetsDir)) Directory.Delete(postAssetsDir, true);
            new DirectoryInfo(noteAssetsDir).DeepCopy(postAssetsDir);
        }
    }
}