namespace Obsidian2Hexo;

internal class Obsidian2HexoHandler
{
    private DirectoryInfo m_ObsidianVaultDir;
    private DirectoryInfo m_HexoPostsDir;
    private DirectoryInfo m_ObsidianTempDir;

    public Obsidian2HexoHandler(DirectoryInfo obsidianVaultDir, DirectoryInfo hexoPostsDir)
    {
        m_ObsidianVaultDir = obsidianVaultDir;
        m_HexoPostsDir = hexoPostsDir;
        m_ObsidianTempDir = new DirectoryInfo(m_HexoPostsDir + "\\temp\\");
    }

    public void Process()
    {
        if (m_ObsidianTempDir.Exists)
            m_ObsidianTempDir.Delete(true);
        m_ObsidianVaultDir.DeepCopy(m_ObsidianTempDir.FullName, new List<string> {".obsidian", ".git"});
        ConvertObsidianNoteToHexoPostBundles();
        m_ObsidianTempDir.Delete(true);
        Console.WriteLine("Convert Completed");
    }

    private void ConvertObsidianNoteToHexoPostBundles()
    {
        Directory.GetFiles(m_ObsidianTempDir.FullName, "*.md", SearchOption.AllDirectories)
                 .Where(file => file.EndsWith(".md")).ToList().ForEach(notePath =>
                  {
                      Console.WriteLine($"Handle file is {notePath}");
                      var generator = new HexoPostGenerator(notePath, m_HexoPostsDir);
                      bool requiredToBePublished = generator.Generate(out string postPath);
                      if (!requiredToBePublished) return;

                      var formatter = new HexoPostFormatter(notePath, postPath, m_ObsidianTempDir.FullName);
                      formatter.Format();

                      CopyAssetIfExist(notePath);
                  });

        void CopyAssetIfExist(string notePath)
        {
            string noteName = Path.GetFileNameWithoutExtension(notePath);
            string noteAssetsDir = Path.GetDirectoryName(notePath) + @"\assets\" + noteName;

            if (!Path.Exists(noteAssetsDir)) return;

            string postAssetsDir = HexoPostStyleAdapter.AdaptPostPath(m_HexoPostsDir + "\\" + noteName);
            if (Directory.Exists(postAssetsDir)) Directory.Delete(postAssetsDir, true);
            new DirectoryInfo(noteAssetsDir).DeepCopy(postAssetsDir);
        }
    }
}