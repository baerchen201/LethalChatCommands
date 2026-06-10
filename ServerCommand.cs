using System.Text.RegularExpressions;
using GameNetcodeStuff;

namespace ChatCommandAPI;

public abstract class ServerCommand : BaseCommand
{
    public ServerCommand()
    {
        ChatCommandAPI.Logger.LogInfo($"Registering server command {FullName}...");
        var displayName = Name;
        if (string.IsNullOrWhiteSpace(displayName))
            throw new InvalidCommandException(nameof(Name), displayName, this);
        var name = Command.ToLowerInvariant();
        if (
            string.IsNullOrWhiteSpace(name)
            || !Regex.Match(name, ChatCommandAPI.COMMAND_NAME_REGEX).Success
        )
            throw new InvalidCommandException(nameof(Command), name, this);

        var commands = ChatCommandAPI.Instance.ServerCommands;
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
        ChatCommandAPI.Instance.serverCommands[name] = this;
        ChatCommandAPI.Logger.LogInfo($"Registered as '{name}'");
    }

    /// <summary>
    ///     The main command function
    /// </summary>
    /// <param name="caller">The player which invoked the command</param>
    /// <param name="args">Command arguments (everything after the command name)</param>
    /// <remarks>
    ///     Use <see cref="Utils.Chat.Print(PlayerControllerB, string, System.Drawing.Color?)" /> for text output, NOT
    ///     <see cref="Utils.Chat.Print(string, System.Drawing.Color?)" />
    /// </remarks>
    public abstract void Invoke(PlayerControllerB caller, string args);
}
