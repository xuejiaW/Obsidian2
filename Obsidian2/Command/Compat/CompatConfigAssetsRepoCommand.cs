using System.CommandLine;

namespace Obsidian2;

public static class CompatConfigAssetsRepoCommand
{
    internal static Command CreateCommand()
    {
        var assetsRepoCommand = new Command("assets-repo", "Manage assets repository configuration");
        assetsRepoCommand.AddCommand(CreateTokenCommand());
        assetsRepoCommand.AddCommand(CreateOwnerCommand());
        assetsRepoCommand.AddCommand(CreateNameCommand());
        assetsRepoCommand.AddCommand(CreateBranchCommand());
        assetsRepoCommand.AddCommand(CreateImagePathCommand());
        return assetsRepoCommand;
    }

    private static Command CreateTokenCommand()
    {
        var tokenCmd = new Command("token", "Set GitHub Personal Access Token for assets repository");
        var tokenArg = new Argument<string>("token", "GitHub Personal Access Token");
        tokenCmd.AddArgument(tokenArg);
        tokenCmd.SetHandler(SetToken, tokenArg);
        return tokenCmd;

        void SetToken(string token)
        {
            ConfigurationMgr.configuration.GitHub.PersonalAccessToken = token;
            ConfigurationMgr.Save();
            Console.WriteLine("Assets repository access token has been set.");
        }
    }

    private static Command CreateOwnerCommand()
    {
        var ownerCmd = new Command("owner", "Set assets repository owner");
        var ownerArg = new Argument<string>("owner", "Repository owner/organization name");
        ownerCmd.AddArgument(ownerArg);
        ownerCmd.SetHandler(SetOwner, ownerArg);
        return ownerCmd;

        void SetOwner(string owner)
        {
            ConfigurationMgr.configuration.GitHub.RepoOwner = owner;
            ConfigurationMgr.Save();
            Console.WriteLine("Assets repository owner has been set.");
        }
    }

    private static Command CreateNameCommand()
    {
        var nameCmd = new Command("name", "Set assets repository name");
        var nameArg = new Argument<string>("name", "Repository name");
        nameCmd.AddArgument(nameArg);
        nameCmd.SetHandler(SetName, nameArg);
        return nameCmd;

        void SetName(string name)
        {
            ConfigurationMgr.configuration.GitHub.RepoName = name;
            ConfigurationMgr.Save();
            Console.WriteLine("Assets repository name has been set.");
        }
    }

    private static Command CreateBranchCommand()
    {
        var branchCmd = new Command("branch", "Set assets repository branch");
        var branchArg = new Argument<string>("branch", "Branch name");
        branchCmd.AddArgument(branchArg);
        branchCmd.SetHandler(SetBranch, branchArg);
        return branchCmd;

        void SetBranch(string branch)
        {
            ConfigurationMgr.configuration.GitHub.BranchName = branch;
            ConfigurationMgr.Save();
            Console.WriteLine("Assets repository branch has been set.");
        }
    }

    private static Command CreateImagePathCommand()
    {
        var imagePathCmd = new Command("image-path", "Set base path for images in assets repository");
        var pathArg = new Argument<string>("path", "Base path for images (e.g., 'images', 'assets', 'static/resources')");
        imagePathCmd.AddArgument(pathArg);
        imagePathCmd.SetHandler(SetImagePath, pathArg);
        return imagePathCmd;

        void SetImagePath(string path)
        {
            ConfigurationMgr.configuration.GitHub.ImageBasePath = path;
            ConfigurationMgr.Save();
            Console.WriteLine("Assets repository image path has been set.");
        }
    }
}
