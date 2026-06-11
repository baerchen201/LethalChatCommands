using ChatCommandAPI.Utils;

namespace ChatCommandAPI;

public abstract class ToggleCommand : Command
{
    public sealed override string[] Syntax => ["{ true | false }"];

    /// <summary>
    ///     The current value of the command
    /// </summary>
    /// <remarks>Use override to point this to a static variable (with your preferred default value)</remarks>
    public virtual bool CurrentValue { get; set; }

    public sealed override void Invoke(string args)
    {
        bool result;
        if (string.IsNullOrWhiteSpace(args))
            result = !CurrentValue;
        else if (!bool.TryParse(args, out result))
            throw new InvalidArgumentsException();
        var oldValue = CurrentValue;
        CurrentValue = result;
        Changed(oldValue);
    }

    /// <summary>
    ///     The callback when the user changes the value
    /// </summary>
    /// <param name="oldValue">The value before the change</param>
    protected virtual void Changed(bool oldValue)
    {
        Chat.Print($"{Name} is now {CurrentValue}");
    }
}
