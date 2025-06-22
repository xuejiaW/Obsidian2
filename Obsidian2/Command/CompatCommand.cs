using System.CommandLine;
using Obsidian2.ConsoleUI;

namespace Obsidian2;

internal static class CompatCommand
{
    internal static Command CreateCommand()
    {
        var compatCommand
            = new Command("compat", "Converts markdown files to compatible format");

        var inputOption = new Option<FileInfo>(name: "--input", description: "Path to the markdown file to convert");

        var outputDirOption = new Option<DirectoryInfo>(name: "--output-dir",
                                                        description: "Directory to save the converted file and assets",
                                                        getDefaultValue: () => new DirectoryInfo(Path
                                                                .Combine(Directory.GetCurrentDirectory(),
                                                                         "compat_output")));

        var compressOption = new Option<bool>(name: "--compress",
                                              description: "Compress images larger than 500KB",
                                              getDefaultValue: () => true);

        var convertSvgOption = new Option<bool>(name: "--convert-svg",
                                                description:
                                                "Convert SVG images to PNG format (automatically enabled when uploading to GitHub)",
                                                getDefaultValue: () => true);


        compatCommand.AddOption(inputOption);
        compatCommand.AddOption(outputDirOption);
        compatCommand.AddOption(compressOption);
        compatCommand.AddOption(convertSvgOption);

        compatCommand.SetHandler(ConvertMarkdownToCompat, inputOption,
                                 outputDirOption, compressOption, convertSvgOption);

        return compatCommand;
    }

    private static void ConvertMarkdownToCompat(FileInfo inputFile, DirectoryInfo outputDir, bool compress,
                                                bool convertSvg)
    {
        if (inputFile == null) throw new ArgumentNullException(nameof(inputFile));
        if (!inputFile.Exists) throw new ArgumentNullException($"Error: Input file {inputFile} does not exist.");

        Console.WriteLine($"Processing file: {inputFile.FullName}");

        StopWatch.CreateStopWatch("Compatibility Conversion", () =>
        {
            var formatter = new CompatPostFormatter(inputFile, outputDir, compress);
            formatter.Format().Wait();
        });
    }
}