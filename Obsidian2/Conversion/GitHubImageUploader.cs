using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net.Http.Json;
using Obsidian2.Utilities;

namespace Obsidian2;

/// <summary>
/// Responsible for uploading images to GitHub repository
/// </summary>
public class GitHubImageUploader
{
    private readonly string m_Owner;
    private readonly string m_Repo;
    private readonly string m_Branch;
    private readonly string m_ImageBasePath;
    private readonly HttpClient m_HttpClient;

    private readonly int m_UploadTimeoutSeconds = 60;

    private readonly HashSet<string> m_UploadedFiles = new();

    private readonly string m_CacheFilePath;
    private Dictionary<string, string> m_FileHashCache = new();

    /// <summary>
    /// Creates a GitHub image uploader instance
    /// </summary>
    /// <param name="token">GitHub personal access token</param>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="branch">Branch name</param>
    /// <param name="imageBasePath">Base path for images in the repository</param>
    public GitHubImageUploader(string token, string owner, string repo, string branch, string imageBasePath)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("GitHub personal access token cannot be empty", nameof(token));

        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Repository owner cannot be empty", nameof(owner));

        if (string.IsNullOrWhiteSpace(repo))
            throw new ArgumentException("Repository name cannot be empty", nameof(repo));

        m_Owner = owner;
        m_Repo = repo;
        m_Branch = string.IsNullOrWhiteSpace(branch) ? "master" : branch;
        m_ImageBasePath = string.IsNullOrWhiteSpace(imageBasePath) ? "images" : imageBasePath;

        // Create HttpClient for API calls with timeout settings
        m_HttpClient = new HttpClient();
        m_HttpClient.Timeout = TimeSpan.FromSeconds(m_UploadTimeoutSeconds);
        m_HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Obsidian2-App", "1.0"));
        m_HttpClient.DefaultRequestHeaders.Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        m_HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Set cache file path
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string cacheDir = Path.Combine(appDataPath, "Obsidian2");

        if (!Directory.Exists(cacheDir))
            Directory.CreateDirectory(cacheDir);

