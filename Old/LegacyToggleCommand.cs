using System;
using ChatCommandAPI.Utils;

namespace ChatCommandAPI.Old;

[Obsolete(LegacyCommand.OBSOLETE_MESSAGE)]
public abstract class LegacyToggleCommand : ToggleCommand
{
    protected LegacyToggleCommand()
    {
        ChatCommandAPI.Logger.LogWarning(
            $"The toggle command {FullName} ({this}) is using the Legacy API. Please upgrade it to the new API"
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

    public sealed override bool CurrentValue
    {
        get => Value;
        set => Value = value;
    }

    public abstract bool Value { get; set; }

    public virtual string EnabledString => "enabled";
    public virtual string DisabledString => "disabled";
    public virtual string ValueString => Value ? EnabledString : DisabledString;

    public virtual string? ToggleDescription => null;

    public sealed override string Description
    {
        get
        {
            var desc = ToggleDescription;
            return string.IsNullOrWhiteSpace(desc) ? ValueString : $"{desc} - {ValueString}";
        }
    }

    public virtual void PrintValue()
    {
        Chat.Print($"{Name} {ValueString}");
    }

    protected sealed override void Changed(bool oldValue)
    {
        PrintValue();
    }
}
