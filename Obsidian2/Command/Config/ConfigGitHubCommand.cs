using System.CommandLine;

namespace Obsidian2;

public static class ConfigGitHubCommand
{
    internal static Command CreateTokenCommand()
    {
        var setGitHubTokenCmd = new Command("github-token", "Set GitHub Personal Access Token");
        var tokenArg = new Argument<string>();
        setGitHubTokenCmd.AddArgument(tokenArg);
        setGitHubTokenCmd.SetHandler(SetGitHubToken, tokenArg);
        return setGitHubTokenCmd;

        void SetGitHubToken(string token)
        {
            ConfigurationMgr.configuration.GitHub.PersonalAccessToken = token;
            ConfigurationMgr.Save();
            Console.WriteLine("GitHub Personal Access Token has been set.");
        }
    }

    internal static Command CreateOwnerCommand()
    {
        var setGitHubOwnerCmd = new Command("github-owner", "Set GitHub repository owner");
        var ownerArg = new Argument<string>();
        setGitHubOwnerCmd.AddArgument(ownerArg);
        setGitHubOwnerCmd.SetHandler(SetGitHubOwner, ownerArg);
        return setGitHubOwnerCmd;

        void SetGitHubOwner(string owner)
        {
            ConfigurationMgr.configuration.GitHub.RepoOwner = owner;
            ConfigurationMgr.Save();
            Console.WriteLine("GitHub repository owner has been set.");
        }
    }

    internal static Command CreateRepoCommand()
    {
        var setGitHubRepoCmd = new Command("github-repo", "Set GitHub repository name");
        var repoArg = new Argument<string>();
        setGitHubRepoCmd.AddArgument(repoArg);
        setGitHubRepoCmd.SetHandler(SetGitHubRepo, repoArg);
        return setGitHubRepoCmd;

        void SetGitHubRepo(string repo)
        {
            ConfigurationMgr.configuration.GitHub.RepoName = repo;
            ConfigurationMgr.Save();
            Console.WriteLine("GitHub repository name has been set.");
        }
    }

    internal static Command CreateBranchCommand()
    {
        var setGitHubBranchCmd = new Command("github-branch", "Set GitHub repository branch");
        var branchArg = new Argument<string>();
        setGitHubBranchCmd.AddArgument(branchArg);
        setGitHubBranchCmd.SetHandler(SetGitHubBranch, branchArg);
        return setGitHubBranchCmd;

        void SetGitHubBranch(string branch)
        {
            ConfigurationMgr.configuration.GitHub.BranchName = branch;
            ConfigurationMgr.Save();
            Console.WriteLine("GitHub repository branch has been set.");
        }
    }

    internal static Command CreateImagePathCommand()
    {
        var setGitHubImagePathCmd = new Command("github-image-path", "Set base path for images in GitHub repository");
        var pathArg = new Argument<string>();
        setGitHubImagePathCmd.AddArgument(pathArg);
        setGitHubImagePathCmd.SetHandler(SetGitHubImagePath, pathArg);
        return setGitHubImagePathCmd;

        void SetGitHubImagePath(string path)
        {
            ConfigurationMgr.configuration.GitHub.ImageBasePath = path;
            ConfigurationMgr.Save();
            Console.WriteLine("GitHub image base path has been set.");
        }
    }
}
