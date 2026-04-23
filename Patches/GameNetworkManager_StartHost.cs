using System;
using ChatCommandAPI.BuiltinCommands;
using HarmonyLib;

namespace ChatCommandAPI.Patches;

[HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.StartHost))]
internal static class GameNetworkManager_StartHost
{
    private static void Postfix()
    {
        ServerStatus.startTime = DateTime.Now;
        ChatCommandAPI.confirmationRequests.Clear();
    }
}
