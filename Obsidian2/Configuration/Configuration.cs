namespace Obsidian2;

public class Configuration
{
    public string obsidianVaultPath { get; set; }
    public string hexoPostsPath { get; set; }
    public HashSet<string> ignoresPaths { get; set; } = new();
    
    // GitHub相关配置
    public GitHubConfig GitHub { get; set; } = new GitHubConfig();
}

public class GitHubConfig
{
    public string PersonalAccessToken { get; set; }
    public string RepoOwner { get; set; }
    public string RepoName { get; set; }
    public string BranchName { get; set; } = "master";
    public string ImageBasePath { get; set; } = "images";
}