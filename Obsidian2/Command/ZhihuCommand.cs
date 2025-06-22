using System.CommandLine;
using System.CommandLine.Parsing;
using Obsidian2.ConsoleUI;

namespace Obsidian2;

internal static class ZhihuCommand
{
    internal static Command CreateCommand()
    {
        Configuration configuration = ConfigurationMgr.configuration;

        var zhihuCommand = new Command("zhihu", "Converts mathematical formulas in markdown files for Zhihu platform");
        
        // 单文件处理选项
        var inputOption = new Option<FileInfo>(name: "--input",
                                             description: "Path to the markdown file to convert");
        
        // 输出目录选项
        var outputDirOption = new Option<DirectoryInfo>(name: "--output-dir",
                                                   description: "Directory to save the converted file",
                                                   getDefaultValue: () =>
                                                       new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "zhihu_output")));
        
        // 指定输出文件选项
        var outputFileOption = new Option<FileInfo>(name: "--output-file",
                                                  description: "Specific output markdown file path. If specified, this overrides the default naming.");
        
        // 文件夹批处理选项
        var inputDirOption = new Option<DirectoryInfo>(name: "--input-dir", 
                                                    description: "Directory containing markdown files to convert");
        
        // 递归处理子文件夹
        var recursiveOption = new Option<bool>(name: "--recursive",
                                            description: "Process markdown files in subdirectories recursively",
                                            getDefaultValue: () => false);

        zhihuCommand.AddOption(inputOption);
        zhihuCommand.AddOption(inputDirOption);
        zhihuCommand.AddOption(outputDirOption);
        zhihuCommand.AddOption(outputFileOption);
        zhihuCommand.AddOption(recursiveOption);

        zhihuCommand.SetHandler((FileInfo input, DirectoryInfo inputDir, DirectoryInfo outputDir, FileInfo outputFile) => {
            // 设置默认值
            bool recursive = false;
            
            // 命令行参数中指定的值会覆盖默认值
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg == "--recursive") recursive = true;
            }
            
            ConvertMarkdownForZhihu(input, inputDir, outputDir, outputFile, recursive);
        }, inputOption, inputDirOption, outputDirOption, outputFileOption);
                
        return zhihuCommand;
    }

    private static void ConvertMarkdownForZhihu(
        FileInfo inputFile, 
        DirectoryInfo inputDir, 
        DirectoryInfo outputDir, 
        FileInfo outputFile, 
        bool recursive)
    {
        // 验证输入参数
        if (inputFile == null && inputDir == null)
        {
            Console.WriteLine("Error: You must specify either --input or --input-dir");
            return;
        }

        if (inputFile != null && inputDir != null)
        {
            Console.WriteLine("Warning: Both --input and --input-dir are specified. Using --input file only.");
        }

        // 处理单个文件
        if (inputFile != null)
        {
            ProcessSingleFile(inputFile, outputDir, outputFile);
            return;
        }

        // 处理文件夹中的所有 Markdown 文件
        if (inputDir != null)
        {
            ProcessDirectory(inputDir, outputDir, recursive);
        }
    }

    private static void ProcessSingleFile(
        FileInfo inputFile, 
        DirectoryInfo outputDir, 
        FileInfo outputFile)
    {
        Console.WriteLine($"Processing file: {inputFile.FullName}");
        
        if (!inputFile.Exists)
        {
            Console.WriteLine($"Error: Input file {inputFile.FullName} does not exist.");
            return;
        }

        // 创建输出目录如果不存在
        if (!outputDir.Exists)
        {
            outputDir.Create();
        }

        StopWatch.CreateStopWatch("Zhihu Formula Conversion", () =>
        {
            var formatter = new ZhihuPostFormatter(inputFile, outputDir);
            
            // 如果指定了输出文件，设置特定输出路径
            if (outputFile != null)
            {
                // 确保输出文件的目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile.FullName));
                formatter.SetOutputFilePath(outputFile.FullName);
            }
            
            formatter.Format().Wait();
        });
    }

    private static void ProcessDirectory(
        DirectoryInfo inputDir, 
        DirectoryInfo outputDir, 
        bool recursive)
    {
        if (!inputDir.Exists)
        {
            Console.WriteLine($"Error: Input directory {inputDir.FullName} does not exist.");
            return;
        }

        // 创建输出目录如果不存在
        if (!outputDir.Exists)
        {
            outputDir.Create();
        }

        Console.WriteLine($"Processing directory: {inputDir.FullName}");
        Console.WriteLine($"Output directory: {outputDir.FullName}");
        Console.WriteLine($"Recursive mode: {recursive}");

        // 获取所有markdown文件
        SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        FileInfo[] markdownFiles = inputDir.GetFiles("*.md", searchOption);

        if (markdownFiles.Length == 0)
        {
            Console.WriteLine("No markdown files found in the specified directory.");
            return;
        }

        Console.WriteLine($"Found {markdownFiles.Length} markdown files to process.");

        // 使用 Progress 来显示处理进度
        Progress.CreateProgress("Converting Markdown Files for Zhihu", async progressTask =>
        {
            progressTask.MaxValue = markdownFiles.Length;

            foreach (FileInfo file in markdownFiles)
            {
                try
                {
                    // 维持目录结构
                    string relativePath = Path.GetRelativePath(inputDir.FullName, file.DirectoryName);
                    DirectoryInfo specificOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, relativePath));
                    
                    if (!specificOutputDir.Exists)
                    {
                        specificOutputDir.Create();
                    }

                    // 处理单个文件
                    var formatter = new ZhihuPostFormatter(file, specificOutputDir);
                    await formatter.Format();
                    
                    Console.WriteLine($"Processed: {file.FullName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {file.FullName}: {ex.Message}");
                }
                finally
                {
                    progressTask.Increment(1);
                }
            }
        }).Wait(); // 添加 Wait() 确保处理完成
    }
}
