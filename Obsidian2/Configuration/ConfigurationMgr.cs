using System.Reflection;
using System.Text.Json;

namespace Obsidian2;

public static class ConfigurationMgr
{
    private static readonly Dictionary<string, Type> RegisteredTypes = new();

    private static string s_ConfigurationPath = null;

    private static string configurationPath => s_ConfigurationPath ??= Path.Join(configurationFolder, "configuration.json");

    private static string configurationFolder
    {
        get
        {
            var homeVar = Environment.OSVersion.Platform == PlatformID.Unix ? "HOME" : "USERPROFILE";
            var baseFolder = Environment.OSVersion.Platform == PlatformID.Unix ? ".config" : "AppData/Local";
            return Path.Combine(Environment.GetEnvironmentVariable(homeVar) ?? "", baseFolder, "Obsidian2");
        }
    }

    private static Configuration s_Configuration;

    public static Configuration configuration
    {
        get
        {
            if (s_Configuration == null)
            {
                if (!Directory.Exists(configurationFolder))
                    Directory.CreateDirectory(configurationFolder);

                if (!File.Exists(configurationPath))
                {
                    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Obsidian2.Resources.configuration.json");
                    using var fileStream = File.Create(configurationPath);
                    stream?.CopyTo(fileStream);
                    Console.WriteLine($"Created initial configuration at: {configurationPath}");

                }

                s_Configuration = Load();
            }
            return s_Configuration;
        }
        private set => s_Configuration = value;

    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

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

            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            var config = new Configuration();

            if (root.TryGetProperty("obsidianVaultPath", out var vaultPath))
                config.obsidianVaultPath = vaultPath.GetString();

            if (root.TryGetProperty("ignoresPaths", out var ignorePaths))
            {
                config.ignoresPaths = ignorePaths.Deserialize<HashSet<string>>(JsonOptions) ?? new HashSet<string>();
            }

            // Load command configurations
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
                                if (prop.Name != "$type") configData[prop.Name] = prop.Value;
                            }

                            var cleanJsonText = JsonSerializer.Serialize(configData, JsonOptions);
                            var commandConfig = (ICommandConfig)JsonSerializer.Deserialize(cleanJsonText, configType, JsonOptions);
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
                        var commandConfig = (ICommandConfig)JsonSerializer.Deserialize(commandJson.GetRawText(), registeredType, JsonOptions);
                        if (commandConfig != null)
                        {
                            commandConfig.SetDefaults();
                            config.commandConfigs[commandName] = commandConfig;
                        }
                    }
                }
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
            var commandConfigsWithTypes = new Dictionary<string, object>();
            foreach (var kvp in configuration.commandConfigs)
            {
                var configType = kvp.Value.GetType();
                var configData = JsonSerializer.SerializeToElement(kvp.Value, configType, JsonOptions);

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

            var json = JsonSerializer.Serialize(configToSave, JsonOptions);
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