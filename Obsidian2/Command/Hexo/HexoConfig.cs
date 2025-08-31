using System.Text.Json.Serialization;

namespace Obsidian2;

public class HexoConfig : ICommandConfig
{
    [JsonIgnore]
    public string CommandName => "hexo";
    
    [JsonPropertyName("postsPath")]
    public string postsPath { get; set; }

    public void SetDefaults()
    {
        postsPath ??= "";
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(postsPath))
        {
            errors.Add("Posts path is required for hexo command");
            return false;
        }

        if (!Directory.Exists(postsPath))
        {
            errors.Add($"Posts directory does not exist: {postsPath}");
            return false;
        }

        return true;
    }
}