        m_CacheFilePath = Path.Combine(cacheDir, "github_upload_cache.json");
        LoadCacheFromDisk();
    }

    /// <summary>
    /// Load cache from disk
    /// </summary>
    private void LoadCacheFromDisk()
    {
        try
        {
            if (!File.Exists(m_CacheFilePath)) return;
            string json = File.ReadAllText(m_CacheFilePath);
            var cache = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (cache == null) return;
            m_FileHashCache = cache;
            Console.WriteLine($"Upload cache loaded from disk, containing {m_FileHashCache.Count} entries");
        } catch (Exception ex)
        {
            Console.WriteLine($"Error loading upload cache: {ex.Message}");
            m_FileHashCache = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Save cache to disk
    /// </summary>
    private void SaveCacheToDisk()
    {
        try
        {
            string json = JsonConvert.SerializeObject(m_FileHashCache);
            File.WriteAllText(m_CacheFilePath, json);
        } catch (Exception ex)
        {
            Console.WriteLine($"Error saving upload cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Upload image to GitHub repository
    /// </summary>
    /// <param name="localImagePath">Local image path</param>
    /// <param name="remoteFolderName">Remote folder name (usually article name)</param>
    /// <returns>Original URL after successful upload</returns>
    public async Task<string> UploadImageAsync(string localImagePath, string remoteFolderName)
    {
        try
        {
            if (!File.Exists(localImagePath)) throw new FileNotFoundException($"Image not found: {localImagePath}");

            string imageFileName = Path.GetFileName(localImagePath);
            string sanitizedFolderName = PathUtils.SanitizeFolderName(remoteFolderName);
            string remotePath = Path.Combine(m_ImageBasePath, sanitizedFolderName, imageFileName).Replace('\\', '/');

            string fileHash = FileUtils.ComputeFileHash(localImagePath);

            // Check if record exists in local cache and hash matches
            if (m_FileHashCache.TryGetValue(remotePath, out string cachedHash) && cachedHash == fileHash)
            {
                Console.WriteLine($"Image {imageFileName} already exists on GitHub with identical content, skipping upload");
                return GetRawUrl(remotePath);
            }

            // Check memory cache (valid only for current session)
            string cacheKey = $"{remotePath}:{fileHash}";
            if (m_UploadedFiles.Contains(cacheKey))
            {
                Console.WriteLine($"Image {imageFileName} already uploaded in current session, using cached URL");
                return GetRawUrl(remotePath);
            }

            // Use upload method with timeout and cancellation token
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(m_UploadTimeoutSeconds));

            try
            {
                // Check if file already exists on GitHub
                string existingSha = await GetFileShaAsync(remotePath, cancellationTokenSource.Token);

                // If file exists, don't upload, record hash and return URL
                if (!string.IsNullOrEmpty(existingSha))
                {
                    Console.WriteLine($"File {remotePath} already exists on GitHub, SHA: {existingSha}, recording hash and skipping upload");

                    // Update cache
                    m_UploadedFiles.Add(cacheKey);
                    m_FileHashCache[remotePath] = fileHash;
                    SaveCacheToDisk();

                    return GetRawUrl(remotePath);
                }

                // Only upload if file doesn't exist
                Console.WriteLine($"Uploading image: {imageFileName} to path: {remotePath}");
                Console.WriteLine($"Starting file upload, timeout set to: {m_UploadTimeoutSeconds} seconds");

                // Upload new file
                string url = await UploadFileToGitHubAsync(localImagePath, remotePath, null,
                                                           cancellationTokenSource.Token);

                if (!string.IsNullOrEmpty(url))
                {
                    // Update cache
                    m_UploadedFiles.Add(cacheKey);
                    m_FileHashCache[remotePath] = fileHash;
                    SaveCacheToDisk();

                    Console.WriteLine($"Image {imageFileName} successfully uploaded to GitHub: {url}");
                    return url;
                }

                Console.WriteLine($"Warning: Failed to upload image {imageFileName}");
                return string.Empty;
            } catch (OperationCanceledException)
            {
                Console.WriteLine($"Error: Upload of image {imageFileName} timed out, operation cancelled");
                return string.Empty;
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"Failed to upload image {Path.GetFileName(localImagePath)} to GitHub: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"Detailed error: {ex.InnerException.Message}");

            // Return empty string on upload failure
            return string.Empty;
        }
    }

    /// <summary>
    /// Directly upload file using GitHub API
    /// </summary>
    private async Task<string> UploadFileToGitHubAsync(string localFilePath, string remotePath, string existingSha,
                                                       CancellationToken cancellationToken)
    {
        try
        {
            byte[] fileContent = await File.ReadAllBytesAsync(localFilePath, cancellationToken);
            var fileInfo = new FileInfo(localFilePath);
            Console.WriteLine($"Reading file: {localFilePath}, size: {fileContent.Length} bytes, modified: {fileInfo.LastWriteTime}");

            // Prepare request content
            object requestBody;

            if (!string.IsNullOrEmpty(existingSha))
            {
                Console.WriteLine($"File {remotePath} already exists, SHA: {existingSha}, updating file");
                requestBody = new
                {
                    message = $"Update file {Path.GetFileName(remotePath)}",
                    content = Convert.ToBase64String(fileContent),
                    sha = existingSha,
                    branch = m_Branch
                };
            }
            else
            {
                Console.WriteLine($"Creating new file: {remotePath}");
                requestBody = new
                {
                    message = $"Add file {Path.GetFileName(remotePath)}",
                    content = Convert.ToBase64String(fileContent),
                    branch = m_Branch
                };
            }

            // Send request
            var requestUri = $"https://api.github.com/repos/{m_Owner}/{m_Repo}/contents/{remotePath}";
            Console.WriteLine($"Sending request to: {requestUri}");

            // Use JsonContent instead of PutAsJsonAsync to avoid serialization issues
            var jsonContent = JsonContent.Create(requestBody);

            // Monitor upload progress
            Console.WriteLine($"Starting file content upload, size: {fileContent.Length} bytes...");
            var uploadStartTime = DateTime.Now;

            try
            {
                var response = await m_HttpClient.PutAsync(requestUri, jsonContent, cancellationToken);

                var uploadDuration = DateTime.Now - uploadStartTime;
                Console.WriteLine($"Upload request completed, time elapsed: {uploadDuration.TotalSeconds:F2} seconds");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"File upload successful: {remotePath}, status code: {response.StatusCode}");
                    return GetRawUrl(remotePath);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    Console.WriteLine($"API returned error: {response.StatusCode}");
                    Console.WriteLine($"Error details: {errorContent}");
                    return string.Empty;
                }
            } catch (TaskCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Upload cancelled: timeout exceeded");
                }
                else
                {
                    Console.WriteLine("Upload cancelled: HTTP request timeout");
                }

                throw;
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner error: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Get file SHA value
    /// </summary>
    private async Task<string> GetFileShaAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var requestUri = $"https://api.github.com/repos/{m_Owner}/{m_Repo}/contents/{path}?ref={m_Branch}";
            Console.WriteLine($"Checking file SHA: {requestUri}");

            var response = await m_HttpClient.GetAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var fileInfo = JsonConvert.DeserializeObject<dynamic>(content);
                return fileInfo.sha.ToString();
            }

            // File doesn't exist or other error
            return string.Empty;
        } catch (Exception ex)
        {
            Console.WriteLine($"Error getting file SHA: {ex.Message}");
            // Default to file not existing on error
            return string.Empty;
        }
    }

    /// <summary>
    /// Get raw URL for a file
    /// </summary>
    private string GetRawUrl(string path)
    {
        return $"https://raw.githubusercontent.com/{m_Owner}/{m_Repo}/{m_Branch}/{path}";
    }
}

