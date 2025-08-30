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
        
        configCommand.AddCommand(ConfigPathCommand.CreateObsidianVaultDirCommand());
        configCommand.AddCommand(ConfigIgnoreCommand.CreateCommand());
        configCommand.AddCommand(ConfigImportExportCommand.CreateExportCommand());
        configCommand.AddCommand(ConfigImportExportCommand.CreateImportCommand());
        
        return configCommand;
    }

    private static void HandleListOption(bool listAll)
    {
        if (!listAll) return;
        
        var config = ConfigurationMgr.configuration;
        
        Console.WriteLine("Current Configuration:");
        Console.WriteLine("=====================");
        Console.WriteLine($"Obsidian Vault Path: {config.obsidianVaultPath ?? "Not set"}");
        Console.WriteLine($"Ignored Paths: {(config.ignoresPaths.Any() ? string.Join(", ", config.ignoresPaths) : "None")}");
        Console.WriteLine();
        Console.WriteLine("Note: Hexo posts directory configuration is now managed under 'hexo config posts-dir'");
        Console.WriteLine("Note: Assets repository configuration is now managed under 'compat config assets-repo'");
    }
}
