using System.Text.RegularExpressions;
using System.Text;
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

        content = await ProcessImageLinks(content);

        content = ProcessTables(content);

        content = AdmonitionsFormatter.FormatMkDocsCalloutToQuote(content);

        content = ConvertHtmlImgToMarkdown(content);

        content = FixMarkdownImageLinks(content);

        await File.WriteAllTextAsync(m_OutputFilePath, content);

        // Cleanup unused images
        CleanupUnusedImages();

        Console.WriteLine($"Conversion complete, file saved to: {m_OutputFilePath}");
    }

    private async Task<string> ProcessImageLinks(string content)
    {
        // 处理 Markdown 标准图片语法 ![alt](path)
        content = await ReplaceMarkdownImagesAsync(content);

        // 处理 HTML 图片标签 <img src="path" ...>
        content = await ReplaceHtmlImagesAsync(content);

        return content;
    }

    private async Task<string> ReplaceMarkdownImagesAsync(string content)
    {
        // 使用正则表达式查找所有的Markdown图片
        var matches = Regex.Matches(content, @"!\[(.*?)\]\((.*?)\)");

        // 逐一处理每个图片
        foreach (Match match in matches)
        {
            string altText = match.Groups[1].Value;
            string imagePath = match.Groups[2].Value;

            string newImagePath = await ProcessImageAsync(imagePath);
            if (!string.IsNullOrEmpty(newImagePath))
            {
                string newImageMarkdown = $"![{altText}]({newImagePath})";
                content = content.Replace(match.Value, newImageMarkdown);
            }
        }

        return content;
    }

    private async Task<string> ReplaceHtmlImagesAsync(string content)
    {
        // 使用正则表达式查找所有的HTML图片标签
        var matches = Regex.Matches(content, @"<img src=""(.*?)""");

        // 逐一处理每个图片
        foreach (Match match in matches)
        {
            string imagePath = match.Groups[1].Value;

            string newImagePath = await ProcessImageAsync(imagePath);
            if (!string.IsNullOrEmpty(newImagePath))
            {
                string replacement = $"<img src=\"{newImagePath}\"";
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
            // 解码 URL 编码的路径（例如 %20 转换为空格）
            string decodedImagePath = Uri.UnescapeDataString(imagePath);

            // 检查是否是相对路径
            string fullImagePath = Path.IsPathRooted(decodedImagePath)
                ? decodedImagePath
                : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(m_InputFile.FullName) ?? string.Empty,
                                                decodedImagePath));

            // 生成唯一的图片名称
            string imgStem = Path.GetFileNameWithoutExtension(fullImagePath);
            string imgSuffix = Path.GetExtension(fullImagePath);
            string imgNameNew = imgStem + imgSuffix;

            int i = 1;
            while (File.Exists(Path.Combine(m_AssetsFolderPath, imgNameNew)))
            {
                imgNameNew = $"{imgStem}_{i}{imgSuffix}";
                i++;
            }

            string destImagePath = Path.Combine(m_AssetsFolderPath, imgNameNew);
            bool isSvg = imgSuffix.Equals(".svg", StringComparison.OrdinalIgnoreCase);

            if (isSvg)
            {
                string pngFileName = Path.GetFileNameWithoutExtension(imgNameNew) + ".png";
                string pngFilePath = Path.Combine(m_AssetsFolderPath, pngFileName);

                if (ConvertSvgToPng(fullImagePath, pngFilePath))
                {
                    destImagePath = pngFilePath;
                }
                else
                {
                    Console.WriteLine($"Warning: Uploading to GitHub requires converting SVG to PNG, but the conversion failed. Please ensure Inkscape is installed.");
                }
            }
            else
            {
                // 复制或压缩图片
                if (m_CompressImages && new FileInfo(fullImagePath).Length > k_CompressThreshold)
                {
                    CompressImage(fullImagePath, destImagePath);
                }
                else
                {
                    File.Copy(fullImagePath, destImagePath);
                }
            }

            m_UsedImages.Add(Path.GetFileName(destImagePath));

            // 如果启用了GitHub上传，上传图片
            if (m_GitHubUploader != null)
            {
                string articleName = Path.GetFileNameWithoutExtension(m_InputFile.Name);
                string githubUrl = await m_GitHubUploader.UploadImageAsync(destImagePath, articleName);

                if (!string.IsNullOrEmpty(githubUrl))
                {
                    Console.WriteLine($"Image {Path.GetFileName(destImagePath)} has been uploaded to GitHub: {githubUrl}");
                    return githubUrl;
                }
            }

            // 构造GitHub原始链接格式，确保 URL 中的空格被编码为 %20
            string noteName = Uri.EscapeDataString(Path.GetFileNameWithoutExtension(m_InputFile.Name));
            string encodedImageName = Uri.EscapeDataString(Path.GetFileName(destImagePath));

            return $"{m_GithubRepoPrefix}{noteName}/{encodedImageName}";
        } catch (Exception ex)
        {
            Console.WriteLine($"Error processing image: {ex.Message}");
            return string.Empty;
        }
    }

    private bool ConvertSvgToPng(string svgPath, string pngPath)
    {
        try
        {
            // 尝试使用 Inkscape 进行转换（如果安装了）
            if (TryConvertWithInkscape(svgPath, pngPath))
            {
                Console.WriteLine($"Successfully converted SVG to PNG: {Path.GetFileName(svgPath)}");
                return true;
            }

            // 没有找到合适的转换工具，复制原始 SVG
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
        try
        {
            // 检查是否安装了 Inkscape
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "inkscape",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                process.WaitForExit(3000); // 等待最多3秒

                // 如果 Inkscape 存在，使用它转换 SVG 到 PNG
                var convertProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "inkscape",
                        Arguments = $"--export-filename=\"{pngPath}\" \"{svgPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                convertProcess.Start();
                convertProcess.WaitForExit(10000); // 等待最多10秒

                return File.Exists(pngPath);
            } catch (Exception)
            {
                // Inkscape 不存在
                return false;
            }
        } catch
        {
            return false;
        }
    }

    private void CompressImage(string sourcePath, string destPath)
    {
        try
        {
            // 由于我们没有添加 System.Drawing.Common 依赖，
            // 这里简单地复制文件并添加警告消息
            File.Copy(sourcePath, destPath);

            var fileSize = new FileInfo(sourcePath).Length / 1024.0;
            Console.WriteLine($"Warning: Image {Path.GetFileName(sourcePath)} size is {fileSize:F2}KB, " +
                              $"which exceeds the recommended size of {k_CompressThreshold / 1024}KB.");
            Console.WriteLine("The platform may compress this image. It is recommended to optimize the image size manually.");
        } catch (Exception ex)
        {
            Console.WriteLine($"Error copying image: {ex.Message}");
            // 尝试直接复制
            try
            {
                File.Copy(sourcePath, destPath);
            } catch
            {
                Console.WriteLine($"Could not copy image {sourcePath}");
            }
        }
    }

    private string ProcessTables(string content)
    {
        // 给表格行后添加额外的���行符
        return Regex.Replace(content, @"\|\n", "|\n\n");
    }

    /// <summary>
    /// 将HTML图片标签&lt;img src="..."&gt;转换为Markdown格式![](...)
    /// </summary>
    private string ConvertHtmlImgToMarkdown(string content)
    {
        // 匹配完整的HTML图片标签，包括可选的alt属性和其他属性
        string pattern = @"<img\s+(?:src=""(.*?)"")?(?:\s+alt=""(.*?)"")?[^>]*>";

        return Regex.Replace(content, pattern, match =>
        {
            string src = match.Groups[1].Success ? match.Groups[1].Value : "";
            string alt = match.Groups[2].Success ? match.Groups[2].Value : "";

            if (string.IsNullOrEmpty(src))
            {
                // 如果没有src属性，则保持原样
                return match.Value;
            }

            // 对URL中的空格进行编码
            src = PathUtils.EncodeSpacesInUrl(src);

            // 转换为Markdown格式
            return $"![{alt}]({src})";
        });
    }

    /// <summary>
    /// 修复Markdown��片链接中的空格问题
    /// </summary>
    private string FixMarkdownImageLinks(string content)
    {
        // 匹配Markdown图片链接：![alt](url)
        string pattern = @"!\[(.*?)\]\((.*?)\)";

        return Regex.Replace(content, pattern, match =>
        {
            string alt = match.Groups[1].Value;
            string url = match.Groups[2].Value;

            // 对URL中的空格进行编码
            string encodedUrl = PathUtils.EncodeSpacesInUrl(url);

            return $"![{alt}]({encodedUrl})";
        });
    }

    private void CleanupUnusedImages()
    {
        if (!Directory.Exists(m_AssetsFolderPath))
            return;

        foreach (string imagePath in Directory.GetFiles(m_AssetsFolderPath))
        {
            string imageName = Path.GetFileName(imagePath);
            if (!m_UsedImages.Contains(imageName))
            {
                Console.WriteLine($"Deleting unused image: {imageName}, {imagePath}");
                File.Delete(imagePath);
            }
        }
    }
}

