using System.CommandLine;
using System.Linq;

namespace Obsidian2;

public static class ConfigIgnoreCommand
{
    internal static Command CreateCommand()
    {
        var ignoreCommand = new Command("ignore", "Manage ignored paths");
        ignoreCommand.AddCommand(CreateAddCommand());
        ignoreCommand.AddCommand(CreateRemoveCommand());
        ignoreCommand.AddCommand(CreateListCommand());
        return ignoreCommand;
    }

    private static Command CreateAddCommand()
    {
        var addCommand = new Command("add", "Add a path to the ignore list");
        var pathArg = new Argument<string>("path", "Path to ignore");
        addCommand.AddArgument(pathArg);
        addCommand.SetHandler(AddIgnorePath, pathArg);
        return addCommand;

        void AddIgnorePath(string path)
        {
            ConfigurationMgr.configuration.ignoresPaths.Add(path);
            ConfigurationMgr.Save();
            Console.WriteLine($"Added '{path}' to ignore list.");
        }
    }

    private static Command CreateRemoveCommand()
    {
        var removeCommand = new Command("remove", "Remove a path from the ignore list");
        var pathArg = new Argument<string>("path", "Path to remove from ignore list");
        removeCommand.AddArgument(pathArg);
        removeCommand.SetHandler(RemoveIgnorePath, pathArg);
        return removeCommand;

        void RemoveIgnorePath(string path)
        {
            if (ConfigurationMgr.configuration.ignoresPaths.Remove(path))
            {
                ConfigurationMgr.Save();
                Console.WriteLine($"Removed '{path}' from ignore list.");
            }
            else
            {
                Console.WriteLine($"Path '{path}' was not found in ignore list.");
            }
        }
    }

    private static Command CreateListCommand()
    {
        var listCommand = new Command("list", "List all ignored paths");
        listCommand.SetHandler(PrintAllIgnorePaths);
        return listCommand;

        void PrintAllIgnorePaths()
        {
            Console.WriteLine("Files in the following paths will not be processed:");
            var ignorePaths = ConfigurationMgr.configuration.ignoresPaths.ToList();
            if (ignorePaths.Any())
            {
                ignorePaths.ForEach(Console.WriteLine);
            }
            else
            {
                Console.WriteLine("No paths are currently ignored.");
            }
        }
    }
}
