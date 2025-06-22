using System.Text.RegularExpressions;
using System.Diagnostics;
using Obsidian2.Utilities;

namespace Obsidian2;

internal class CompatPostFormatter
{
    private readonly FileInfo m_InputFile;
    private readonly bool m_CompressImages;
    private readonly List<string> m_UsedImages = new();
    private string m_OutputFilePath;
    private string m_AssetsFolderPath;

    private const long k_CompressThreshold = 500 * 1024;

    private string m_GithubRepoPrefix = null;

    private GitHubImageUploader m_GitHubUploader;

    public CompatPostFormatter(FileInfo inputFile, DirectoryInfo outputDir, bool compressImages)
    {
        m_InputFile = inputFile;
        m_OutputFilePath = Path.Combine(outputDir.FullName,
                                        Path.GetFileNameWithoutExtension(inputFile.Name) + "_compat.md");
        m_CompressImages = compressImages;
        m_AssetsFolderPath = Path.Combine(outputDir.FullName, Path.GetFileNameWithoutExtension(inputFile.Name));

        if (!outputDir.Exists) outputDir.Create();
        if (!Directory.Exists(m_AssetsFolderPath)) Directory.CreateDirectory(m_AssetsFolderPath);

        InitGitHubUploader();
        GitHubConfig config = ConfigurationMgr.configuration.GitHub;
        m_GithubRepoPrefix
            = $"https://raw.githubusercontent.com/{config.RepoOwner}/{config.RepoName}/{config.BranchName}/{config.ImageBasePath}/";
    }

