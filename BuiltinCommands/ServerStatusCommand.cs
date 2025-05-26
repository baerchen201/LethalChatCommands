using System;
using System.Collections.Generic;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace ChatCommandAPI.BuiltinCommands;

public class ServerStatus : ServerCommand
{
    public override string Name => "Status";
    public override string[] Commands => [Name.ToLower()];
    public override string Description => "Displays information about this server";

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
            sb.Append(player.isHostPlayerObject ? " (HOST)\n" : "\n");
            sb.Append(player.isPlayerDead ? "</color>" : "");
        }

        TimeSpan timePlaying = DateTime.Now - startTime;
        sb.Append(
            $"Game time: {(int)timePlaying.TotalHours:D2}:{timePlaying.Minutes:D2}:{timePlaying.Seconds:D2}"
        );

        ChatCommandAPI.Print(caller, sb.ToString());
        return true;
    }

    private static DateTime startTime = new(0);

    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.StartHost))]
    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.StartClient))]
    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.StartServer))]
    internal class StartPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(ref bool __result)
        {
            if (__result)
                startTime = DateTime.Now;
        }
    }
}
