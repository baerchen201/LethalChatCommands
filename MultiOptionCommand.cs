using System;
using ChatCommandAPI.Utils;

namespace ChatCommandAPI;

public abstract class MultiOptionCommand<T> : Command
    where T : Enum
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    protected MultiOptionCommand()
    {
        var values = Enum.GetValues(typeof(T));
        if (values.Length == 0)
            throw new ArgumentException(
                $"Invalid enum type {typeof(T).AssemblyQualifiedName} (contains no values)",
                nameof(T)
            );
    }

    public sealed override string[] Syntax
    {
        get
        {
            var values = Enum.GetValues(typeof(T));
            return values.Length <= 5 ? [$"{{ {string.Join(" | ", values)} }}"] : ["[value]"];
        }
    }

    /// <summary>
    ///     The current value of the command
    /// </summary>
    /// <remarks>Use override to point this to a static variable (with your preferred default value)</remarks>
    public virtual T CurrentValue { get; set; }

    public sealed override void Invoke(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            var values = Enum.GetValues(typeof(T));
            Chat.Print(
                $"{typeof(T).Name} values:\n<indent=15%>{string.Join('\n', values)}</indent>\n{Name} value: <color=#{(Enum.IsDefined(typeof(T), CurrentValue) ? "00ffff" : "ff0000")}>{CurrentValue}</color>"
            );
            return;
        }

        if (!Enum.TryParse(typeof(T), args, out var result))
            throw new InvalidArgumentsException();
        var oldValue = CurrentValue;
        CurrentValue = (T)result;
        Changed(oldValue);
    }

    /// <summary>
    /// The callback when the user changes the value
    /// </summary>
    /// <param name="oldValue">The value before the change</param>
    protected virtual void Changed(T oldValue)
    {
        Chat.Print($"{Name} changed to {CurrentValue}");
    }
}
