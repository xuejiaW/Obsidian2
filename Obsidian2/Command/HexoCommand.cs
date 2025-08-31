using System.CommandLine;
using Obsidian2.ConsoleUI;
using Obsidian2.Utilities;

namespace Obsidian2;

internal static class HexoCommand
{
    static HexoCommand()
    {
        ConfigurationMgr.RegisterCommandConfig<HexoConfig>();
    }

    internal static Command CreateCommand()
    {
        var hexoCommand = new Command("hexo", "Converts obsidian notes to hexo posts");
        var obsidianOption = new Option<DirectoryInfo>(name: "--obsidian-vault-dir", description: "Path to the Obsidian vault directory",
                                                       getDefaultValue: () => new DirectoryInfo(ConfigurationMgr.configuration.obsidianVaultPath ?? ""));

        var hexoOption = new Option<DirectoryInfo>(name: "--hexo-posts-dir", description: "Path to the Hexo posts directory", getDefaultValue: () =>
        {
            try
            {
                var hexoConfig = ConfigurationMgr.GetCommandConfig<HexoConfig>();
                return new DirectoryInfo(hexoConfig.postsPath ?? ".");
            }
            catch
            {
                return new DirectoryInfo(".");
            }
        });

        hexoCommand.AddOption(obsidianOption);
        hexoCommand.AddOption(hexoOption);

        hexoCommand.AddCommand(HexoConfigCommand.CreateCommand());

        hexoCommand.SetHandler(ConvertObsidian2Hexo, obsidianOption, hexoOption);
        return hexoCommand;
    }


    private static void ConvertObsidian2Hexo(DirectoryInfo obsidianVaultDir, DirectoryInfo hexoPostsDir)
    {
        Console.WriteLine($"Obsidian vault path is {obsidianVaultDir.FullName}");
        Console.WriteLine($"Hexo posts path is {hexoPostsDir.FullName}");

        FileSystemUtils.CheckDirectory(obsidianVaultDir, "Obsidian vault directory");
        FileSystemUtils.CheckDirectory(hexoPostsDir, "Hexo posts directory");

        StopWatch.CreateStopWatch("Whole Operation", () =>
        {
            var obsidian2HexoHandler = new Obsidian2HexoHandler(obsidianVaultDir, hexoPostsDir);
            obsidian2HexoHandler.Process().Wait();
        });
    }
}