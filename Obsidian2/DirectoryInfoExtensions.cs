namespace Obsidian2;

public static class DirectoryInfoExtensions
{
    public static async Task DeepCopy(this DirectoryInfo directory, string destinationDir,
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

        List<string> files = Directory.GetFiles(directory.FullName, "*.*", SearchOption.AllDirectories)
                                      .Where(path => ignoreList == null || !ignoreList.Any(path.Contains)).ToList();


        IEnumerable<Task> tasks = files.Select(file => Task.Run(async () =>
        {
            await CopyFile(file);
        }));

        await Task.WhenAll(tasks);

        Task CopyFile(string file)
        {
            string targetPath = file.Replace(directory.FullName, destinationDir);
            var info = new FileInfo(targetPath);
            targetPath = Path.Join(info.Directory.FullName, info.Name.ToLower());

            File.Copy(file, targetPath, true);
            return Task.CompletedTask;
        }
    }
}