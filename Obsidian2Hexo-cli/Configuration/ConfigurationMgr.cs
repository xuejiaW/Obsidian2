using Newtonsoft.Json;

namespace Obsidian2Hexo;

public static class ConfigurationMgr
{
    private static string s_ConfigurationPath = null;

    static ConfigurationMgr()
    {
        string dir = Path.Join(Directory.GetCurrentDirectory(), "Resources\\");
        s_ConfigurationPath = Path.Join(dir, "configuration.json");
    }

    public static Configuration Load()
    {
        var configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(s_ConfigurationPath));

        return configuration;
    }

    public static void Save(Configuration configuration)
    {
        string json = JsonConvert.SerializeObject(configuration);
        File.WriteAllText(s_ConfigurationPath, json);
    }
}