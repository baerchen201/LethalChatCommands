using ChatCommandAPI.Utils;

namespace ChatCommandAPI.Commands;

#if DEBUG
public class ExampleCommand : Command
{
    public override string Name => "Example command";

    public override string Description => "Echoes arguments to chat";

    public override string Command => "echo";

    public override string[] Syntax => ["..."];

    public override void Invoke(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            Chat.Print("Hello, World!");
        else
            Chat.PrintWarning(args.Trim());
    }
}
#endif
