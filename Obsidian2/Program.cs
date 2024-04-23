using System.CommandLine;

namespace Obsidian2
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.AddCommand(HexoCommand.CreateCommand());
            rootCommand.AddCommand(SetCommand.CreateCommand());
            rootCommand.AddCommand(IgnoreCommand.CreateCommand());

            rootCommand.SetHandler(() => { });

            return await rootCommand.InvokeAsync(args);
        }
    }
}