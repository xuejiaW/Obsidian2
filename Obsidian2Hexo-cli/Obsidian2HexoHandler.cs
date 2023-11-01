using System.Net;

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
                 .Where(file => file.EndsWith(".md")).ToList().ForEach(file =>
                  {
                      Console.WriteLine($"Handle file is {file}");
                      var generator = new HexoPostGenerator(file, m_HexoPostsDir);
                      string postPath = generator.Generate();

                      if (!string.IsNullOrEmpty(postPath))
                      {
                          var formatter = new HexoPostFormatter(file, postPath);
                          formatter.Format();
                      }
                  });
    }
}