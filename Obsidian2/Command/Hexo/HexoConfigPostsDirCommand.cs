using System.CommandLine;

namespace Obsidian2;

public static class HexoConfigPostsDirCommand
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
            Utils.CheckDirectory(hexoPostsDir, "Hexo posts directory");

            ConfigurationMgr.configuration.hexoPostsPath = hexoPostsDir.FullName;
            ConfigurationMgr.Save();
            Console.WriteLine("Hexo posts directory has been set.");
        }
    }
}
