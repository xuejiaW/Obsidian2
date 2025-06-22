using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text;
using System.Diagnostics;
using System.Web;

namespace Obsidian2;

internal class ZhihuPostFormatter
{
    private readonly FileInfo _inputFile;
    private readonly DirectoryInfo _outputDir;
    private string _outputFileName;
    private string _outputFilePath;
    
    // 正则表达式，用于匹配行内公式和块级公式
    private static readonly Regex _inlineLatexRegex = new Regex(@"(?<!\$)\$(?!\$)(.+?)(?<!\$)\$(?!\$)", RegexOptions.Compiled);
    private static readonly Regex _blockLatexRegex = new Regex(@"\$\$([\s\S]*?)\$\$", RegexOptions.Compiled);

    public ZhihuPostFormatter(FileInfo inputFile, DirectoryInfo outputDir)
    {
        _inputFile = inputFile;
        _outputDir = outputDir;
        _outputFileName = Path.GetFileNameWithoutExtension(inputFile.Name) + "_for_zhihu.md";
        _outputFilePath = Path.Combine(outputDir.FullName, _outputFileName);
        
        // 创建输出目录如果不存在
        if (!Directory.Exists(outputDir.FullName))
        {
            Directory.CreateDirectory(outputDir.FullName);
        }
    }
    
    /// <summary>
    /// 设置自定义输出文件路径
    /// </summary>
    public void SetOutputFilePath(string filePath)
    {
        _outputFilePath = filePath;
    }
    
    /// <summary>
    /// 执行格式化过程
    /// </summary>
    public async Task Format()
    {
        Console.WriteLine($"开始处理文件: {_inputFile.FullName}");
        Console.WriteLine($"输出文件: {_outputFilePath}");
        
        // 读取原始Markdown内容
        string content = await File.ReadAllTextAsync(_inputFile.FullName);
        
        // 转换公式
        content = await ConvertMathFormulas(content);
        
        // 写入到输出文件
        await File.WriteAllTextAsync(_outputFilePath, content);
        
        Console.WriteLine($"处理完成，已保存到: {_outputFilePath}");
    }
    
    /// <summary>
    /// 转换Markdown中的数学公式
    /// </summary>
    private Task<string> ConvertMathFormulas(string content)
    {
        int inlineFormulaCount = 0;
        int originalBlockFormulaCount = _blockLatexRegex.Matches(content).Count;
        
        // 转换行内公式为块级公式
        content = _inlineLatexRegex.Replace(content, match =>
        {
            inlineFormulaCount++;
            string formula = match.Groups[1].Value;
            // 将行内公式转换为块级公式
            return $"$${formula}$$";
        });
        
        // 转换后的块级公式总数
        int totalBlockFormulaCount = _blockLatexRegex.Matches(content).Count;
        
        Console.WriteLine($"已转换 {inlineFormulaCount} 个行内公式为块级公式，原有块级公式 {originalBlockFormulaCount} 个，现有块级公式共 {totalBlockFormulaCount} 个");
        return Task.FromResult(content);
    }
}

