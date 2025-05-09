using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using ChatCommandAPI;
using HarmonyLib;
using Unity.Netcode;

namespace ExampleMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("baer1.ChatCommandAPI", BepInDependency.DependencyFlags.HardDependency)]
public class ExampleMod : BaseUnityPlugin
{
    public static ExampleMod Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        Patch();

        _ = new StatusCommand();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}

public class StatusCommand : Command
{
    public override string Name => "Status";
    public override string? Description =>
        "Displays some useful information about the current game";

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = null!;
        StringBuilder sb = new StringBuilder($"Current game: ");
        var lobby = GameNetworkManager.Instance.currentLobby;
        if (lobby == null)
            sb.Append("unknown");
        else
            sb.Append(lobby.Value.GetData("name"));
        sb.Append("\n");

        sb.Append("Players:\n");
        foreach (var i in StartOfRound.Instance.allPlayerScripts)
        {
            ExampleMod.Logger.LogDebug(
                $"{(i == null ? "null" : $"{i}\n#{i.playerClientId} {i.playerUsername}")}\nisPlayerControlled: {(i == null ? "null" : i.isPlayerControlled)}\nisPlayerDead: {(i == null ? "null" : i.isPlayerDead)}\ndisconnectedMidGame: {(i == null ? "null" : i.disconnectedMidGame)}"
            );
            if (i == null || !Utils.IsPlayerControlled(i))
                continue;
            sb.Append(i.isPlayerDead ? "<color=#ff0000>" : "");
            sb.Append($" #{i.playerClientId} {i.playerUsername}");
            sb.Append(i.isHostPlayerObject ? " (HOST)\n" : "\n");
            sb.Append(i.isPlayerDead ? "</color>" : "");
        }

        TimeSpan timePlaying = DateTime.Now - _connectTime;
        ExampleMod.Logger.LogDebug(
            $"timePlaying: {timePlaying}\nconnectTime: {_connectTime}\nNow: {DateTime.Now}"
        );
        ExampleMod.Logger.LogDebug(timePlaying.Seconds);
        sb.Append(
            $"Time playing: {(int)timePlaying.TotalHours:D2}:{timePlaying.Minutes:D2}:{timePlaying.Seconds:D2}"
        );

        Print(sb.ToString());
        return true;
    }

    private static DateTime _connectTime;

    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.StartClient))]
    internal class ClientStartPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (__result)
                _connectTime = DateTime.Now;
        }
    }

    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.StartHost))]
    internal class HostStartPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (__result)
                _connectTime = DateTime.Now;
        }
    }

    private static void Print(string value) => ChatCommandAPI.ChatCommandAPI.Print(value);
}
