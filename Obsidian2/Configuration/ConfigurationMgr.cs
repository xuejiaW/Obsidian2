using Newtonsoft.Json;

namespace Obsidian2;

public static class ConfigurationMgr
{
    private static string s_ConfigurationPath = null;

    public static Configuration configuration { get; private set; }

    static ConfigurationMgr()
    {
        string dir = Path.Join(Directory.GetCurrentDirectory(), "Resources\\");
        s_ConfigurationPath = Path.Join(dir, "configuration.json");
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
}