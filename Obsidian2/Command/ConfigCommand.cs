using System.CommandLine;
using System.Linq;

namespace Obsidian2;

public static class ConfigCommand
{
    internal static Command CreateCommand()
    {
        var configCommand = new Command("config", "Manage configuration settings");
        
        var listOption = new Option<bool>("--list", "List all configuration settings");
        configCommand.AddOption(listOption);
        configCommand.SetHandler(HandleListOption, listOption);
        
        configCommand.AddCommand(ConfigPathCommand.CreateHexoPostsDirCommand());
        configCommand.AddCommand(ConfigPathCommand.CreateObsidianVaultDirCommand());
        configCommand.AddCommand(ConfigGitHubCommand.CreateTokenCommand());
        configCommand.AddCommand(ConfigGitHubCommand.CreateOwnerCommand());
        configCommand.AddCommand(ConfigGitHubCommand.CreateRepoCommand());
        configCommand.AddCommand(ConfigGitHubCommand.CreateBranchCommand());
        configCommand.AddCommand(ConfigGitHubCommand.CreateImagePathCommand());
        configCommand.AddCommand(ConfigIgnoreCommand.CreateCommand());
        
        return configCommand;
    }

    private static void HandleListOption(bool listAll)
    {
        if (!listAll) return;
        
        var config = ConfigurationMgr.configuration;
        
        Console.WriteLine("Current Configuration:");
        Console.WriteLine("=====================");
        Console.WriteLine($"Obsidian Vault Path: {config.obsidianVaultPath ?? "Not set"}");
        Console.WriteLine($"Hexo Posts Path: {config.hexoPostsPath ?? "Not set"}");
        Console.WriteLine($"Ignored Paths: {(config.ignoresPaths.Any() ? string.Join(", ", config.ignoresPaths) : "None")}");
        Console.WriteLine();
        Console.WriteLine("GitHub Configuration:");
        Console.WriteLine($"  Repository Owner: {config.GitHub.RepoOwner ?? "Not set"}");
        Console.WriteLine($"  Repository Name: {config.GitHub.RepoName ?? "Not set"}");
        Console.WriteLine($"  Branch Name: {config.GitHub.BranchName}");
        Console.WriteLine($"  Image Base Path: {config.GitHub.ImageBasePath}");
        Console.WriteLine($"  Personal Access Token: {(string.IsNullOrEmpty(config.GitHub.PersonalAccessToken) ? "Not set" : "***")}");
    }
}
