using System.CommandLine;

namespace Obsidian2;

public static class ConfigPathCommand
{
    internal static Command CreateObsidianVaultDirCommand()
    {
        var setObsidianVaultDirCmd = new Command("obsidian-vault-dir", "Set Obsidian vault directory");
        var obsidianVaultDirArg = new Argument<DirectoryInfo>("directory", "Path to Obsidian vault directory");
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
