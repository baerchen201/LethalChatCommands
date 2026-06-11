using System;
using System.Linq;
using ChatCommandAPI.Utils;

namespace ChatCommandAPI.Old;

[Obsolete(OBSOLETE_MESSAGE)]
public abstract class LegacyCommand : Command
{
    internal const string OBSOLETE_MESSAGE =
        "This is provided for easier porting of old mods.\nTHIS IS A TEMPORARY SOLUTION AND IT MAY BE COMPLETELY REMOVED IN FUTURE VERSIONS";

    protected LegacyCommand()
    {
        ChatCommandAPI.Logger.LogWarning(
            $"The command {FullName} ({this}) is using the Legacy API. Please upgrade it to the new API"
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

    public sealed override void Invoke(string args)
    {
        if (!Invoke(Args.Parse(args).ToArray(), out var error))
            throw new CommandException(error!);
    }

    public abstract bool Invoke(string[] args, out string? error);
}
