namespace Obsidian2;

public static class Utils
{
    public static void CheckDirectory(DirectoryInfo directory, string description)
    {
        if (!directory.Exists)
            throw new ArgumentException($"The {description}: {directory.FullName} does not exist.");
    }
}