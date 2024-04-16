using Microsoft.Extensions.Configuration;

namespace Obsidian2Hexo;

public static class ConfigurationMgr
{
    public static Configuration Load()
    {
        string configurationPath = Path.Join(Directory.GetCurrentDirectory(), "Resources\\");
        IConfigurationRoot configurationBuilder = new ConfigurationBuilder()
                                                 .SetBasePath(configurationPath)
                                                 .AddJsonFile("configuration.json")
                                                 .Build();

        var configuration = new Configuration();
        configurationBuilder.Bind(configuration);

        return configuration;
    }
}