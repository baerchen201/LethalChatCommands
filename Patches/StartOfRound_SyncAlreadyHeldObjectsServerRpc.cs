using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ChatCommandAPI.Patches;

[HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncAlreadyHeldObjectsServerRpc))]
internal static class StartOfRound_SyncAlreadyHeldObjectsServerRpc
{
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        return new CodeMatcher(instructions)
            .MatchForward(
                false,
                new CodeMatch(
                    OpCodes.Call,
                    AccessTools.Method(typeof(Debug), nameof(Debug.Log), [typeof(string)])
                )
            )
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(
                    typeof(StartOfRound_SyncAlreadyHeldObjectsServerRpc),
                    nameof(SendWelcomeMessage)
                )
            )
            .InstructionEnumeration();
    }

    internal static void SendWelcomeMessage(ulong clientId)
    {
        if (
            ChatCommandAPI.Instance.ServerWelcomeMessage == null
            || ChatCommandAPI.Instance.ServerWelcomeMessage.IsNullOrWhiteSpace()
            || ChatCommandAPI.Instance.serverCommandList.All(i => i.Hidden)
        )
            return;

        ChatCommandAPI.targetClientId = clientId;
        HUDManager.Instance.AddTextMessageClientRpc(
            $"<color=#7069ff>{ChatCommandAPI.Instance.ServerWelcomeMessage}</color>"
        );
        ChatCommandAPI.targetClientId = null;
    }
}
