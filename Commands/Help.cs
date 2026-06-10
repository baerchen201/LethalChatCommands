using System.Linq;
using System.Text.RegularExpressions;
using ChatCommandAPI.Utils;

namespace ChatCommandAPI.Commands;

public class Help : Command
{
    internal const string SEPARATOR = "<color=#00FFFF>===============</color>\n";

    public override string Description =>
        "Displays all available commands or details about a single one";

    public override string[] Aliases => ["?"];
    public override string[] Syntax => ["[command]"];
    public override bool Hidden => true;

    public override void Invoke(string args)
    {
        var prefix = ChatCommandAPI.Instance.CommandPrefix;
        if (!string.IsNullOrWhiteSpace(args))
        {
            var match = Regex.Match(args.Trim(), ChatCommandAPI.COMMAND_NAME_REGEX);
            if (!match.Success)
                throw new CommandException("Invalid command name");
            if (
                !ChatCommandAPI.Instance.TryGetCommand(
                    match.Value,
                    out var command,
                    out var primaryName
                )
            )
                throw new CommandException($"Unknown command '{match.Value}'");
            Chat.Print(DetailedHelp(prefix, command, primaryName));
            return;
        }

        var commands = ChatCommandAPI.Instance.Commands.Where(kvp => !kvp.Value.Hidden).ToArray();
        Chat.Print(
            $"{DetailedHelp(prefix, this, includeSource: false)}\n{SEPARATOR}{(commands.Length <= 0 ? "<color=#ff0000>No commands available</color>" : $"Available commands:\n{string.Join('\n', commands.Select(kvp => $"{prefix}{kvp.Key}"))}")}"
        );
    }

    /// <summary>
    ///     Creates a string describing a command
    /// </summary>
    /// <param name="prefix">Command prefix</param>
    /// <param name="command">The command to describe</param>
    /// <param name="primaryName">The alias to use for the command (default = command.Name)</param>
    /// <param name="includeSource">Includes a footnote about the command source (Assembly)</param>
    /// <returns>Description string formatted with TMP rich text tags</returns>
    public static string DetailedHelp(
        char prefix,
        BaseCommand command,
        string primaryName = null!,
        bool includeSource = true
    )
    {
        if (string.IsNullOrWhiteSpace(primaryName))
            primaryName = command.Command.ToLowerInvariant();
        var description = command.Description;
        var syntax = command.Syntax;
        return $"{command.Name}{(string.IsNullOrWhiteSpace(description) ? null : $" - {description}")}\n{string.Join('\n', (syntax.Length <= 0 ? [null!] : syntax).Select(i => $"<color=#ffff00>{prefix}{primaryName}</color>{(string.IsNullOrWhiteSpace(i) ? null : $" <color=#dddd00><noparse>{i}</noparse></color>")}"))}{(includeSource ? $"\n<color=#888888><size=60%><b>{command.FullName}</b> in {command.GetType().Assembly.GetName().Name}</size></color>" : null)}";
    }
}
