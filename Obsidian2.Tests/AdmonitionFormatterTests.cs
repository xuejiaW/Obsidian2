namespace Obsidian2.Tests;

public class AdmonitionFormatterTests
{
    [SetUp]
    public void Setup() { }

    [Test]
    public void TestAdmonition_MkDocs_GetExpectResult()
    {
        string resourcePath = GetTestResourcesPath("MkDocsStyle.md");
        string expectPath = GetTestResourcesPath("MkDocsStyle_ExpectResult.md");
        Assert.That(File.Exists(resourcePath), "Resource file not found");
        Assert.That(File.Exists(expectPath), "Expect file not found");

        string resourceContent = File.ReadAllText(resourcePath);
        string expectContent = File.ReadAllText(expectPath);

        string formatResult = AdmonitionsFormatter.FormatMkDocsStyle2ButterflyStyle(resourceContent);
        
        expectContent = FormatTestContent(expectContent);
        formatResult = FormatTestContent(formatResult);
        
        Console.Write(formatResult);

        Assert.That(formatResult, Is.EqualTo(expectContent), "The result is not as expected");


        string GetTestResourcesPath(string fileName)
        {
            string baseDictionary = AppDomain.CurrentDomain.BaseDirectory;
            string relativePath = $"../../../TestResources/Admonition/{fileName}";
            return Path.GetFullPath(Path.Combine(baseDictionary, relativePath));
        }
        
        string FormatTestContent(string content)
        {
            string ret = content.Replace("\r\n", "\n");
            if (ret.EndsWith("\n")) ret = ret.Substring(0, ret.Length - 1);
            return ret;
        }
    }
}