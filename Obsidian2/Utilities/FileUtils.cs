using System;
using System.IO;
using System.Security.Cryptography;

namespace Obsidian2.Utilities
{
    public static class FileUtils
    {
        /// <summary>
        /// Computes the MD5 hash of a file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>The MD5 hash as a lowercase string.</returns>
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
                // If hash calculation fails, use file size and modification time as a fallback.
                var fileInfo = new FileInfo(filePath);
                return $"{fileInfo.Length}_{fileInfo.LastWriteTimeUtc.Ticks}";
            }
        }
    }
}

