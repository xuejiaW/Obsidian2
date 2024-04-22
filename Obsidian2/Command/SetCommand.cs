using System.CommandLine;

namespace Obsidian2;

public static class SetCommand
{
    internal static Command CreateCommand()
    {
        Configuration configuration = ConfigurationMgr.configuration;

        var setCommand = new Command("set", "Set Configuration");
        var obsidianOption = new Option<DirectoryInfo>(name: "--obsidian-vault-dir",
                                                       description: "Path to the Obsidian vault directory",
                                                       getDefaultValue: () =>
                                                           new DirectoryInfo(configuration.obsidianVaultPath));

        var hexoOption = new Option<DirectoryInfo>(name: "--hexo-posts-dir",
                                                   description: "Path to the Hexo posts directory",
                                                   getDefaultValue: () =>
                                                       new DirectoryInfo(configuration.hexoPostsPath));

        setCommand.AddOption(obsidianOption);
        setCommand.AddOption(hexoOption);
        setCommand.SetHandler(SetHexoPostsDir, hexoOption);
        setCommand.SetHandler(SetObsidianVaultDir, obsidianOption);
        return setCommand;
    }

    private static void SetHexoPostsDir(DirectoryInfo hexoPostsDir)
    {
        Utils.CheckDirectory(hexoPostsDir, "Hexo posts directory");

        ConfigurationMgr.configuration.hexoPostsPath = hexoPostsDir.FullName;
        ConfigurationMgr.Save();
    }

    private static void SetObsidianVaultDir(DirectoryInfo obsidianVaultDir)
    {
        Utils.CheckDirectory(obsidianVaultDir, "Obsidian vault directory");
        ConfigurationMgr.configuration.obsidianVaultPath = obsidianVaultDir.FullName;
        ConfigurationMgr.Save();
    }
}