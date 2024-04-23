namespace Obsidian2;

using System.CommandLine;
using System.Linq;

internal static class IgnoreCommand
{
    internal static Command CreateCommand()
    {
        var ignoreCommand = new Command("ignore", "Ignore files and directories");
        ignoreCommand.AddCommand(CreateAddCommand());
        ignoreCommand.AddCommand(CreateRemoveCommand());
        ignoreCommand.AddCommand(CreateListCommand());

        return ignoreCommand;
    }

    private static Command CreateAddCommand()
    {
        var addCommand = new Command("add", "Ignore files and directories");
        var pathArg = new Argument<string>();
        addCommand.AddArgument(pathArg);
        addCommand.SetHandler(AddIgnorePath, pathArg);

        return addCommand;

        void AddIgnorePath(string path)
        {
            ConfigurationMgr.configuration.ignoresPaths.Add(path);
            ConfigurationMgr.Save();
        }
    }

    private static Command CreateRemoveCommand()
    {
        var removeCommand = new Command("remove", "Ignore files and directories");
        var pathArg = new Argument<string>();
        removeCommand.AddArgument(pathArg);
        removeCommand.SetHandler(RemoveIgnorePath, pathArg);

        return removeCommand;

        void RemoveIgnorePath(string path)
        {
            ConfigurationMgr.configuration.ignoresPaths.Remove(path);
            ConfigurationMgr.Save();
        }
    }

    private static Command CreateListCommand()
    {
        var listCommand = new Command("list", "Ignore files and directories");
        listCommand.SetHandler(PrintAllIgnorePaths);

        return listCommand;

        void PrintAllIgnorePaths()
        {
            Console.WriteLine("Files in the following path will not be processed:");
            ConfigurationMgr.configuration.ignoresPaths.ToList().ForEach(Console.WriteLine);
        }
    }
}