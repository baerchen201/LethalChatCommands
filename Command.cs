using System.Collections.Generic;

namespace ChatCommandAPI;

public abstract class Command
{
    protected Command()
    {
        ChatCommandAPI.Instance.RegisterCommand(this);
    }

    public virtual string Name => this.GetType().Name;
    public virtual string[] Commands => [Name.ToLower()];
    public virtual string? Description => null;
    public virtual string[]? Syntax => null;
    public virtual bool Hidden => false;

    public abstract bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error);
}
