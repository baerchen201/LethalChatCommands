using System;
using System.Collections.Generic;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;

namespace ChatCommandAPI.BuiltinCommands;

public class ServerStatus() : ServerCommand(true)
{
    public override string Name => "Status";
    public override string Description => "Displays information about this server";

    public override bool Invoke(
        ref PlayerControllerB caller,
        string[] args,
        Dictionary<string, string> kwargs,
        out string? error
    )
    {
        error = null;
        StringBuilder sb = new StringBuilder("Current game: ");

        var lobby = GameNetworkManager.Instance.currentLobby;
        sb.Append((lobby == null ? "unknown" : lobby.Value.GetData("name")) + "\n");

        sb.Append("Players:\n");
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!Utils.IsPlayerControlled(player))
                continue;

            sb.Append(player!.isPlayerDead ? "<color=#ff0000>" : "");
            sb.Append($" #{player.playerClientId} {player.playerUsername}");
            if (player.isHostPlayerObject)
                sb.Append(" (HOST)");
            sb.Append(player.isPlayerDead ? "</color>" : "");
        }

        if (startTime.Ticks > 0)
        {
            TimeSpan timePlaying = DateTime.Now - startTime;
            sb.Append(
                $"\nGame time: {(int)timePlaying.TotalHours:D2}:{timePlaying.Minutes:D2}:{timePlaying.Seconds:D2}"
            );
        }

        ChatCommandAPI.Print(caller, sb.ToString());
        return true;
    }

    private static DateTime startTime = new(0);

    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.StartHost))]
    internal class StartPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix() => startTime = DateTime.Now;
    }
}
