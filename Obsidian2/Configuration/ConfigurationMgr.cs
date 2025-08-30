using System.Reflection;
using Newtonsoft.Json;

namespace Obsidian2;

public static class ConfigurationMgr
{
    private static string s_ConfigurationPath = null;

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

    public static Configuration Load()
    {
        return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configurationPath));
    }

    public static void Save()
    {
        string json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
        File.WriteAllText(configurationPath, json);
    }

    public static void UpdateConfiguration(Configuration newConfiguration)
    {
        configuration = newConfiguration;
        Save();
    }
}