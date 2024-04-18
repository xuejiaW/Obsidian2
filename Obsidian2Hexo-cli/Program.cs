using System.CommandLine;
using System.Diagnostics;
using Obsidian2Hexo.ConsoleUI;

namespace Obsidian2Hexo
{
    internal static class Program
    {
        private static Option<DirectoryInfo> s_ObsidianOption = null;
        private static Option<DirectoryInfo> s_HexoOption = null;
        private static Configuration s_Configuration = null;

        private static async Task<int> Main(string[] args)
        {
            s_Configuration = ConfigurationMgr.Load();

            s_ObsidianOption = new Option<DirectoryInfo>(name: "--obsidian-vault-dir",
                                                         description: "Path to the Obsidian vault directory",
                                                         getDefaultValue: () =>
                                                             new DirectoryInfo(s_Configuration.obsidianVaultPath));

            s_HexoOption = new Option<DirectoryInfo>(name: "--hexo-posts-dir",
                                                     description: "Path to the Hexo posts directory",
                                                     getDefaultValue: () =>
                                                         new DirectoryInfo(s_Configuration.hexoPostsPath));

            var rootCommand = new RootCommand();
            rootCommand.AddOption(s_ObsidianOption);
            rootCommand.AddOption(s_HexoOption);

            rootCommand.AddCommand(CreateSetCommand());
            rootCommand.SetHandler(ConvertObsidian2Hexo, s_ObsidianOption, s_HexoOption);

            return await rootCommand.InvokeAsync(args);
        }

        private static Command CreateSetCommand()
        {
            var setCommand = new Command("set", "Converts obsidian notes to hexo posts");
            setCommand.AddOption(s_ObsidianOption);
            setCommand.AddOption(s_HexoOption);
            setCommand.SetHandler(SetConfiguration, s_ObsidianOption, s_HexoOption);
            return setCommand;
        }

        private static void SetConfiguration(DirectoryInfo obsidianVaultDir, DirectoryInfo hexoPostsDir)
        {
            CheckDirectory(obsidianVaultDir, "Obsidian vault directory");
            CheckDirectory(hexoPostsDir, "Hexo posts directory");

            s_Configuration.obsidianVaultPath = obsidianVaultDir.FullName;
            s_Configuration.hexoPostsPath = hexoPostsDir.FullName;
            ConfigurationMgr.Save(s_Configuration);
        }

        private static void ConvertObsidian2Hexo(DirectoryInfo obsidianVaultDir, DirectoryInfo hexoPostsDir)
        {
            Console.WriteLine($"Obsidian vault path is {obsidianVaultDir.FullName}");
            Console.WriteLine($"Hexo posts path is {hexoPostsDir.FullName}");

            CheckDirectory(obsidianVaultDir, "Obsidian vault directory");
            CheckDirectory(hexoPostsDir, "Hexo posts directory");

            StopWatch.CreateStopWatch("Whole Operation", () =>
            {
                var obsidian2HexoHandler = new Obsidian2HexoHandler(obsidianVaultDir, hexoPostsDir);
                obsidian2HexoHandler.Process().Wait();
            });
        }

        private static void CheckDirectory(DirectoryInfo directory, string description)
        {
            if (!directory.Exists)
                throw new ArgumentException($"The {description}: {directory.FullName} does not exist.");
        }
    }
}