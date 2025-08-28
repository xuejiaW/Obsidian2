using System.CommandLine;

namespace Obsidian2;

public static class ConfigPathCommand
{
    internal static Command CreateHexoPostsDirCommand()
    {
        var setHexoPostsDirCmd = new Command("hexo-posts-dir", "Set Hexo posts directory");
        var hexoPostsDirArg = new Argument<DirectoryInfo>();
        setHexoPostsDirCmd.AddArgument(hexoPostsDirArg);
        setHexoPostsDirCmd.SetHandler(SetHexoPostsDir, hexoPostsDirArg);
        return setHexoPostsDirCmd;

        void SetHexoPostsDir(DirectoryInfo hexoPostsDir)
        {
            Utils.CheckDirectory(hexoPostsDir, "Hexo posts directory");

            ConfigurationMgr.configuration.hexoPostsPath = hexoPostsDir.FullName;
            ConfigurationMgr.Save();
            Console.WriteLine("Hexo posts directory has been set.");
        }
    }

    internal static Command CreateObsidianVaultDirCommand()
    {
        var setObsidianVaultDirCmd = new Command("obsidian-vault-dir", "Set Obsidian vault directory");
        var obsidianVaultDirArg = new Argument<DirectoryInfo>();
        setObsidianVaultDirCmd.AddArgument(obsidianVaultDirArg);
        setObsidianVaultDirCmd.SetHandler(SetObsidianVaultDir, obsidianVaultDirArg);
        return setObsidianVaultDirCmd;

        void SetObsidianVaultDir(DirectoryInfo obsidianVaultDir)
        {
            Utils.CheckDirectory(obsidianVaultDir, "Obsidian vault directory");
            ConfigurationMgr.configuration.obsidianVaultPath = obsidianVaultDir.FullName;
            ConfigurationMgr.Save();
            Console.WriteLine("Obsidian vault directory has been set.");
        }
    }
}
