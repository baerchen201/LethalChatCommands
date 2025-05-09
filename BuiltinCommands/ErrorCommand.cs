using System.Collections.Generic;

namespace ChatCommandAPI.BuiltinCommands;

public class ErrorCommand : Command
{
    public override string Name => "Error";
    public override string Description => "Raises a NullReferenceException, for testing";

    public override bool Hidden => true;

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = null!;

        return Func().EndsWith("a");

        string Func()
        {
            return null!;
        }
    }
}