    private void InitGitHubUploader()
    {
        GitHubConfig config = ConfigurationMgr.configuration.GitHub;

        if (string.IsNullOrWhiteSpace(config.PersonalAccessToken) ||
            string.IsNullOrWhiteSpace(config.RepoOwner) ||
            string.IsNullOrWhiteSpace(config.RepoName))
        {
            Console.WriteLine("Warning: GitHub configuration is incomplete, cannot upload images to GitHub. Please check your configuration file.");
            return;
        }

        try
        {
            m_GitHubUploader = new GitHubImageUploader(config.PersonalAccessToken,
                                                       config.RepoOwner,
                                                       config.RepoName,
                                                       config.BranchName,
                                                       config.ImageBasePath
                                                      );

            Console.WriteLine($"GitHub uploader initialized, will upload images to {config.RepoOwner}/{config.RepoName} repository");
        } catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize GitHub uploader: {ex.Message}");
        }
    }

    public async Task Format()
    {
        string content = await File.ReadAllTextAsync(m_InputFile.FullName);

        // 处理所有图片链接和格式化
        content = await ProcessImageLinks(content);
        content = ProcessTables(content);
        content = AdmonitionsFormatter.FormatMkDocsCalloutToQuote(content);
        content = ConvertHtmlImgToMarkdown(content);

        // 写入文件并清理
        await File.WriteAllTextAsync(m_OutputFilePath, content);
        CleanupUnusedImages();

        Console.WriteLine($"Conversion complete, file saved to: {m_OutputFilePath}");
    }

    private async Task<string> ProcessImageLinks(string content)
    {
        content = await ProcessImageLinksWithPattern(content,
                                                     @"!\[(.*?)\]\((.*?)\)",
                                                     (match, newPath) => $"![{match.Groups[1].Value}]({newPath})");

        content = await ProcessImageLinksWithPattern(content,
                                                     @"<img src=""(.*?)""",
                                                     (_, newPath) => $"<img src=\"{newPath}\"");

        return content;
    }

    private async Task<string> ProcessImageLinksWithPattern(string content, string pattern,
                                                            Func<Match, string, string> replacementBuilder)
    {
        MatchCollection matches = Regex.Matches(content, pattern);

        foreach (Match match in matches)
        {
            string imagePath = match.Groups[pattern.Contains("img") ? 1 : 2].Value;
            string newImagePath = await ProcessImageAsync(imagePath);

            if (!string.IsNullOrEmpty(newImagePath))
            {
                string replacement = replacementBuilder(match, newImagePath);
                content = content.Replace(match.Value, replacement);
            }
        }

        return content;
    }

    /// <summary>
    /// 转换并上传图片，返回正确的URL
    /// </summary>
    private async Task<string> ProcessImageAsync(string imagePath)
    {
        try
        {
            // 解码路径并获取完整路径
            string decodedImagePath = Uri.UnescapeDataString(imagePath);
            string fullImagePath = GetFullImagePath(decodedImagePath);

            // 生成唯一的图片名称并处理图片
            string destImagePath = GetUniqueImagePath(fullImagePath);
            bool isSvg = Path.GetExtension(fullImagePath).Equals(".svg", StringComparison.OrdinalIgnoreCase);

            if (isSvg)
            {
                destImagePath = HandleSvgImage(fullImagePath, destImagePath);
            }
            else
            {
                HandleRegularImage(fullImagePath, destImagePath);
            }

            // 记录已使用的图片
            m_UsedImages.Add(Path.GetFileName(destImagePath));

            // 尝试上传到GitHub并返回URL
            return await GetImageUrl(destImagePath);
        } catch (Exception ex)
        {
            Console.WriteLine($"Error processing image: {ex.Message}");
            return string.Empty;
        }
        
        string HandleSvgImage(string fullImagePath, string destImagePath)
        {
            string pngFileName = Path.GetFileNameWithoutExtension(Path.GetFileName(destImagePath)) + ".png";
            string pngFilePath = Path.Combine(m_AssetsFolderPath, pngFileName);

            if (ConvertSvgToPng(fullImagePath, pngFilePath))
            {
                return pngFilePath;
            }

            return destImagePath;
        }

        void HandleRegularImage(string fullImagePath, string destImagePath)
        {
            if (m_CompressImages && new FileInfo(fullImagePath).Length > k_CompressThreshold)
            {
                CompressImage(fullImagePath, destImagePath);
            }
            else
            {
                File.Copy(fullImagePath, destImagePath);
            }
        }
    }

    private string GetFullImagePath(string imagePath)
    {
        return Path.IsPathRooted(imagePath)
            ? imagePath
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(m_InputFile.FullName) ?? string.Empty, imagePath));
    }

    private string GetUniqueImagePath(string fullImagePath)
    {
        string imgStem = Path.GetFileNameWithoutExtension(fullImagePath);
        string imgSuffix = Path.GetExtension(fullImagePath);
        string imgNameNew = imgStem + imgSuffix;

        int i = 1;
        while (File.Exists(Path.Combine(m_AssetsFolderPath, imgNameNew)))
        {
            imgNameNew = $"{imgStem}_{i}{imgSuffix}";
            i++;
        }

        return Path.Combine(m_AssetsFolderPath, imgNameNew);
    }


    private async Task<string> GetImageUrl(string imagePath)
    {
        // 如果启用了GitHub上传，上传图片
        if (m_GitHubUploader != null)
        {
            string articleName = Path.GetFileNameWithoutExtension(m_InputFile.Name);
            string githubUrl = await m_GitHubUploader.UploadImageAsync(imagePath, articleName);

            if (!string.IsNullOrEmpty(githubUrl))
            {
                Console.WriteLine($"Image {Path.GetFileName(imagePath)} has been uploaded to GitHub: {githubUrl}");
                return githubUrl;
            }
        }

        // 构造GitHub原始链接格式
        string noteName = Uri.EscapeDataString(Path.GetFileNameWithoutExtension(m_InputFile.Name));
        string encodedImageName = Uri.EscapeDataString(Path.GetFileName(imagePath));

        return $"{m_GithubRepoPrefix}{noteName}/{encodedImageName}";
    }

    private bool ConvertSvgToPng(string svgPath, string pngPath)
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
            File.Copy(svgPath, Path.Combine(m_AssetsFolderPath, Path.GetFileName(svgPath)), true);
            return false;
        } catch (Exception ex)
        {
            Console.WriteLine($"Error converting SVG to PNG: {ex.Message}");
            return false;
        }
    }

    private bool TryConvertWithInkscape(string svgPath, string pngPath)
    {
        // 检查是否安装了 Inkscape 并转换图片
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
        } catch
        {
            return false; // Inkscape 不存在或转换失败
        }
    }

    private void CompressImage(string sourcePath, string destPath)
    {
        try
        {
            // 复制文件
            File.Copy(sourcePath, destPath);

            // 显示警告信息
            var fileSize = new FileInfo(sourcePath).Length / 1024.0;
            Console.WriteLine($"Warning: Image {Path.GetFileName(sourcePath)} size is {fileSize:F2}KB, " +
                              $"which exceeds the recommended size of {k_CompressThreshold / 1024}KB.");
            Console.WriteLine("The platform may compress this image. It is recommended to optimize the image size manually.");
        } catch (Exception ex)
        {
            Console.WriteLine($"Error copying image: {ex.Message}");
            Console.WriteLine($"Could not copy image {sourcePath}");
        }
    }

    private string ProcessTables(string content)
    {
        // 给表格行后添加额外的���行符
        return Regex.Replace(content, @"\|\n", "|\n\n");
    }

    /// <summary>
    /// 将HTML图片标签&lt;img src="..."&gt;转换为Markdown格式![](...)
    /// 同时修复Markdown图片链接中的空格问题
    /// </summary>
    private string ConvertHtmlImgToMarkdown(string content)
    {
        // 首先处理HTML图片标签
        string htmlPattern = @"<img\s+(?:src=""(.*?)"")?(?:\s+alt=""(.*?)"")?[^>]*>";
        content = Regex.Replace(content, htmlPattern, match =>
        {
            string src = match.Groups[1].Success ? match.Groups[1].Value : "";
            string alt = match.Groups[2].Success ? match.Groups[2].Value : "";

            if (string.IsNullOrEmpty(src))
                return match.Value;

            src = PathUtils.EncodeSpacesInUrl(src);
            return $"![{alt}]({src})";
        });

        // 然后处理Markdown图片链接中的空格问题
        string mdPattern = @"!\[(.*?)\]\((.*?)\)";
        content = Regex.Replace(content, mdPattern, match =>
        {
            string alt = match.Groups[1].Value;
            string url = match.Groups[2].Value;
            string encodedUrl = PathUtils.EncodeSpacesInUrl(url);
            return $"![{alt}]({encodedUrl})";
        });

        return content;
    }

    private void CleanupUnusedImages()
    {
        if (!Directory.Exists(m_AssetsFolderPath))
            return;

        var files = Directory.GetFiles(m_AssetsFolderPath);
        foreach (string imagePath in files)
        {
            string imageName = Path.GetFileName(imagePath);
            if (!m_UsedImages.Contains(imageName))
            {
                Console.WriteLine($"Deleting unused image: {imageName}");
                File.Delete(imagePath);
            }
        }
    }
}