using System.Reflection;
using System.Text.Json;

namespace Obsidian2;

public static class ConfigurationMgr
{
    private static string s_ConfigurationPath = null;
    private static readonly Dictionary<string, Type> RegisteredTypes = new();

    private static string configurationPath => s_ConfigurationPath ??= Path.Join(configurationFolder, "configuration.json");

    private static string configurationFolder
    {
        get
        {
            string folder = Environment.OSVersion.Platform == PlatformID.Unix
                ? Path.Combine(GetEnvironmentVariable("HOME"), ".config", "Obsidian2")
                : Path.Combine(GetEnvironmentVariable("USERPROFILE"), "APPData", "Local", "Obsidian2");
            return folder;

            string GetEnvironmentVariable(string key)
            {
                return Environment.GetEnvironmentVariable(key) ?? string.Empty;
            }
        }
    }

    public static Configuration configuration { get; private set; }

    static ConfigurationMgr()
    {
        // Explicitly register configuration types to ensure they are available during deserialization
        RegisterCommandConfig<HexoConfig>();
        RegisterCommandConfig<CompatConfig>();
        
        if (!Directory.Exists(configurationFolder))
            Directory.CreateDirectory(configurationFolder);

        if (!File.Exists(configurationPath)) CreateInitialConfiguration();

        configuration = Load();

        void CreateInitialConfiguration()
        {
            using Stream stream = Assembly.GetExecutingAssembly()
                                          .GetManifestResourceStream("Obsidian2.Resources.configuration.json");
            using FileStream fileStream = File.Create(configurationPath);
            stream.CopyTo(fileStream);
            Console.WriteLine(configurationPath);
        }
    }

    public static void RegisterCommandConfig<T>() where T : class, ICommandConfig, new()
    {
        var instance = new T();
        RegisteredTypes[instance.CommandName] = typeof(T);
    }

    public static Configuration Load()
    {
        try
        {
            var jsonString = File.ReadAllText(configurationPath);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            var config = new Configuration();

            // Load basic properties
            if (root.TryGetProperty("obsidianVaultPath", out var vaultPath))
                config.obsidianVaultPath = vaultPath.GetString();

            if (root.TryGetProperty("ignoresPaths", out var ignorePaths))
            {
                config.ignoresPaths = ignorePaths.Deserialize<HashSet<string>>(jsonOptions) ?? new HashSet<string>();
            }

            // Handle migration from old format
            bool needsMigration = false;

            // Check for old hexoPostsPath
            if (root.TryGetProperty("hexoPostsPath", out var hexoPostsPath))
            {
                var hexoConfig = new HexoConfig();
                hexoConfig.postsPath = hexoPostsPath.GetString();
                hexoConfig.SetDefaults();
                config.commandConfigs["hexo"] = hexoConfig;
                needsMigration = true;
            }

            // Check for old GitHub config
            if (root.TryGetProperty("GitHub", out var githubElement))
            {
                var compatConfig = new CompatConfig();
                compatConfig.AssetsRepo = JsonSerializer.Deserialize<AssetsRepoConfig>(githubElement.GetRawText(), jsonOptions) ?? new AssetsRepoConfig();
                compatConfig.SetDefaults();
                config.commandConfigs["compat"] = compatConfig;
                needsMigration = true;
            }

            // Load new-style command configurations (will override old ones if present)
            if (root.TryGetProperty("commandConfigs", out var commandConfigs))
            {
                foreach (var commandProp in commandConfigs.EnumerateObject())
                {
                    var commandName = commandProp.Name;
                    var commandJson = commandProp.Value;

                    // Check if this command config has type information
                    if (commandJson.TryGetProperty("$type", out var typeElement))
                    {
                        var typeName = typeElement.GetString();
                        if (RegisteredTypes.Values.Any(t => t.Name == typeName))
                        {
                            var configType = RegisteredTypes.Values.First(t => t.Name == typeName);
                            
                            // Create a new JSON object without the $type property
                            var configData = new Dictionary<string, object>();
                            foreach (var prop in commandJson.EnumerateObject())
                            {
                                if (prop.Name != "$type")
                                {
                                    configData[prop.Name] = prop.Value;
                                }
                            }
                            
                            var cleanJsonText = JsonSerializer.Serialize(configData, jsonOptions);
                            var commandConfig = (ICommandConfig)JsonSerializer.Deserialize(cleanJsonText, configType, jsonOptions);
                            if (commandConfig != null)
                            {
                                commandConfig.SetDefaults();
                                config.commandConfigs[commandName] = commandConfig;
                            }
                        }
                    }
                    else if (RegisteredTypes.TryGetValue(commandName, out var registeredType))
                    {
                        // Fallback: use registered type for this command name
                        var commandConfig = (ICommandConfig)JsonSerializer.Deserialize(commandJson.GetRawText(), registeredType, jsonOptions);
                        if (commandConfig != null)
                        {
                            commandConfig.SetDefaults();
                            config.commandConfigs[commandName] = commandConfig;
                        }
                    }
                }
            }

            // If we migrated from old format, save the new format
            if (needsMigration)
            {
                Console.WriteLine("Migrating configuration to new format...");
                var oldConfig = configuration;
                configuration = config;
                Save();
                configuration = oldConfig; // Restore until we finish loading
            }

            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            return new Configuration();
        }
    }

    public static void Save()
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            // Build the command configs with type information
            var commandConfigsWithTypes = new Dictionary<string, object>();
            foreach (var kvp in configuration.commandConfigs)
            {
                var configType = kvp.Value.GetType();
                var configData = JsonSerializer.SerializeToElement(kvp.Value, configType, jsonOptions);
                
                // Create a new object with $type included
                var configWithType = new Dictionary<string, object>();
                configWithType["$type"] = configType.Name;
                
                // Add all properties from the original config
                foreach (var prop in configData.EnumerateObject())
                {
                    configWithType[prop.Name] = prop.Value;
                }
                
                commandConfigsWithTypes[kvp.Key] = configWithType;
            }

            var configToSave = new
            {
                obsidianVaultPath = configuration.obsidianVaultPath,
                ignoresPaths = configuration.ignoresPaths,
                commandConfigs = commandConfigsWithTypes
            };

            var json = JsonSerializer.Serialize(configToSave, jsonOptions);
            File.WriteAllText(configurationPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving configuration: {ex.Message}");
        }
    }

    public static void UpdateConfiguration(Configuration newConfiguration)
    {
        configuration = newConfiguration;
        Save();
    }

    // Helper methods for backward compatibility and easy access
    public static T GetCommandConfig<T>() where T : class, ICommandConfig, new()
    {
        return configuration.GetCommandConfig<T>();
    }

    public static void SaveCommandConfig<T>(T config) where T : class, ICommandConfig
    {
        configuration.AddCommandConfig(config);
        Save();
    }
}