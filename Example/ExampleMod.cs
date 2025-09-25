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
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    private void Awake()
    {
        Logger = base.Logger;

        Patch();

        _ = new StatusCommand(); // Initialize command once to register it

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID); // Create Harmony patcher if it doesn't exist

        Logger.LogDebug("Patching...");

        Harmony.PatchAll(); // Apply all Harmony patches (See below StartPatch)

        Logger.LogDebug("Finished patching!");
    }
}

public class StatusCommand : Command // Create command subclass
{
    public override string Name => "Status"; // Set command name (default would have been "StatusCommand")
    public override string Description => "Displays some useful information about the current game"; // Command description for /help

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = null!; // No error is expected, no need to set error string (exceptions are handled automatically)

        var sb = new StringBuilder("Current game: "); // Create StringBuilder, multiple chat messages are not recommended (they could get cut off because of the chat length limit, 4 messages in vanilla)

        // Display information about the lobby you're playing in
        var lobby = GameNetworkManager.Instance.currentLobby;
        // The comment below shuts up my IDE because I want this to be simple and easily readable
        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (lobby == null)
            sb.Append("unknown"); // No lobby set, either you're playing without steam (LAN mode) or the game is bugged
        else
            sb.Append(lobby.Value.GetData("name")); // Get steam lobby name
        sb.Append("\n"); // Add newline after lobby name

        // List all players and if they are dead
        sb.Append("Players:\n");
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            ExampleMod.Logger.LogDebug(
                $"{(player == null ? "null" : $"{player}\n#{player.playerClientId} {player.playerUsername}")}\nisPlayerControlled: {(player == null ? "null" : player.isPlayerControlled)}\nisPlayerDead: {(player == null ? "null" : player.isPlayerDead)}\ndisconnectedMidGame: {(player == null ? "null" : player.disconnectedMidGame)}"
            );

            if (!Utils.IsPlayerControlled(player)) // Check if player object is actually a real player
                continue;

            sb.Append(player!.isPlayerDead ? "<color=#ff0000>" : ""); // Show player name in red if player is dead
            sb.Append($" #{player.playerClientId} {player.playerUsername}"); // Player id and username
            sb.Append(player.isHostPlayerObject ? " (HOST)\n" : "\n"); // If the player is the lobby host, add "(HOST)"
            sb.Append(player.isPlayerDead ? "</color>" : ""); // End of red color (if player is dead)
        }

        // Display time you have spent in the current lobby
        var timePlaying = DateTime.Now - connectTime; // Get the time difference to the time you connected to the current game
        sb.Append(
            $"Time playing: {(int)timePlaying.TotalHours:D2}:{timePlaying.Minutes:D2}:{timePlaying.Seconds:D2}"
        );

        ChatCommandAPI.ChatCommandAPI.Print(sb.ToString()); // Print generated output to chat
        return true;
    }

    // You can use static properties because the command should only ever be instantiated once
    private static DateTime connectTime = new(0); // Initialize date-time to prevent null errors

    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.StartClient))] // On join lobby
    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.StartHost))] // On start hosting lobby
    internal class StartPatch
    {
        // The comment below disables the check for unused functions, because while the function is not called directly, it is used by Harmony at runtime.
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(ref bool __result)
        {
            if (__result) // If connected successfully
                connectTime = DateTime.Now; // Set connect time to current time
        }
    }
}
