using Spectre.Console;

namespace Obsidian2Hexo.ConsoleUI;

public static class Progress
{
    public static async Task CreateProgress(string message, Func<ProgressTask, Task> onProgressStart)
    {
        await AnsiConsole.Progress().StartAsync(async ctx =>
        {
            ProgressTask task = ctx.AddTask(message);

            await onProgressStart(task);
        });
    }
}