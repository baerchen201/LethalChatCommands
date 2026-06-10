using System;
using ChatCommandAPI.Utils;
using HarmonyLib;
using Unity.Netcode;

namespace ChatCommandAPI.Patches;

[HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddPlayerChatMessageServerRpc))]
internal static class HUDManager_AddPlayerChatMessageServerRpc
{
    private static bool Prefix(ref HUDManager __instance, ref string chatMessage, ref int playerId)
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
