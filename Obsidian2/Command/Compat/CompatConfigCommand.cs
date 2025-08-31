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
        
        var config = ConfigurationMgr.GetCommandConfig<CompatConfig>();
        
        Console.WriteLine("Compat Configuration:");
        Console.WriteLine("====================");
        Console.WriteLine("Assets Repository:");
        Console.WriteLine($"  Owner: {config.AssetsRepo.repoOwner ?? "Not set"}");
        Console.WriteLine($"  Name: {config.AssetsRepo.repoName ?? "Not set"}");
        Console.WriteLine($"  Branch: {config.AssetsRepo.branchName}");
        Console.WriteLine($"  Image Path: {config.AssetsRepo.imageFolderPath}");
        Console.WriteLine($"  Access Token: {(string.IsNullOrEmpty(config.AssetsRepo.personalAccessToken) ? "Not set" : "***")}");
    }
}
