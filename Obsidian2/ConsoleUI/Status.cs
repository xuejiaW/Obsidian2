using Spectre.Console;

namespace Obsidian2.ConsoleUI;

public static class Status
{
    public static void CreateStatus(string message, Action onStatusStart)
    {
        AnsiConsole.Status().Start(message, _ =>
        {
            onStatusStart?.Invoke();
        });
    }
}