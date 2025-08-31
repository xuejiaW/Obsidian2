using System.CommandLine;
using Obsidian2.Utilities;

namespace Obsidian2;

public static class HexoPostsDirCommand
{
    internal static Command CreateCommand()
    {
        var postsDirCmd = new Command("posts-dir", "Set Hexo posts directory");
        var hexoPostsDirArg = new Argument<DirectoryInfo>("directory", "Path to Hexo posts directory");
        postsDirCmd.AddArgument(hexoPostsDirArg);
        postsDirCmd.SetHandler(SetPostsDir, hexoPostsDirArg);
        return postsDirCmd;

        void SetPostsDir(DirectoryInfo hexoPostsDir)
        {
            FileSystemUtils.CheckDirectory(hexoPostsDir, "Hexo posts directory");

            var config = ConfigurationMgr.GetCommandConfig<HexoConfig>();
            config.postsPath = hexoPostsDir.FullName;
            ConfigurationMgr.SaveCommandConfig(config);
            Console.WriteLine("Hexo posts directory has been set.");
        }
    }
}
