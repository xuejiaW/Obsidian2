using Obsidian2.Utilities;

namespace Obsidian2.Tests;

[TestFixture]
public class MarkdownUtilsTests
{
    [Test]
    public void ConvertHtmlImgToMarkdown_SimpleHtmlImg_ConvertsToMarkdown()
    {
        var htmlContent = @"<p>Some text <img src=""image.jpg"" alt=""Test Image""> more text</p>";
        
        var result = MarkdownUtils.ConvertHtmlImgToMarkdown(htmlContent);
        
        Assert.That(result, Does.Contain("![Test Image](image.jpg)"));
        Assert.That(result, Does.Not.Contain("<img"));
    }

    [Test]
    public void ConvertHtmlImgToMarkdown_HtmlImgWithSpacesInSrc_EncodesSpaces()
    {
        var htmlContent = @"<img src=""path with spaces/image.jpg"" alt=""Test"">";
        
        var result = MarkdownUtils.ConvertHtmlImgToMarkdown(htmlContent);
        
        Assert.That(result, Does.Contain("![Test](path%20with%20spaces/image.jpg)"));
    }

    [Test]
    public void ConvertHtmlImgToMarkdown_HtmlImgWithoutAlt_UsesEmptyAlt()
    {
        var htmlContent = @"<img src=""image.jpg"">";
        
        var result = MarkdownUtils.ConvertHtmlImgToMarkdown(htmlContent);
        
        Assert.That(result, Does.Contain("![](image.jpg)"));
    }

    [Test]
    public void ConvertHtmlImgToMarkdown_HtmlImgWithoutSrc_KeepsOriginal()
    {
        var htmlContent = @"<img alt=""Test Image"">";
        
        var result = MarkdownUtils.ConvertHtmlImgToMarkdown(htmlContent);
        
        Assert.That(result, Does.Contain(@"<img alt=""Test Image"">"));
    }

    [Test]
    public void ConvertHtmlImgToMarkdown_MultipleImages_ConvertsAll()
    {
        var htmlContent = @"<img src=""image1.jpg"" alt=""First""> and <img src=""image2.png"" alt=""Second"">";
        
        var result = MarkdownUtils.ConvertHtmlImgToMarkdown(htmlContent);
        
        Assert.That(result, Does.Contain("![First](image1.jpg)"));
        Assert.That(result, Does.Contain("![Second](image2.png)"));
        Assert.That(result, Does.Not.Contain("<img"));
    }

    [Test]
    public void FixMarkdownImageLinks_ImageWithSpaces_EncodesSpaces()
    {
        var markdownContent = @"![Alt text](path with spaces/image.jpg)";
        
        var result = MarkdownUtils.FixMarkdownImageLinks(markdownContent);
        
        Assert.That(result, Is.EqualTo("![Alt text](path%20with%20spaces/image.jpg)"));
    }

    [Test]
    public void FixMarkdownImageLinks_ImageWithoutSpaces_RemainsUnchanged()
    {
        var markdownContent = @"![Alt text](path/image.jpg)";
        
        var result = MarkdownUtils.FixMarkdownImageLinks(markdownContent);
        
        Assert.That(result, Is.EqualTo("![Alt text](path/image.jpg)"));
    }

    [Test]
    public void FixMarkdownImageLinks_MultipleImages_FixesAll()
    {
        var markdownContent = @"![First](path with spaces/image1.jpg) and ![Second](another path/image2.png)";
        
        var result = MarkdownUtils.FixMarkdownImageLinks(markdownContent);
        
        Assert.That(result, Does.Contain("![First](path%20with%20spaces/image1.jpg)"));
        Assert.That(result, Does.Contain("![Second](another%20path/image2.png)"));
    }

    [Test]
    public void FormatMarkdownTables_TableWithoutTrailingNewlines_AddsNewlines()
    {
        var markdownContent = "| Header 1 | Header 2 |\n|----------|----------|\n| Cell 1   | Cell 2   |\nNext paragraph";
        
        var result = MarkdownUtils.FormatMarkdownTables(markdownContent);
        
        Assert.That(result, Does.Contain("| Cell 2   |\n\nNext paragraph"));
    }

    [Test]
    public void FormatMarkdownTables_NoTables_RemainsUnchanged()
    {
        var markdownContent = "Just some regular text without tables.";
        
        var result = MarkdownUtils.FormatMarkdownTables(markdownContent);
        
        Assert.That(result, Is.EqualTo(markdownContent));
    }

    [Test]
    public void ConvertHtmlImgToMarkdown_EmptyString_ReturnsEmptyString()
    {
        var result = MarkdownUtils.ConvertHtmlImgToMarkdown("");
        
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void ConvertHtmlImgToMarkdown_NullString_ReturnsEmptyString()
    {
        var result = MarkdownUtils.ConvertHtmlImgToMarkdown(null);
        
        Assert.That(result, Is.EqualTo(""));
    }
}
