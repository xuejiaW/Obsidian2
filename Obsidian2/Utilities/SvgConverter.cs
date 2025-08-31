using System.Diagnostics;

namespace Obsidian2.Utilities;

public static class SvgConverter
{
    public static bool ConvertSvgToPng(string svgPath, string pngPath, string targetFolder)
    {
        try
        {
            if (TryConvertWithInkscape(svgPath, pngPath))
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

    private static bool TryConvertWithInkscape(string svgPath, string pngPath)
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
