using System.CommandLine;

namespace Obsidian2;

public static class HexoConfigCommand
{
    internal static Command CreateCommand()
    {
        var configCommand = new Command("config", "Manage Hexo command configuration");
        
        var listOption = new Option<bool>("--list", "List Hexo configuration settings");
        configCommand.AddOption(listOption);
        configCommand.SetHandler(HandleListOption, listOption);
        
        configCommand.AddCommand(HexoConfigPostsDirCommand.CreateCommand());
        
        return configCommand;
    }

    private static void HandleListOption(bool listAll)
    {
        if (!listAll) return;
        
        var config = ConfigurationMgr.GetCommandConfig<HexoConfig>();
        
        Console.WriteLine("Hexo Configuration:");
        Console.WriteLine("==================");
        Console.WriteLine($"Posts Directory: {config.postsPath ?? "Not set"}");
    }
}
