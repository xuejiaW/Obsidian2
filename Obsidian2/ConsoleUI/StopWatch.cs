using System.Diagnostics;
using Spectre.Console;

namespace Obsidian2.ConsoleUI;

public static class StopWatch
{
    public static void CreateStopWatch(string message, Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        action?.Invoke();
        stopwatch.Stop();
        AnsiConsole.MarkupLine($"{message} takes: [italic]{stopwatch.ElapsedMilliseconds}[/] ms");
    }
}