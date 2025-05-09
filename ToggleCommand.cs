using System.Collections.Generic;

namespace ChatCommandAPI;

public abstract class ToggleCommand : Command
{
    public virtual string? ToggleDescription => null;
    public sealed override string Description =>
        ToggleDescription == null ? ValueString : $"{ToggleDescription} - {ValueString}";
    public sealed override string[] Syntax => ["", "{ on | off }"];

    public virtual string EnabledString => "enabled";
    public virtual string DisabledString => "disabled";

    public virtual bool Value { get; set; }
    public virtual string ValueString => Value ? EnabledString : DisabledString;

    public virtual void PrintValue() => ChatCommandAPI.Print($"{Name} {ValueString}");

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = "Invalid argument";
        if (args.Length == 0)
            Value = !Value;
        else
            switch (args[0])
            {
                case "on":
                case "enable":
                    Value = true;
                    break;
                case "off":
                case "disable":
                    Value = false;
                    break;
                default:
                    return false;
            }
        error = null!;
        PrintValue();
        return true;
    }
}
