using System.Text.Json.Serialization;

namespace Obsidian2;

public class CompatConfig : ICommandConfig
{
    [JsonIgnore]
    public string CommandName => "compat";
    
    [JsonPropertyName("assetsRepo")]
    public AssetsRepoConfig AssetsRepo { get; set; } = new AssetsRepoConfig();

    public void SetDefaults()
    {
        AssetsRepo ??= new AssetsRepoConfig();
        AssetsRepo.branchName ??= "main";
        AssetsRepo.imageFolderPath ??= "images";
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(AssetsRepo.repoOwner))
            errors.Add("Repository owner is required");
        if (string.IsNullOrWhiteSpace(AssetsRepo.repoName))
            errors.Add("Repository name is required");

        return errors.Count == 0;
    }

    public void DisplayConfiguration()
    {
        Console.WriteLine("Compat Configuration:");
        Console.WriteLine("====================");
        Console.WriteLine("Assets Repository:");
        Console.WriteLine($"  Owner: {AssetsRepo.repoOwner ?? "Not set"}");
        Console.WriteLine($"  Name: {AssetsRepo.repoName ?? "Not set"}");
        Console.WriteLine($"  Branch: {AssetsRepo.branchName}");
        Console.WriteLine($"  Image Path: {AssetsRepo.imageFolderPath}");
        Console.WriteLine($"  Access Token: {(string.IsNullOrEmpty(AssetsRepo.personalAccessToken) ? "Not set" : "***")}");
    }
}

public class AssetsRepoConfig
{
    [JsonPropertyName("personalAccessToken")]
    public string personalAccessToken { get; set; }
    
    [JsonPropertyName("repoOwner")]
    public string repoOwner { get; set; }
    
    [JsonPropertyName("repoName")]
    public string repoName { get; set; }
    
    [JsonPropertyName("branchName")]
    public string branchName { get; set; } = "main";
    
    [JsonPropertyName("imageFolderPath")]
    public string imageFolderPath { get; set; } = "images";
}
