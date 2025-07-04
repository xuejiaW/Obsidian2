using System.Text.RegularExpressions;
using System.Diagnostics;
using Obsidian2.Utilities;

namespace Obsidian2;

internal class CompatPostFormatter
{
    private readonly FileInfo m_InputFile;
    private readonly List<string> m_UsedImages = new();
    private string m_OutputFilePath;
    private string m_AssetsFolderPath;

    private string m_GithubRepoPrefix = null;

    private GitHubImageUploader m_GitHubUploader;

    public CompatPostFormatter(FileInfo inputFile, DirectoryInfo outputDir)
    {
        m_InputFile = inputFile;
        m_OutputFilePath = Path.Combine(outputDir.FullName,
                                        Path.GetFileNameWithoutExtension(inputFile.Name) + "_compat.md");
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize GitHub uploader: {ex.Message}");
        }
    }

    public async Task Format()
    {
        string content = await File.ReadAllTextAsync(m_InputFile.FullName);

        // 处理所有图片链接和格式化
        content = await ProcessImageLinks(content);
        content = MarkdownConverter.FormatMarkdownTables(content);
        content = AdmonitionsFormatter.FormatMkDocsCalloutToQuote(content);
        content = MarkdownConverter.ConvertHtmlImgToMarkdown(content);

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

    private async Task<string> ProcessImageAsync(string imagePath)
    {
        try
        {
            // Decode and get the full path of the image.
            string decodedImagePath = Uri.UnescapeDataString(imagePath);
            string fullImagePath = Path.IsPathRooted(decodedImagePath)
                ? decodedImagePath
                : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(m_InputFile.FullName) ?? string.Empty,
                                                decodedImagePath));

            // Generate a unique image name and process the image.
            string destImagePath = GetUniqueImagePath(fullImagePath);
            bool isSvg = Path.GetExtension(fullImagePath).Equals(".svg", StringComparison.OrdinalIgnoreCase);

            if (isSvg)
            {
                string pngFileName = Path.GetFileNameWithoutExtension(Path.GetFileName(destImagePath)) + ".png";
                string pngFilePath = Path.Combine(m_AssetsFolderPath, pngFileName);

                if (SvgConverter.ConvertSvgToPng(fullImagePath, pngFilePath, m_AssetsFolderPath))
                {
                    destImagePath = pngFilePath;
                }
            }
            else
            {
                File.Copy(fullImagePath, destImagePath);
            }

            m_UsedImages.Add(Path.GetFileName(destImagePath));

            return await GetGithubImageUrl(destImagePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing image: {ex.Message}");
            return string.Empty;
        }
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

    private async Task<string> GetGithubImageUrl(string imagePath)
    {
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

        string noteName = Uri.EscapeDataString(Path.GetFileNameWithoutExtension(m_InputFile.Name));
        string encodedImageName = Uri.EscapeDataString(Path.GetFileName(imagePath));

        return $"{m_GithubRepoPrefix}{noteName}/{encodedImageName}";
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

