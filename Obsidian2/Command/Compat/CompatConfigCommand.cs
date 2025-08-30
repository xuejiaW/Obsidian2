using System.CommandLine;
using System.Linq;

namespace Obsidian2;

public static class CompatConfigCommand
{
    internal static Command CreateCommand()
    {
        var configCommand = new Command("config", "Manage compat command configuration");
        
        var listOption = new Option<bool>("--list", "List compat configuration settings");
        configCommand.AddOption(listOption);
        configCommand.SetHandler(HandleListOption, listOption);
        
        configCommand.AddCommand(CompatConfigAssetsRepoCommand.CreateCommand());
        
        return configCommand;
    }

    private static void HandleListOption(bool listAll)
    {
        if (!listAll) return;
        
        var config = ConfigurationMgr.configuration;
        
        Console.WriteLine("Compat Configuration:");
        Console.WriteLine("====================");
        Console.WriteLine("Assets Repository:");
        Console.WriteLine($"  Owner: {config.GitHub.RepoOwner ?? "Not set"}");
        Console.WriteLine($"  Name: {config.GitHub.RepoName ?? "Not set"}");
        Console.WriteLine($"  Branch: {config.GitHub.BranchName}");
        Console.WriteLine($"  Image Path: {config.GitHub.ImageBasePath}");
        Console.WriteLine($"  Access Token: {(string.IsNullOrEmpty(config.GitHub.PersonalAccessToken) ? "Not set" : "***")}");
    }
}
