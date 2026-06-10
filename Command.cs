using System.Text.RegularExpressions;

namespace ChatCommandAPI;

public abstract class Command : BaseCommand
{
    protected Command()
    {
        ChatCommandAPI.Logger.LogInfo($"Registering command {FullName}...");
        var displayName = Name;
        if (string.IsNullOrWhiteSpace(displayName))
            throw new InvalidCommandException(nameof(Name), displayName, this);
        var name = Command.ToLowerInvariant();
        if (
            string.IsNullOrWhiteSpace(name)
            || !Regex.Match(name, ChatCommandAPI.COMMAND_NAME_REGEX).Success
        )
            throw new InvalidCommandException(nameof(Command), name, this);

        var commands = ChatCommandAPI.Instance.Commands;
        if (!commands.TryGetValue(name, out var exCommand))
            goto register;
        ChatCommandAPI.Logger.LogDebug($"{nameof(Command)} '{name}' >> {exCommand}");

        foreach (var alias in Aliases)
        {
            name = alias.ToLowerInvariant();
            if (
                string.IsNullOrWhiteSpace(name)
                || !Regex.Match(name, ChatCommandAPI.COMMAND_NAME_REGEX).Success
            )
                throw new InvalidCommandException(nameof(Aliases), name, this);
            if (!commands.TryGetValue(name, out exCommand))
                goto register;
            ChatCommandAPI.Logger.LogDebug($"{nameof(Aliases)} '{name}' >> {exCommand}");
        }

        name = FullName.ToLowerInvariant();
        if (commands.TryGetValue(name, out exCommand))
        {
            ChatCommandAPI.Logger.LogDebug($"{nameof(FullName)} '{name}' >> {exCommand}");
            throw new DuplicateCommandException(this);
        }

        register:
        ChatCommandAPI.Instance.commands[name] = this;
        ChatCommandAPI.Logger.LogInfo($"Registered as '{name}'");
    }

    /// <summary>
    ///     The main command function
    /// </summary>
    /// <param name="args">Command arguments (everything after the command name)</param>
    /// <remarks>Use <see cref="Utils.Chat.Print(string, System.Drawing.Color?)" /> for text output</remarks>
    public abstract void Invoke(string args);
}
