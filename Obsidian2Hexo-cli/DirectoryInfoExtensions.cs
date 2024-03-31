﻿namespace Obsidian2Hexo;

public static class DirectoryInfoExtensions
{
    public static void DeepCopy(this DirectoryInfo directory, string destinationDir,
                                List<string> ignoreList = null)
    {
        if (string.IsNullOrEmpty(destinationDir)) return;
        if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);

        Directory.GetDirectories(directory.FullName, "*", SearchOption.AllDirectories)
                 .Where(dir => ignoreList == null || !ignoreList.Any(dir.Contains)).ToList()
                 .ForEach(dir =>
                  {
                      Directory.CreateDirectory(dir.Replace(directory.FullName, destinationDir));
                  });

        Directory.GetFiles(directory.FullName, "*.*", SearchOption.AllDirectories)
                 .Where(path => ignoreList == null || !ignoreList.Any(path.Contains)).ToList()
                 .ForEach(file =>
                  {
                      string targetPath = file.Replace(directory.FullName, destinationDir);
                      var info = new FileInfo(targetPath);
                      targetPath = Path.Join(info.Directory.FullName, info.Name.ToLower());

                      File.Copy(file, targetPath, true);
                  });
    }
}