using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using GameNetcodeStuff;
using HarmonyLib;

namespace ChatCommandAPI.BuiltinCommands;

public class ServerMods() : ServerCommand(true)
{
    public override string Name => "ServerMods";
    public override string Description => "Shows a list of all mods installed on the server";

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

        ChatCommandAPI.Print(
            caller,
            $"Mods ({Chainloader.PluginInfos.Count}):\n"
                + Chainloader
                    .PluginInfos.Select(i => $"{i.Key} ({i.Value.Metadata.Version})")
                    .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase)
                    .Join(null, "\n")
        );
        return true;
    }
}
