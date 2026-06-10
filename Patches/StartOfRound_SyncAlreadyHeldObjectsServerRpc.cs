using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ChatCommandAPI.Utils;
using HarmonyLib;
using LethalModUtils;
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

    private static void SendWelcomeMessage(ulong clientId)
    {
        var message = ChatCommandAPI.Instance.ServerWelcomeMessage;
        if (
            string.IsNullOrWhiteSpace(message)
            || ChatCommandAPI.Instance.ServerCommands.All(kvp => kvp.Value.Hidden)
        )
            return;
        Chat.Print(clientId, message, Chat.DEFAULT_CHAT_COLOR);
    }
}
