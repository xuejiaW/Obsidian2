using System.Reflection;
using Newtonsoft.Json;

namespace Obsidian2;

public static class ConfigurationMgr
{
    private static string s_ConfigurationPath = null;
    public static Configuration configuration { get; private set; }

    static ConfigurationMgr()
    {
        if (!Directory.Exists(configurationPath))
            Directory.CreateDirectory(configurationPath);

        s_ConfigurationPath = Path.Join(configurationPath, "configuration.json");
        if (!File.Exists(s_ConfigurationPath)) CreateInitialConfiguration();

        configuration = Load();
    }

    public static Configuration Load()
    {
        return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(s_ConfigurationPath));
    }

    public static void Save()
    {
        string json = JsonConvert.SerializeObject(configuration);
        File.WriteAllText(s_ConfigurationPath, json);
    }

    public static void UpdateConfiguration(Configuration newConfiguration)
    {
        configuration = newConfiguration;
        Save();
    }

    private static void CreateInitialConfiguration()
    {
        using Stream stream = Assembly.GetExecutingAssembly()
                                      .GetManifestResourceStream("Obsidian2.Resources.configuration.json");
        using FileStream fileStream = File.Create(s_ConfigurationPath);
        stream.CopyTo(fileStream);
        Console.WriteLine(s_ConfigurationPath);
    }

    private static string configurationPath
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
}