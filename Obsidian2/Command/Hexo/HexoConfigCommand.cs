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
        
        configCommand.AddCommand(CreatePostsDirCommand());
        
        return configCommand;
    }

    private static void HandleListOption(bool listAll)
    {
        if (!listAll) return;
        
        var config = ConfigurationMgr.configuration;
        
        Console.WriteLine("Hexo Configuration:");
        Console.WriteLine("==================");
        Console.WriteLine($"Posts Directory: {config.hexoPostsPath ?? "Not set"}");
    }

    private static Command CreatePostsDirCommand()
    {
        var postsDirCmd = new Command("posts-dir", "Set Hexo posts directory");
        var hexoPostsDirArg = new Argument<DirectoryInfo>("directory", "Path to Hexo posts directory");
        postsDirCmd.AddArgument(hexoPostsDirArg);
        postsDirCmd.SetHandler(SetPostsDir, hexoPostsDirArg);
        return postsDirCmd;

        void SetPostsDir(DirectoryInfo hexoPostsDir)
        {
            Utils.CheckDirectory(hexoPostsDir, "Hexo posts directory");

            ConfigurationMgr.configuration.hexoPostsPath = hexoPostsDir.FullName;
            ConfigurationMgr.Save();
            Console.WriteLine("Hexo posts directory has been set.");
        }
    }
}
