using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Obsidian2;

public static class ConfigImportExportCommand
{
    internal static Command CreateExportCommand()
    {
        var exportCommand = new Command("export", "Export current configuration to a JSON file");
        var filenameArg = new Argument<string>("filename", () => null, "Output filename (defaults to obsidian2-config.json)");
        var outputDirOption = new Option<string>("--output-dir", () => null, "Output directory (defaults to current directory)");
        
        exportCommand.AddArgument(filenameArg);
        exportCommand.AddOption(outputDirOption);
        exportCommand.SetHandler(ExportConfiguration, filenameArg, outputDirOption);
        
        return exportCommand;

        void ExportConfiguration(string filename, string outputDir)
        {
            try
            {
                if (string.IsNullOrEmpty(filename))
                {
                    filename = "obsidian2-config.json";
                }

                string outputPath;
                if (!string.IsNullOrEmpty(outputDir))
                {
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    outputPath = Path.Combine(outputDir, filename);
                }
                else
                {
                    outputPath = Path.Combine(Directory.GetCurrentDirectory(), filename);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var configJson = JsonSerializer.Serialize(ConfigurationMgr.configuration, options);
                
                File.WriteAllText(outputPath, configJson);
                
                Console.WriteLine($"Configuration exported successfully to: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to export configuration: {ex.Message}");
            }
        }
    }

    internal static Command CreateImportCommand()
    {
        var importCommand = new Command("import", "Import configuration from a JSON file");
        var filenameArg = new Argument<string>("filename", "JSON configuration file to import");
        var backupOption = new Option<bool>("--backup", () => true, "Create backup of current configuration before importing");
        var forceOption = new Option<bool>("--force", () => false, "Force import without confirmation prompt");
        
        importCommand.AddArgument(filenameArg);
        importCommand.AddOption(backupOption);
        importCommand.AddOption(forceOption);
        importCommand.SetHandler(ImportConfiguration, filenameArg, backupOption, forceOption);
        
        return importCommand;

        void ImportConfiguration(string filename, bool createBackup, bool force)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    Console.WriteLine($"Configuration file not found: {filename}");
                    return;
                }

                if (!force)
                {
                    Console.WriteLine("This will replace your current configuration.");
                    Console.Write("Are you sure you want to continue? (y/N): ");
                    var response = Console.ReadLine()?.Trim().ToLower();
                    if (response != "y" && response != "yes")
                    {
                        Console.WriteLine("Import cancelled.");
                        return;
                    }
                }

                if (createBackup)
                {
                    var backupFilename = $"obsidian2-config-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };
                    var backupJson = JsonSerializer.Serialize(ConfigurationMgr.configuration, options);
                    File.WriteAllText(backupFilename, backupJson);
                    Console.WriteLine($"Current configuration backed up to: {backupFilename}");
                }

                var configJson = File.ReadAllText(filename);
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var importedConfig = JsonSerializer.Deserialize<Configuration>(configJson, deserializeOptions);
                
                if (importedConfig == null)
                {
                    Console.WriteLine("Failed to parse configuration file. Invalid JSON format.");
                    return;
                }

                ConfigurationMgr.UpdateConfiguration(importedConfig);

                Console.WriteLine($"Configuration imported successfully from: {filename}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse JSON configuration file: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to import configuration: {ex.Message}");
            }
        }
    }
}
