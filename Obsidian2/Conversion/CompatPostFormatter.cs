using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;

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
            Console.WriteLine("警告: GitHub配置不完整，无法上传图片到GitHub。请检查配置文件。");
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

            Console.WriteLine($"已初始化GitHub上传器，将上传图片到 {config.RepoOwner}/{config.RepoName} 仓库");
        } catch (Exception ex)
        {
            Console.WriteLine($"初始化GitHub上传器失败: {ex.Message}");
        }
    }


    public async Task Format()
    {
        string content = await File.ReadAllTextAsync(m_InputFile.FullName);

        // 处理图片链接
        content = await ProcessImageLinks(content);

        // 处理表格
        content = ProcessTables(content);

        // 处理Obsidian的callout为兼容的格式
        content = ProcessCallouts(content);

        // 将HTML图片标签转换为Markdown格式
        content = ConvertHtmlImgToMarkdown(content);

        // 修复Markdown图片链接中的空格问题
        content = FixMarkdownImageLinks(content);

        // 写入转换后的文件
        await File.WriteAllTextAsync(m_OutputFilePath, content);

        // 清理未使用的图片
        CleanupUnusedImages();

        Console.WriteLine($"转换完成，文件已保存到：{m_OutputFilePath}");
    }

    /// <summary>
    /// 将Obsidian的callout格式转换为兼容的简单引用格式
    /// </summary>
    private string ProcessCallouts(string content)
    {
        // 匹配Obsidian的callout格式，例如：> [!note]\n>\n> 内容
        // 该方法会匹配 > [!type] 开头的 callout 块，并去除标题和空行
        var calloutPattern = @">\s*\[!([a-zA-Z]+)\].*?\n((?:>\s*.*(?:\n|$))+)";

        // 先处理所有的callout
        var processedContent = Regex.Replace(content, calloutPattern, match =>
        {
            // 提取callout内容部分
            string calloutContent = match.Groups[2].Value;
            string[] lines = calloutContent.Split('\n');

            // 移除每行开头的> 和空格，重新构建为单个引用块
            StringBuilder newContent = new StringBuilder();
            bool previousLineIsEmpty = false;

            foreach (var line in lines)
            {
                // 检查是否为空行（只包含 ">" 或 "> " 或空白）
                bool isEmptyLine = string.IsNullOrWhiteSpace(line) ||
                                   Regex.IsMatch(line, @"^\s*>\s*$");

                if (isEmptyLine)
                {
                    // 避免连续的空行
                    if (!previousLineIsEmpty)
                    {
                        previousLineIsEmpty = true;
                    }

                    continue;
                }

                previousLineIsEmpty = false;
                string cleanLine = line.TrimStart();

                // 去除行首的引用符号和空格
                if (cleanLine.StartsWith(">"))
                {
                    cleanLine = cleanLine.Substring(1).TrimStart();
                }

                // 如果不是空行，添加到新内容中，确保有引用符号
                if (!string.IsNullOrWhiteSpace(cleanLine))
                {
                    newContent.AppendLine($"> {cleanLine}");
                }
            }

            // 确保引用块后面有一个空行，这样知乎可以正确识别引用块
            return newContent.ToString().TrimEnd() + "\n\n";
        });

        // 移除连续的多个空行，保持最多两个空行
        processedContent = Regex.Replace(processedContent, @"\n{3,}", "\n\n");

        // 移除两个不同blockquote之间的多余空行，保持一个空行分隔
        processedContent = Regex.Replace(processedContent, @"(\n>.*?\n)\n+(?=>)", "$1\n");

        // 确保文档最后不会有过多的空行
        processedContent = processedContent.TrimEnd() + "\n";

        return processedContent;
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
                    Console.WriteLine($"警告：上传到GitHub需要将SVG转换为PNG，但转换失败。请确保安装了Inkscape。");
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
                    Console.WriteLine($"图片 {Path.GetFileName(destImagePath)} 已上传到GitHub: {githubUrl}");
                    return githubUrl;
                }
            }

            // 构造GitHub原始链接格式，确保 URL 中的空格被编码为 %20
            string noteName = Uri.EscapeDataString(Path.GetFileNameWithoutExtension(m_InputFile.Name));
            string encodedImageName = Uri.EscapeDataString(Path.GetFileName(destImagePath));

            return $"{m_GithubRepoPrefix}{noteName}/{encodedImageName}";
        } catch (Exception ex)
        {
            Console.WriteLine($"处理图片时出错：{ex.Message}");
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
                Console.WriteLine($"成功将 SVG 转换为 PNG: {Path.GetFileName(svgPath)}");
                return true;
            }

            // 没有找到合适的转换工具，复制原始 SVG
            Console.WriteLine($"警告：无法将 SVG 转换为 PNG。请确保安装了 Inkscape 并添加到 PATH 中。");
            Console.WriteLine($"复制原始 SVG 文件: {Path.GetFileName(svgPath)}");
            File.Copy(svgPath, Path.Combine(m_AssetsFolderPath, Path.GetFileName(svgPath)), true);
            return false;
        } catch (Exception ex)
        {
            Console.WriteLine($"SVG 转 PNG 出错: {ex.Message}");
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
            Console.WriteLine($"警告：图片 {Path.GetFileName(sourcePath)} 大小为 {fileSize:F2}KB，" +
                              $"超过了建议大小（{k_CompressThreshold / 1024}KB）。");
            Console.WriteLine("平台可能会压缩这个图片。建议手动优化图片大小。");
        } catch (Exception ex)
        {
            Console.WriteLine($"复制图片时出错：{ex.Message}");
            // 尝试直接复制
            try
            {
                File.Copy(sourcePath, destPath);
            } catch
            {
                Console.WriteLine($"无法复制图片 {sourcePath}");
            }
        }
    }

    private string ProcessTables(string content)
    {
        // 给表格行后添加额外的换行符
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
            src = EncodeSpacesInUrl(src);

            // 转换为Markdown格式
            return $"![{alt}]({src})";
        });
    }

    /// <summary>
    /// 修复Markdown图片链接中的空格问题
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
            string encodedUrl = EncodeSpacesInUrl(url);

            return $"![{alt}]({encodedUrl})";
        });
    }

    /// <summary>
    /// 对URL中的空格进行编码，转换为%20
    /// </summary>
    private string EncodeSpacesInUrl(string url)
    {
        // 检查URL是否是HTTP/HTTPS链接
        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            // 将URL解析为Uri对象
            try
            {
                var uri = new Uri(url);
                string scheme = uri.Scheme;
                string host = uri.Host;
                string path = uri.AbsolutePath;
                string query = uri.Query;

                // 对路径中的空格进行编码
                string encodedPath = path.Replace(" ", "%20");

                // 重建URL
                string result = $"{scheme}://{host}{encodedPath}{query}";
                return result;
            } catch
            {
                // 如果URL解析失败，回退到简单替换
                return url.Replace(" ", "%20");
            }
        }

        // 对于非HTTP/HTTPS链接，直接替换空格
        return url.Replace(" ", "%20");
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
                Console.WriteLine($"删除未使用的图片：{imageName}, {imagePath}");
                File.Delete(imagePath);
            }
        }
    }
}