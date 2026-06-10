namespace ChatCommandAPI.Commands;

#if DEBUG
public class ExampleExceptionCommand : Command
{
    public override string Name => "Exception";

    public override string Description => "Throws a natural exception";

    public override void Invoke(string args)
    {
        _ = ((string)null!).Trim();
    }
}
#endif
