using System.Diagnostics;

namespace Obsidian2.Utilities;

/// <summary>
/// Provides utilities for image processing and format conversion.
/// </summary>
public static class ImageUtils
{
    /// <summary>
    /// Converts an SVG file to PNG format using Inkscape, with fallback to copying original SVG.
    /// </summary>
    /// <param name="svgPath">Path to the source SVG file</param>
    /// <param name="pngPath">Path where the PNG output should be saved</param>
    /// <param name="targetFolder">Target folder for fallback SVG copy</param>
    /// <returns>True if conversion succeeded, false if fallback was used</returns>
    /// <example>
    /// <code>
    /// // Convert SVG to PNG
    /// var success = ImageUtils.ConvertSVGToPNG(
    ///     @"C:\images\icon.svg",
    ///     @"C:\output\icon.png",
    ///     @"C:\output"
    /// );
    ///
    /// if (success)
    /// {
    ///     // PNG file created at C:\output\icon.png
    /// }
    /// else
    /// {
    ///     // Original SVG copied to C:\output\icon.svg
    /// }
    /// </code>
    /// </example>
    public static bool ConvertSVGToPNG(string svgPath, string pngPath, string targetFolder)
    {
        try
        {
            if (TryConvertWithInkScape(svgPath, pngPath))
            {
                Console.WriteLine($"Successfully converted SVG to PNG: {Path.GetFileName(svgPath)}");
                return true;
            }

            Console.WriteLine($"Warning: Could not convert SVG to PNG. Please ensure Inkscape is installed and added to your PATH.");
            Console.WriteLine($"Copying original SVG file: {Path.GetFileName(svgPath)}");
            File.Copy(svgPath, Path.Combine(targetFolder, Path.GetFileName(svgPath)), true);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting SVG to PNG: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Determines if a file is an SVG file based on its extension.
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>True if file has .svg extension (case-insensitive), false otherwise</returns>
    /// <example>
    /// <code>
    /// var isSvg1 = ImageUtils.IsSvgFile("icon.svg");        // true
    /// var isSvg2 = ImageUtils.IsSvgFile("icon.SVG");        // true
    /// var isSvg3 = ImageUtils.IsSvgFile("photo.jpg");       // false
    /// var isSvg4 = ImageUtils.IsSvgFile(@"C:\path\logo.svg"); // true
    /// </code>
    /// </example>
    public static bool IsSvgFile(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".svg", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a file is an image file based on its extension.
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>True if file has a supported image extension, false otherwise</returns>
    /// <example>
    /// <code>
    /// var isImg1 = ImageUtils.IsImageFile("photo.jpg");     // true
    /// var isImg2 = ImageUtils.IsImageFile("doc.pdf");       // false
    /// var isImg3 = ImageUtils.IsImageFile("icon.PNG");      // true
    /// var isImg4 = ImageUtils.IsImageFile("");              // false
    /// var isImg5 = ImageUtils.IsImageFile("logo.svg");      // true
    ///
    /// // Supported extensions: .jpg, .jpeg, .png, .gif, .bmp, .svg, .webp
    /// </code>
    /// </example>
    public static bool IsImageFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;

        string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp" };
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return imageExtensions.Contains(extension);
    }

    private static bool TryConvertWithInkScape(string svgPath, string pngPath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "inkscape",
                Arguments = $"--export-filename=\"{pngPath}\" \"{svgPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            process.WaitForExit(10000);
            return File.Exists(pngPath);
        }
        catch
        {
            return false;
        }
    }
}
