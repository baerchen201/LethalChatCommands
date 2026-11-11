using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ChatCommandAPI.Utils;
using HarmonyLib;
using LethalModUtils;
using Unity.Netcode;
using UnityEngine;

namespace ChatCommandAPI.Patches;

internal static class ServerCommandPatches
{
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncAlreadyHeldObjectsServerRpc))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> StartOfRound_SyncAlreadyHeldObjectsServerRpc(
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
                CodeInstruction.Call(typeof(ServerCommandPatches), nameof(SendWelcomeMessage))
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

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddPlayerChatMessageServerRpc))]
    [HarmonyPrefix]
    private static bool HUDManager_AddPlayerChatMessageServerRpc(
        ref HUDManager __instance,
        ref string chatMessage,
        ref int playerId
    )
    {
        if (
            __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute
            || !__instance.IsServer
            || string.IsNullOrWhiteSpace(chatMessage)
        )
            return true;

        if (!Player.TryGetPlayer(playerId, out var caller))
        {
            ChatCommandAPI.Logger.LogWarning($"Chat message sent by invalid player #{playerId}");
            return true;
        }

        if (!ChatCommandAPI.Instance.TryParseServerCommand(chatMessage, out var name, out var args))
            return true;

        if (ChatCommandAPI.Instance.TryGetServerCommand(name, out var command, out _))
        {
            ChatCommandAPI.Logger.LogInfo(
                $"Executing server command as '{caller.PlayerString()}': {name} ({command.FullName})"
            );
            try
            {
                command.Invoke(caller, args);
                ChatCommandAPI.Logger.LogInfo("Command executed successfully");
            }
            catch (CommandException e)
            {
                Chat.PrintError(
                    caller,
                    string.IsNullOrWhiteSpace(e.Message)
                        ? $"An unspecified error occurred while executing command '{name}'"
                        : $"An error occurred while executing command '{name}': {e.Message.Trim()}"
                );
                ChatCommandAPI.Logger.LogError(e);
            }
            catch (Exception e)
            {
                Chat.PrintError(
                    caller,
                    $"An unexpected {e.GetType().Name} error occurred while executing command '{name}'"
                );
                Chat.PrintWarning(
                    $"An unexpected {e.GetType().Name} error occurred while executing command '{name}' as {caller.PlayerString()}\nCheck the logs for more details"
                );
                ChatCommandAPI.Logger.LogError(e);
            }

            return false;
        }

        Chat.PrintError(caller, $"Unknown command: '{name}'");
        return false;
    }
}
