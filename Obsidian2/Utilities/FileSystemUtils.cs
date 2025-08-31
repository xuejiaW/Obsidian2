
using System;
using System.IO;
using System.Security.Cryptography;

namespace Obsidian2.Utilities;

public static class FileSystemUtils
{
    public static void CheckDirectory(DirectoryInfo directory, string description)
    {
        if (!directory.Exists)
            throw new ArgumentException($"The {description}: {directory.FullName} does not exist.");
    }

    public static string ComputeFileHash(string filePath)
    {
        try
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    return $"{fileInfo.Length}_{fileInfo.LastWriteTimeUtc.Ticks}";
                }
            }
            catch
            {
                // Ignore errors in fallback calculation
            }

            // Return a hash based on file path if file doesn't exist
            return $"fallback_{filePath.GetHashCode():X}_{DateTime.UtcNow.Ticks}";
        }
    }

    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
