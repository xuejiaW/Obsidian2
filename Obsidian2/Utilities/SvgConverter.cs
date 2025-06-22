using System.Diagnostics;

namespace Obsidian2.Utilities;

/// <summary>
/// 提供 SVG 图像到 PNG 图像的转换功能
/// </summary>
public static class SvgConverter
{
    /// <summary>
    /// 将 SVG 文件转换为 PNG 文件
    /// </summary>
    /// <param name="svgPath">SVG 文件路径</param>
    /// <param name="pngPath">输出 PNG 文件路径</param>
    /// <param name="targetFolder">如果转换失败，用于保存原始 SVG 文件的目标文件夹</param>
    /// <returns>是否成功转换</returns>
    public static bool ConvertSvgToPng(string svgPath, string pngPath, string targetFolder)
    {
        try
        {
            // 尝试使用 Inkscape 进行转换
            if (TryConvertWithInkscape(svgPath, pngPath))
            {
                Console.WriteLine($"Successfully converted SVG to PNG: {Path.GetFileName(svgPath)}");
                return true;
            }

            // 转换失败，提示并复制原始 SVG
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
    /// 尝试使用 Inkscape 将 SVG 转换为 PNG
    /// </summary>
    /// <param name="svgPath">SVG 文件路径</param>
    /// <param name="pngPath">输出 PNG 文件路径</param>
    /// <returns>是否成功转换</returns>
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

            process.WaitForExit(10000); // 等待最多10秒
            return File.Exists(pngPath);
        }
        catch
        {
            return false; // Inkscape 不存在或转换失败
        }
    }
}
