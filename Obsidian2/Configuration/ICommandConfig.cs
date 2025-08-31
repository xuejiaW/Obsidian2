namespace Obsidian2;

public interface ICommandConfig
{
    string CommandName { get; }
    void SetDefaults();
    bool Validate(out List<string> errors);
    void DisplayConfiguration();
}
