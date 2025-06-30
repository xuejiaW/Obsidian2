using System.CommandLine;
using Obsidian2.ConsoleUI;

namespace Obsidian2;

internal static class ZhihuCommand
{
    internal static Command CreateCommand()
    {
        Configuration configuration = ConfigurationMgr.configuration;

        var zhihuCommand = new Command("zhihu", "Converts mathematical formulas in markdown files for Zhihu platform");

        var inputOption = new Option<FileInfo>(name: "--input", description: "Path to the markdown file to convert");

        var outputDirOption = new Option<DirectoryInfo>(name: "--output-dir",
                                                   description: "Directory to save the converted file",
                                                   getDefaultValue: () =>
                                                       new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "zhihu_output")));

        zhihuCommand.AddOption(inputOption);
        zhihuCommand.AddOption(outputDirOption);

        zhihuCommand.SetHandler(ConvertMarkdownForZhihu, inputOption, outputDirOption);
        return zhihuCommand;
    }

    private static void ConvertMarkdownForZhihu(FileInfo inputFile, DirectoryInfo outputDir)
    {
        if (inputFile == null)
        {
            Console.WriteLine("Error: You must specify --input");
            return;
        }

        ProcessSingleFile(inputFile, outputDir);
    }

    private static void ProcessSingleFile(FileInfo inputFile, DirectoryInfo outputDir)
    {
        Console.WriteLine($"Processing file: {inputFile.FullName}");

        if (!inputFile.Exists)
        {
            Console.WriteLine($"Error: Input file {inputFile.FullName} does not exist.");
            return;
        }

        if (!outputDir.Exists)
        {
            outputDir.Create();
        }

        StopWatch.CreateStopWatch("Zhihu Formula Conversion", () =>
        {
            var formatter = new ZhihuPostFormatter(inputFile, outputDir);
            formatter.Format().Wait();
        });
    }
}
