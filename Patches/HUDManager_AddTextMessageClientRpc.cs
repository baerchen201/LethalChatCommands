using System.Collections.Generic;
using System.Reflection.Emit;
using ChatCommandAPI.Utils;
using HarmonyLib;
using LethalModUtils;
using Unity.Netcode;

namespace ChatCommandAPI.Patches;

[HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddTextMessageClientRpc))]
internal static class HUDManager_AddTextMessageClientRpc
{
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        return new CodeMatcher(instructions)
            .MatchForward(
                new CodeMatch(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(NetworkBehaviour),
                        nameof(NetworkBehaviour.__endSendClientRpc)
                    )
                )
            )
            .Advance(-1)
            .Insert(
                CodeInstruction.Call(
                    typeof(HUDManager_AddTextMessageClientRpc),
                    nameof(RedirectMessageToClient)
                )
            )
            .InstructionEnumeration();
    }

    private static ClientRpcParams RedirectMessageToClient(ClientRpcParams clientRpcParams)
    {
        return Chat.targetClientId == null
            ? clientRpcParams
            : new ClientRpcParams { Send = { TargetClientIds = [Chat.targetClientId.Value] } };
    }
}
