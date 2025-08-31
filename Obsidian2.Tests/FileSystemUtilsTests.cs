using System.Security.Cryptography;
using Obsidian2.Utilities;

namespace Obsidian2.Tests;

[TestFixture]
public class FileSystemUtilsTests
{
    private string _tempDirectory;
    private string _testFilePath;

    [SetUp]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "FileSystemUtilsTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _testFilePath = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(_testFilePath, "Test content for hash calculation");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Test]
    public void CheckDirectory_ExistingDirectory_DoesNotThrow()
    {
        var directoryInfo = new DirectoryInfo(_tempDirectory);

        Assert.DoesNotThrow(() => FileSystemUtils.CheckDirectory(directoryInfo, "test directory"));
    }

    [Test]
    public void CheckDirectory_NonExistingDirectory_ThrowsArgumentException()
    {
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent");
        var directoryInfo = new DirectoryInfo(nonExistentPath);

        var ex = Assert.Throws<ArgumentException>(() => 
            FileSystemUtils.CheckDirectory(directoryInfo, "nonexistent directory"));
        
        Assert.That(ex.Message, Does.Contain("nonexistent directory"));
        Assert.That(ex.Message, Does.Contain(nonExistentPath));
        Assert.That(ex.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void ComputeFileHash_ValidFile_ReturnsConsistentHash()
    {
        string hash1 = FileSystemUtils.ComputeFileHash(_testFilePath);
        string hash2 = FileSystemUtils.ComputeFileHash(_testFilePath);

        Assert.That(hash1, Is.Not.Null);
        Assert.That(hash1, Is.Not.Empty);
        Assert.That(hash1, Is.EqualTo(hash2));
        Assert.That(hash1.Length, Is.EqualTo(32)); // MD5 hash length
        Assert.That(hash1, Does.Match("^[a-f0-9]+$")); // only lowercase hex characters
    }

    [Test]
    public void ComputeFileHash_DifferentContent_ReturnsDifferentHashes()
    {
        var testFile2 = Path.Combine(_tempDirectory, "test2.txt");
        File.WriteAllText(testFile2, "Different content");

        string hash1 = FileSystemUtils.ComputeFileHash(_testFilePath);
        string hash2 = FileSystemUtils.ComputeFileHash(testFile2);

        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void ComputeFileHash_NonExistentFile_ReturnsFallbackHash()
    {
        var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.txt");

        // Should not throw exception, but return fallback hash
        Assert.DoesNotThrow(() =>
        {
            string hash = FileSystemUtils.ComputeFileHash(nonExistentFile);
            // Fallback hash should not be empty
            Assert.That(hash, Is.Not.Null);
            Assert.That(hash, Is.Not.Empty);
        });
    }

    [Test]
    public void ComputeFileHash_ValidFile_MatchesExpectedMD5()
    {
        var knownContent = "Hello, World!";
        var knownFile = Path.Combine(_tempDirectory, "known.txt");
        File.WriteAllText(knownFile, knownContent);
        
        // Calculate expected MD5 hash
        string expectedHash;
        using (var md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(File.ReadAllBytes(knownFile));
            expectedHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        string actualHash = FileSystemUtils.ComputeFileHash(knownFile);

        Assert.That(actualHash, Is.EqualTo(expectedHash));
    }

    [Test]
    public void EnsureDirectoryExists_NewDirectory_CreatesDirectory()
    {
        var newDirPath = Path.Combine(_tempDirectory, "newdir");
        Assert.That(Directory.Exists(newDirPath), Is.False);

        FileSystemUtils.EnsureDirectoryExists(newDirPath);

        Assert.That(Directory.Exists(newDirPath), Is.True);
    }

    [Test]
    public void EnsureDirectoryExists_ExistingDirectory_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => FileSystemUtils.EnsureDirectoryExists(_tempDirectory));
        Assert.That(Directory.Exists(_tempDirectory), Is.True);
    }

    [Test]
    public void EnsureDirectoryExists_NestedDirectory_CreatesAllLevels()
    {
        var nestedPath = Path.Combine(_tempDirectory, "level1", "level2", "level3");
        Assert.That(Directory.Exists(nestedPath), Is.False);

        FileSystemUtils.EnsureDirectoryExists(nestedPath);

        Assert.That(Directory.Exists(nestedPath), Is.True);
        Assert.That(Directory.Exists(Path.Combine(_tempDirectory, "level1")), Is.True);
        Assert.That(Directory.Exists(Path.Combine(_tempDirectory, "level1", "level2")), Is.True);
    }
}
