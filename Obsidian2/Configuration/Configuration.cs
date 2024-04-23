namespace Obsidian2;

public class Configuration
{
    public string obsidianVaultPath { get; set; }
    public string hexoPostsPath { get; set; }

    public HashSet<string> ignoresPaths { get; set; } = new();
}