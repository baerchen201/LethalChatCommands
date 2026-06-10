using System;
using System.Globalization;
using ChatCommandAPI.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace ChatCommandAPI.Commands;

#if DEBUG
public class ExampleTeleport : Command
{
    public override string Name => "Teleport";

    public override string Description => "Teleports you to the given position";

    public override string Command => "tp";

    public override string[] Syntax => ["<x> <y> <z>"];

    public override void Invoke(string args)
    {
        var localPlayer = StartOfRound.Instance.localPlayerController;
        if (!Pos.TryParse(args, out var position, localPlayer))
            throw new InvalidArgumentsException();
        Teleport(localPlayer, position);
    }

    public static void Teleport(PlayerControllerB localPlayer, Vector3 position)
    {
        Chat.Print(
            $"Teleported {Math.Round(Vector3.Distance(localPlayer.transform.position, position), 2).ToString(CultureInfo.InvariantCulture)}m"
        );
        localPlayer.TeleportPlayer(position);
    }
}
#endif
