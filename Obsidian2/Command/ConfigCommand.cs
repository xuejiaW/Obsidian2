using System.CommandLine;
using System.Linq;

namespace Obsidian2;

public static class ConfigCommand
{
    internal static Command CreateCommand()
    {
        var configCommand = new Command("config", "Manage configuration settings");

        var listOption = new Option<bool>("--list", "List configuration settings");
        var allOption = new Option<bool>("--a", "Show all command configurations (use with --list)");

        configCommand.AddOption(listOption);
        configCommand.AddOption(allOption);

        configCommand.SetHandler(HandleListOption, listOption, allOption);

        configCommand.AddCommand(ConfigPathCommand.CreateObsidianVaultDirCommand());
        configCommand.AddCommand(ConfigIgnoreCommand.CreateCommand());
        configCommand.AddCommand(ConfigImportExportCommand.CreateExportCommand());
        configCommand.AddCommand(ConfigImportExportCommand.CreateImportCommand());

        return configCommand;
    }

    private static void HandleListOption(bool listAll, bool showAllCommands)
    {
        if (!listAll) return;

        var config = ConfigurationMgr.configuration;

        Console.WriteLine("Global Configuration:");
        Console.WriteLine("=====================");
        Console.WriteLine($"Obsidian Vault Path: {config.obsidianVaultPath ?? "Not set"}");
        Console.WriteLine($"Ignored Paths: {(config.ignoresPaths.Any() ? string.Join(", ", config.ignoresPaths) : "None")}");

        if (showAllCommands)
        {
            Console.WriteLine();
            Console.WriteLine("Command Configurations:");
            Console.WriteLine("=======================");

            foreach (var commandConfig in config.commandConfigs.Values)
            {
                Console.WriteLine($"\n--- {commandConfig.CommandName} ---");
                commandConfig.DisplayConfiguration();
            }
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("Note: Use 'obsidian2 config --list --a' to see all command configurations");
        }
    }
}
