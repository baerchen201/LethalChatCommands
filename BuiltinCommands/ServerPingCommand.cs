using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;

namespace ChatCommandAPI.BuiltinCommands;

public class ServerPing : ServerCommand
{
    public override string Name => "Ping";
    public override string[] Commands => [Name.ToLower()];
    public override string Description => "Displays your latency to the server";

    public override bool Invoke(
        ref PlayerControllerB? caller,
        string[] args,
        Dictionary<string, string> kwargs,
        out string error
    )
    {
        error = "caller is null";
        if (caller == null)
            return false;

        try
        {
            ChatCommandAPI.Print(
                caller,
                $"Latency: {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(
                    caller.actualClientId
                )}ms"
            );
            return true;
        }
        catch (Exception e)
        {
            ChatCommandAPI.Logger.LogError(
                $"Error while requesting ping for player #{caller.playerClientId} {caller.playerUsername} ({caller.actualClientId}): {e}"
            );
            error = "Latency unknown";
            return false;
        }
    }
}
