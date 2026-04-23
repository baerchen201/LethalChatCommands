using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using HarmonyLib;
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
                false,
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

    [SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified")]
    public static ClientRpcParams RedirectMessageToClient(ClientRpcParams clientRpcParams)
    {
        if (ChatCommandAPI.targetClientId == null)
            return clientRpcParams;

        return new ClientRpcParams
        {
            Send = { TargetClientIds = [ChatCommandAPI.targetClientId.Value] },
        };
    }
}
