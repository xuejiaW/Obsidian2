namespace Obsidian2;

internal class HexoPostGenerator
{
    private DirectoryInfo m_PostsDir = null;

    public HexoPostGenerator(DirectoryInfo postDir) { m_PostsDir = postDir; }


    public async Task<string> CreateHexoPostBundle(string notePath)
    {
        string noteName = Path.GetFileNameWithoutExtension(notePath);

        string postPath = Path.Join(m_PostsDir.FullName, HexoPostStyleAdapter.AdaptPostPath(noteName) + ".md");

        if (File.Exists(postPath)) File.Delete(postPath);

        File.Copy(notePath, postPath);

        var noteAssetsDir = new DirectoryInfo(Path.Join($"{Path.GetDirectoryName(notePath)}\\assets\\{noteName}"));
        if (!noteAssetsDir.Exists) return postPath;
        var postAssetsDir = new DirectoryInfo(postPath.Replace(".md", ""));
        if (postAssetsDir.Exists) postAssetsDir.Delete(true);
        await noteAssetsDir.DeepCopy(postAssetsDir.FullName);

        Directory.GetFiles(postAssetsDir.FullName, "*.*", SearchOption.AllDirectories).ToList().ForEach(file =>
        {
            File.Move(file, HexoPostStyleAdapter.AdaptAssetPath(file));
        });
        return postPath;
    }
}