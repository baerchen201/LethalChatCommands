using System.Collections.Generic;
using GameNetcodeStuff;

namespace ChatCommandAPI;

public abstract class ServerCommand
{
    protected ServerCommand()
    {
        ChatCommandAPI.Instance.RegisterServerCommand(this);
    }

    internal ServerCommand(bool builtIn = true)
    {
        if (builtIn)
            ChatCommandAPI.Instance.RegisterBuiltInServerCommand(this);
        else
            ChatCommandAPI.Instance.RegisterServerCommand(this);
    }

    public virtual string Name => GetType().Name;
    public virtual string[] Commands => [Name.ToLower()];
    public virtual string? Description => null;
    public virtual string[]? Syntax => null;
    public virtual bool Hidden => false;

    public abstract bool Invoke(
        ref PlayerControllerB? caller,
        string[] args,
        Dictionary<string, string> kwargs,
        out string? error
    );
}
