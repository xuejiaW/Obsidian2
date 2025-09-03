
using System;
using System.IO;
using System.Security.Cryptography;

namespace Obsidian2.Utilities;

/// <summary>
/// Provides utilities for file system operations including directory validation,
/// file hashing, and directory management.
/// </summary>
public static class FileSystemUtils
{
    /// <summary>
    /// Validates that a directory exists, throwing an exception with a descriptive message if it doesn't.
    /// </summary>
    /// <param name="directory">The directory to check</param>
    /// <param name="description">Human-readable description of the directory for error messages</param>
    /// <exception cref="ArgumentException">Thrown when directory does not exist</exception>
    /// <example>
    /// <code>
    /// var sourceDir = new DirectoryInfo(@"C:\MyProject\Source");
    ///
    /// // This will throw ArgumentException if directory doesn't exist
    /// FileSystemUtils.CheckDirectory(sourceDir, "source directory");
    /// // Exception message: "The source directory: C:\MyProject\Source does not exist."
    /// </code>
    /// </example>
    public static void CheckDirectory(DirectoryInfo directory, string description)
    {
        if (!directory.Exists)
            throw new ArgumentException($"The {description}: {directory.FullName} does not exist.");
    }

    /// <summary>
    /// Computes MD5 hash of a file, with fallback mechanism for error cases.
    /// </summary>
    /// <param name="filePath">Path to the file to hash</param>
    /// <returns>Lowercase hexadecimal MD5 hash, or fallback identifier if file cannot be hashed</returns>
    /// <example>
    /// <code>
    /// // For existing file
    /// var hash = FileSystemUtils.ComputeFileHash(@"C:\temp\document.txt");
    /// // Returns: "d41d8cd98f00b204e9800998ecf8427e"
    ///
    /// // For non-existent file (fallback)
    /// var fallbackHash = FileSystemUtils.ComputeFileHash(@"C:\temp\missing.txt");
    /// // Returns: "fallback_1234567890_637891234567890123"
    /// </code>
    /// </example>
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

    /// <summary>
    /// Ensures that a directory exists, creating it (and parent directories) if necessary.
    /// </summary>
    /// <param name="directoryPath">Path to the directory to ensure exists</param>
    /// <example>
    /// <code>
    /// // Create nested directory structure if it doesn't exist
    /// FileSystemUtils.EnsureDirectoryExists(@"C:\MyApp\Data\Cache\Images");
    ///
    /// // After this call, the entire directory path will exist
    /// // (creates C:\MyApp, C:\MyApp\Data, C:\MyApp\Data\Cache, C:\MyApp\Data\Cache\Images)
    /// </code>
    /// </example>
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Recursively copies a directory and all its contents to a destination directory with optional ignore filtering.
    /// </summary>
    /// <param name="sourceDirectory">Source directory to copy from</param>
    /// <param name="destinationDir">Destination directory path</param>
    /// <param name="ignoreList">Optional list of path patterns to ignore during copying</param>
    /// <returns>Task representing the asynchronous copy operation</returns>
    /// <example>
    /// <code>
    /// // Copy entire directory
    /// await FileSystemUtils.DeepCopyDirectory(@"C:\Source", @"C:\Destination");
    ///
    /// // Copy with ignore patterns
    /// var ignoreList = new List&lt;string&gt; { ".git", "node_modules", ".tmp" };
    /// await FileSystemUtils.DeepCopyDirectory(@"C:\Project", @"C:\Backup", ignoreList);
    /// </code>
    /// </example>
    public static async Task DeepCopyDirectory(DirectoryInfo sourceDirectory, string destinationDir,
                                               List<string> ignoreList = null)
    {
        if (string.IsNullOrEmpty(destinationDir)) return;
        if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);

        Directory.GetDirectories(sourceDirectory.FullName, "*", SearchOption.AllDirectories)
                 .Where(dir => ignoreList == null || !ignoreList.Any(dir.Contains)).ToList()
                 .ForEach(dir =>
                  {
                      Directory.CreateDirectory(dir.Replace(sourceDirectory.FullName, destinationDir));
                  });

        List<string> files = Directory.GetFiles(sourceDirectory.FullName, "*.*", SearchOption.AllDirectories)
                                      .Where(path => ignoreList == null || !ignoreList.Any(path.Contains)).ToList();

        IEnumerable<Task> tasks = files.Select(file => Task.Run(async () =>
        {
            await CopyFileInternal(file);
        }));

        await Task.WhenAll(tasks);

        Task CopyFileInternal(string file)
        {
            string targetPath = file.Replace(sourceDirectory.FullName, destinationDir);
            var info = new FileInfo(targetPath);
            targetPath = Path.Join(info.Directory.FullName, info.Name.ToLower());

            File.Copy(file, targetPath, true);
            return Task.CompletedTask;
        }
    }
}
