using System;
using System.Linq;
using ChatCommandAPI.Utils;
using GameNetcodeStuff;

namespace ChatCommandAPI.Old;

[Obsolete(LegacyCommand.OBSOLETE_MESSAGE)]
public abstract class LegacyServerCommand : ServerCommand
{
    protected LegacyServerCommand()
    {
        ChatCommandAPI.Logger.LogWarning(
            $"The server command {FullName} ({this}) is using the Legacy API. Please upgrade it to the new API"
        );
        if (Commands.Length <= 0)
            throw new Exception("You need to provide at least one command");
    }

    public virtual string[] Commands => [Name.ToLowerInvariant()];

    public sealed override string Command => Commands[0];

    public sealed override string[] Aliases
    {
        get
        {
            var commands = Commands;
            return commands.Length > 0 ? commands[1..] : [];
        }
    }

    public sealed override void Invoke(PlayerControllerB caller, string args)
    {
        if (!Invoke(caller, Args.Parse(args).ToArray(), out var error))
            throw new CommandException(error!);
    }

    public abstract bool Invoke(PlayerControllerB caller, string[] args, out string? error);
}
