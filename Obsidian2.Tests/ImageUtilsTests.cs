using Obsidian2.Utilities;

namespace Obsidian2.Tests;

[TestFixture]
public class ImageUtilsTests
{
    private string _tempDirectory;

    [SetUp]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ImageUtilsTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
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
    public void IsSvgFile_SvgExtension_ReturnsTrue()
    {
        var svgFile = Path.Combine(_tempDirectory, "test.svg");
        File.WriteAllText(svgFile, "<svg></svg>");

        var result = ImageUtils.IsSvgFile(svgFile);

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsSvgFile_NonSvgExtension_ReturnsFalse()
    {
        var pngFile = Path.Combine(_tempDirectory, "test.png");
        File.WriteAllText(pngFile, "fake png content");

        var result = ImageUtils.IsSvgFile(pngFile);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsSvgFile_NonExistentFile_ChecksExtensionOnly()
    {
        var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.svg");

        var result = ImageUtils.IsSvgFile(nonExistentFile);

        // IsSvgFile only checks extension, not file existence
        Assert.That(result, Is.True);
    }

    [TestCase("image.jpg", true)]
    [TestCase("image.jpeg", true)]
    [TestCase("image.png", true)]
    [TestCase("image.gif", true)]
    [TestCase("image.bmp", true)]
    [TestCase("image.svg", true)]
    [TestCase("image.txt", false)]
    [TestCase("image.pdf", false)]
    [TestCase("image", false)]
    public void IsImageFile_VariousExtensions_ReturnsExpectedResult(string fileName, bool expectedResult)
    {
        var result = ImageUtils.IsImageFile(fileName);

        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    public void IsImageFile_NullOrEmptyPath_ReturnsFalse()
    {
        Assert.That(ImageUtils.IsImageFile(null), Is.False);
        Assert.That(ImageUtils.IsImageFile(""), Is.False);
        Assert.That(ImageUtils.IsImageFile("   "), Is.False);
    }

    [Test]
    public void ConvertSVGToPNG_NonExistentSvgFile_ReturnsFalse()
    {
        var nonExistentSvg = Path.Combine(_tempDirectory, "nonexistent.svg");
        var outputPng = Path.Combine(_tempDirectory, "output.png");

        var result = ImageUtils.ConvertSVGToPNG(nonExistentSvg, outputPng, _tempDirectory);

        Assert.That(result, Is.False);
    }

    [Test]
    public void ConvertSVGToPNG_ValidSvgFile_AttemptsConversion()
    {
        // Create a simple SVG file
        var svgFile = Path.Combine(_tempDirectory, "test.svg");
        var svgContent = @"<?xml version=""1.0""?>
<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""100"">
  <circle cx=""50"" cy=""50"" r=""40"" fill=""red""/>
</svg>";
        File.WriteAllText(svgFile, svgContent);
        
        var outputPng = Path.Combine(_tempDirectory, "output.png");

        // Note: This test will return false if Inkscape is not available
        var result = ImageUtils.ConvertSVGToPNG(svgFile, outputPng, _tempDirectory);
        
        // Method should not throw exception regardless of Inkscape availability
        Assert.DoesNotThrow(() => ImageUtils.ConvertSVGToPNG(svgFile, outputPng, _tempDirectory));
    }

    [Test]
    public void ConvertSVGToPNG_ValidParameters_DoesNotThrow()
    {
        var svgFile = Path.Combine(_tempDirectory, "test.svg");
        var svgContent = @"<?xml version=""1.0""?>
<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""100"">
  <rect width=""100"" height=""100"" fill=""blue""/>
</svg>";
        File.WriteAllText(svgFile, svgContent);
        
        var outputPng = Path.Combine(_tempDirectory, "output.png");

        // This should not throw even if Inkscape is not available
        Assert.DoesNotThrow(() => ImageUtils.ConvertSVGToPNG(svgFile, outputPng, _tempDirectory));
    }
}
