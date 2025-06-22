using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Obsidian2.Utilities
{
    public static class PathUtils
    {
        /// <summary>
        /// Sanitizes a folder name by removing illegal characters and replacing spaces.
        /// </summary>
        /// <param name="folderName">The original folder name.</param>
        /// <returns>A sanitized folder name.</returns>
        public static string SanitizeFolderName(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return string.Empty;

            // Remove illegal characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = new(folderName.Where(c => !invalidChars.Contains(c)).ToArray());

            // Replace spaces and special characters with underscores
            sanitized = Regex.Replace(sanitized, @"[\s\-\.\[\]\(\)]", "_");

            // Remove redundant underscores
            sanitized = Regex.Replace(sanitized, @"_{2,}", "_");

            // Trim leading and trailing underscores
            sanitized = sanitized.Trim('_');

            return sanitized;
        }

        /// <summary>
        /// Encodes spaces in a URL to %20.
        /// </summary>
        /// <param name="url">The URL to encode.</param>
        /// <returns>A URL with spaces encoded.</returns>
        public static string EncodeSpacesInUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            // For non-HTTP/HTTPS links, or if URI parsing fails, directly replace spaces
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                return url.Replace(" ", "%20");
            }

            try
            {
                var uri = new Uri(url);
                string scheme = uri.Scheme;
                string authority = uri.Authority;
                string path = uri.AbsolutePath;
                string query = uri.Query;

                // Encode spaces in the path
                string encodedPath = path.Replace(" ", "%20");

                // Rebuild the URL
                return $"{scheme}://{authority}{encodedPath}{query}";
            }
            catch (UriFormatException)
            {
                // Fallback to simple replacement if URL parsing fails
                return url.Replace(" ", "%20");
            }
        }
    }
}

