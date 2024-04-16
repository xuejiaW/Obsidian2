using System.CommandLine;

namespace Obsidian2Hexo
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Configuration configuration = ConfigurationMgr.Load();

            var obsidianOption = new Option<DirectoryInfo>(name: "--obsidian-vault-dir",
                                                           description: "Path to the Obsidian vault directory",
                                                           getDefaultValue: () =>
                                                               new DirectoryInfo(configuration.obsidianVaultPath));

            var hexoOption = new Option<DirectoryInfo>(name: "--hexo-posts-dir",
                                                       description: "Path to the Hexo posts directory",
                                                       getDefaultValue: () =>
                                                           new DirectoryInfo(configuration.hexoPostsPath));


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
            Console.WriteLine($"Obsidian vault path is {obsidianVaultDir.FullName}");
            Console.WriteLine($"Hexo posts path is {hexoPostsDir.FullName}");

            if (!obsidianVaultDir.Exists) throw new ArgumentException("The Obsidian directory does not exist.");
            if (!hexoPostsDir.Exists) throw new ArgumentException("The Hexo posts directory does not exist.");

            var obsidian2HexoHandler = new Obsidian2HexoHandler(obsidianVaultDir, hexoPostsDir);
            obsidian2HexoHandler.Process();
        }
    }
}