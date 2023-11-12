namespace Obsidian2Hexo;

internal class HexoPostGenerator
{
    private string m_NotePath = null;
    private DirectoryInfo m_PostsDir = null;

    public HexoPostGenerator(string notePath, DirectoryInfo postDir)
    {
        m_NotePath = notePath;
        m_PostsDir = postDir;
    }

    public bool Generate(out string notePath)
    {
        notePath = null;
        if (!ObsidianNoteParser.IsRequiredToBePublished(m_NotePath)) return false;
        notePath = CreateHexoPostBundle(m_NotePath);
        return true;
    }


    private string CreateHexoPostBundle(string notePath)
    {
        string noteName = Path.GetFileNameWithoutExtension(notePath);

        string postPath = Path.Join(m_PostsDir.FullName, HexoPostStyleAdapter.AdaptPostPath(noteName) + ".md");

        if (File.Exists(postPath)) File.Delete(postPath);

        File.Copy(notePath, postPath);

        var noteAssetsDir = new DirectoryInfo(Path.Join($"{Path.GetDirectoryName(notePath)}\\assets\\{noteName}"));
        if (!noteAssetsDir.Exists) return postPath;
        var postAssetsDir = new DirectoryInfo(postPath.Replace(".md", ""));
        if (postAssetsDir.Exists) postAssetsDir.Delete(true);
        noteAssetsDir.DeepCopy(postAssetsDir.FullName);

        Directory.GetFiles(postAssetsDir.FullName, "*.*", SearchOption.AllDirectories).ToList().ForEach(file =>
        {
            File.Move(file, file.ToLower());
        });
        return postPath;
    }
}