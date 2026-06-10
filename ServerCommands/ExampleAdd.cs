using ChatCommandAPI.Utils;
using GameNetcodeStuff;

namespace ChatCommandAPI.ServerCommands;

#if DEBUG
public class ExampleAdd : ServerCommand
{
    public override string Name => "Add";

    public override string Description => "Adds numbers";

    public override string[] Syntax => ["[number] ..."];

    public override void Invoke(PlayerControllerB caller, string args)
    {
        var _args = Args.Parse(args);
        var result = 0;
        foreach (var arg in _args)
        {
            if (!int.TryParse(arg, out var i))
                throw new InvalidArgumentsException();
            result += i;
        }

        Chat.Print(caller, result.ToString());
    }
}
#endif
