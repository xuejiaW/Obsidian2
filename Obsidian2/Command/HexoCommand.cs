﻿using System.CommandLine;
using Obsidian2.ConsoleUI;

namespace Obsidian2;

internal static class HexoCommand
{
    internal static Command CreateCommand()
    {
        Configuration configuration = ConfigurationMgr.configuration;

        var hexoCommand = new Command("hexo", "Converts obsidian notes to hexo posts");
        var obsidianOption = new Option<DirectoryInfo>(name: "--obsidian-vault-dir",
                                                       description: "Path to the Obsidian vault directory",
                                                       getDefaultValue: () =>
                                                           new DirectoryInfo(configuration.obsidianVaultPath));

        var hexoOption = new Option<DirectoryInfo>(name: "--hexo-posts-dir",
                                                   description: "Path to the Hexo posts directory",
                                                   getDefaultValue: () =>
                                                       new DirectoryInfo(configuration.hexoPostsPath));

        hexoCommand.AddOption(obsidianOption);
        hexoCommand.AddOption(hexoOption);
        hexoCommand.SetHandler(ConvertObsidian2Hexo, obsidianOption, hexoOption);
        return hexoCommand;
    }


    private static void ConvertObsidian2Hexo(DirectoryInfo obsidianVaultDir, DirectoryInfo hexoPostsDir)
    {
        Console.WriteLine($"Obsidian vault path is {obsidianVaultDir.FullName}");
        Console.WriteLine($"Hexo posts path is {hexoPostsDir.FullName}");

        Utils.CheckDirectory(obsidianVaultDir, "Obsidian vault directory");
        Utils.CheckDirectory(hexoPostsDir, "Hexo posts directory");

        StopWatch.CreateStopWatch("Whole Operation", () =>
        {
            var obsidian2HexoHandler = new Obsidian2HexoHandler(obsidianVaultDir, hexoPostsDir);
            obsidian2HexoHandler.Process().Wait();
        });
    }
}