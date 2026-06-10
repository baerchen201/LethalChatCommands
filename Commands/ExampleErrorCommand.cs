namespace ChatCommandAPI.Commands;

#if DEBUG
public class ExampleErrorCommand : Command
{
    public override string Name => "Error";

    public override string Description => "Echoes arguments to chat as an error";

    public override string[] Syntax => ["..."];

    public override void Invoke(string args)
    {
        throw new CommandException(args.Trim());
    }
}
#endif
