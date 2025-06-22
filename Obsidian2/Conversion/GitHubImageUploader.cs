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
{    // GitHub API settings
    private readonly string m_Owner;
    private readonly string m_Repo;
    private readonly string m_Branch;
    private readonly string m_ImageBasePath;
    private readonly HttpClient m_HttpClient;
    private readonly int m_UploadTimeoutSeconds = 60;

    // Caching system
    private readonly HashSet<string> m_UploadedFiles = new();
    private readonly string m_CacheFilePath;
    private Dictionary<string, string> m_FileHashCache = new();
    
    // Logging system
    private enum LogLevel { Error, Warning, Info }
    private LogLevel m_CurrentLogLevel = LogLevel.Info;

    /// <summary>
    /// Creates a GitHub image uploader instance
    /// </summary>
    /// <param name="token">GitHub personal access token</param>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="branch">Branch name</param>
    /// <param name="imageBasePath">Base path for images in the repository</param>
    /// <param name="logLevel">Log level: 0=Error, 1=Warning, 2=Info (default: 2=Info)</param>
    public GitHubImageUploader(string token, string owner, string repo, string branch, string imageBasePath, int logLevel = 2)
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

        // Set log level
        SetLogLevel(logLevel);

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
    /// Log a message with specified log level
    /// </summary>
    private void Log(LogLevel level, string message)
    {
        if (level <= m_CurrentLogLevel)
        {            string prefix = level switch
            {
                LogLevel.Error => "ERROR: ",
                LogLevel.Warning => "WARNING: ",
                LogLevel.Info => "INFO: ",
                _ => string.Empty
            };

            Console.WriteLine($"{prefix}{message}");
        }
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
            Log(LogLevel.Info, $"Upload cache loaded from disk, containing {m_FileHashCache.Count} entries");
        }
        catch (Exception ex)
        {
            Log(LogLevel.Warning, $"Error loading upload cache: {ex.Message}");
            m_FileHashCache = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Save cache to disk
    /// </summary>
    private void SaveCacheToDisk()
    {        try
        {
            string json = JsonConvert.SerializeObject(m_FileHashCache);
            File.WriteAllText(m_CacheFilePath, json);
            Log(LogLevel.Info, $"Cache saved to disk with {m_FileHashCache.Count} entries");
        }
        catch (Exception ex)
        {
            Log(LogLevel.Warning, $"Error saving upload cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Update the file cache with new hash information
    /// </summary>
    private void UpdateCache(string remotePath, string fileHash, string cacheKey)
    {
        m_UploadedFiles.Add(cacheKey);
        m_FileHashCache[remotePath] = fileHash;
        SaveCacheToDisk();
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

            string fileHash = FileUtils.ComputeFileHash(localImagePath);            // Check if record exists in local cache and hash matches
            if (m_FileHashCache.TryGetValue(remotePath, out string cachedHash) && cachedHash == fileHash)
            {
                Log(LogLevel.Info, $"Image {imageFileName} already exists on GitHub with identical content, skipping upload");
                return GetRawUrl(remotePath);
            }

            // Check memory cache (valid only for current session)
            string cacheKey = $"{remotePath}:{fileHash}";
            if (m_UploadedFiles.Contains(cacheKey))
            {
                Log(LogLevel.Info, $"Image {imageFileName} already uploaded in current session, using cached URL");
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
                    Log(LogLevel.Info, $"File {remotePath} already exists on GitHub, recording hash and skipping upload");

                    // Update cache
                    UpdateCache(remotePath, fileHash, cacheKey);                    return GetRawUrl(remotePath);
                }
                
                // Only upload if file doesn't exist
                Log(LogLevel.Info, $"Uploading image: {imageFileName} to path: {remotePath}");

                // Upload new file
                string url = await UploadFileToGitHubAsync(localImagePath, remotePath, null,
                                                           cancellationTokenSource.Token);

                if (!string.IsNullOrEmpty(url))
                {
                    // Update cache
                    UpdateCache(remotePath, fileHash, cacheKey);

                    Log(LogLevel.Info, $"Image {imageFileName} successfully uploaded to GitHub: {url}");
                    return url;
                }

                Log(LogLevel.Warning, $"Failed to upload image {imageFileName}");
                return string.Empty;
            }
            catch (OperationCanceledException)
            {
                Log(LogLevel.Error, $"Upload of image {imageFileName} timed out, operation cancelled");
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, $"Failed to upload image {Path.GetFileName(localImagePath)} to GitHub: {ex.Message}");
            if (ex.InnerException != null)
                Log(LogLevel.Error, $"Detailed error: {ex.InnerException.Message}");

            // Return empty string on upload failure
            return string.Empty;        }
    }
    
    /// <summary>
    /// Directly upload file using GitHub API
    /// </summary>
    private async Task<string> UploadFileToGitHubAsync(string localFilePath, string remotePath, string existingSha,
                                                       CancellationToken cancellationToken)
    {        try
        {
            // Read file content
            byte[] fileContent = await File.ReadAllBytesAsync(localFilePath, cancellationToken);
            var fileInfo = new FileInfo(localFilePath);

            // Prepare request content
            object requestBody = !string.IsNullOrEmpty(existingSha)
                ? new // Update existing file
                {
                    message = $"Update file {Path.GetFileName(remotePath)}",
                    content = Convert.ToBase64String(fileContent),
                    sha = existingSha,
                    branch = m_Branch
                }
                : new // Create new file
                {
                    message = $"Add file {Path.GetFileName(remotePath)}",
                    content = Convert.ToBase64String(fileContent),                    branch = m_Branch
                };

            // Send request
            var requestUri = $"https://api.github.com/repos/{m_Owner}/{m_Repo}/contents/{remotePath}";

            // Upload file
            var jsonContent = JsonContent.Create(requestBody);
            var uploadStartTime = DateTime.Now;            try
            {
                var response = await m_HttpClient.PutAsync(requestUri, jsonContent, cancellationToken);
                var uploadDuration = DateTime.Now - uploadStartTime;

                if (response.IsSuccessStatusCode)
                {
                    Log(LogLevel.Info, $"File upload successful: {Path.GetFileName(remotePath)}");
                    return GetRawUrl(remotePath);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    Log(LogLevel.Error, $"API error: {response.StatusCode} - {errorContent}");
                    return string.Empty;
                }
            }
            catch (TaskCanceledException)
            {
                Log(LogLevel.Error, cancellationToken.IsCancellationRequested
                    ? "Upload cancelled: timeout exceeded"
                    : "Upload cancelled: HTTP request timeout");
                throw;
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, $"Error uploading file: {ex.Message}");
            return string.Empty;        }
    }
    
    /// <summary>
    /// Get file SHA value
    /// </summary>
    private async Task<string> GetFileShaAsync(string path, CancellationToken cancellationToken)
    {        try
        {
            var requestUri = $"https://api.github.com/repos/{m_Owner}/{m_Repo}/contents/{path}?ref={m_Branch}";

            var response = await m_HttpClient.GetAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var fileInfo = JsonConvert.DeserializeObject<dynamic>(content);
                return fileInfo.sha.ToString();
            }

            // File doesn't exist or other error
            return string.Empty;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Warning, $"Error checking file existence: {ex.Message}");
            return string.Empty;        }
    }
    
    /// <summary>
    /// Set the log level for this uploader
    /// </summary>
    /// <param name="level">0=Error, 1=Warning, 2=Info</param>
    public void SetLogLevel(int level)
    {
        m_CurrentLogLevel = level switch
        {
            0 => LogLevel.Error,
            1 => LogLevel.Warning,
            2 => LogLevel.Info,
            _ => LogLevel.Info
        };
    }

    /// <summary>
    /// Get raw URL for a file
    /// </summary>
    private string GetRawUrl(string path)
    {
        return $"https://raw.githubusercontent.com/{m_Owner}/{m_Repo}/{m_Branch}/{path}";
    }
}

