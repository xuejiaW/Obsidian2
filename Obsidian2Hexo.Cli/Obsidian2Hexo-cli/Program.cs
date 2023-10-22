using System.CommandLine;

namespace Obsidian2Hexo
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var obsidianOption
                = new Option<DirectoryInfo>("--obsidian-vault-dir", "Path to the Obsidian vault directory");
            var hexoOption = new Option<DirectoryInfo>("--hexo-posts-dir", "Path to the Hexo posts directory");

            var rootCommand = new RootCommand("Tools converts obsidian notes to hexo posts");
            rootCommand.AddOption(obsidianOption);
            rootCommand.AddOption(hexoOption);

            rootCommand.SetHandler((obsidianVaultDir, hexoPostsDir) =>
            {
                Run(obsidianVaultDir!, hexoPostsDir!);
            }, obsidianOption, hexoOption);

            return await rootCommand.InvokeAsync(args);
        }

        private static void Run(DirectoryInfo obsidianVaultDir, DirectoryInfo hexoPostsDir)
        {
            Console.WriteLine($"Hexo posts directory: {hexoPostsDir.FullName}");
            Console.WriteLine($"Obsidian vault directory: {obsidianVaultDir.FullName}");

            if (!obsidianVaultDir.Exists) throw new ArgumentException("The Obsidian directory does not exist.");
            if (!hexoPostsDir.Exists) throw new ArgumentException("The Hexo posts directory does not exist.");

            var obsidian2HexoHandler = new Obsidian2HexoHandler(obsidianVaultDir, hexoPostsDir);
            obsidian2HexoHandler.Process();
        }
    }
}