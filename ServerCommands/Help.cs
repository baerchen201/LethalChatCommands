using System.Linq;
using System.Text.RegularExpressions;
using ChatCommandAPI.Utils;
using GameNetcodeStuff;

namespace ChatCommandAPI.ServerCommands;

public class Help : ServerCommand
{
    public override string Description =>
        "Displays all available commands or details about a single one";

    public override string[] Aliases => ["?"];
    public override string[] Syntax => ["[command]"];
    public override bool Hidden => true;

    public override void Invoke(PlayerControllerB caller, string args)
    {
        var prefix = ChatCommandAPI.Instance.ServerCommandPrefix;
        if (!string.IsNullOrWhiteSpace(args))
        {
            var match = Regex.Match(args.Trim(), ChatCommandAPI.COMMAND_NAME_REGEX);
            if (!match.Success)
                throw new CommandException("Invalid command name");
            if (
                !ChatCommandAPI.Instance.TryGetServerCommand(
                    match.Value,
                    out var command,
                    out var primaryName
                )
            )
                throw new CommandException($"Unknown command '{match.Value}'");
            Chat.Print(caller, Commands.Help.DetailedHelp(prefix, command, primaryName));
            return;
        }

        var commands = ChatCommandAPI
            .Instance.ServerCommands.Where(kvp => !kvp.Value.Hidden)
            .ToArray();
        Chat.Print(
            caller,
            $"{Commands.Help.DetailedHelp(prefix, this, includeSource: false)}\n{Commands.Help.SEPARATOR}{(commands.Length <= 0 ? "<color=#ff0000>No commands available</color>" : $"Available commands:\n{string.Join('\n', commands.Select(kvp => $"{prefix}{kvp.Key}"))}")}"
        );
    }
}
