using System.Linq;
using System.Text;
using BepInEx;
using GameNetcodeStuff;
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
            || !(__instance.NetworkManager.IsServer || __instance.NetworkManager.IsHost)
            || chatMessage.IsNullOrWhiteSpace()
            || !ChatCommandAPI.Instance.IsServerCommand(chatMessage)
        )
            return true;

        ChatCommandAPI.Logger.LogInfo(
            $">> Parsing server command by player {playerId}: {chatMessage}"
        );
        PlayerControllerB caller = null!;
        if (playerId >= 0 && playerId < StartOfRound.Instance.allPlayerScripts.Length)
            caller = StartOfRound.Instance.allPlayerScripts[playerId];
        ChatCommandAPI.Logger.LogDebug(
            $"   caller: {(caller == null ? "null" : caller.playerUsername)}"
        );
        if (caller == null || !Utils.IsPlayerControlled(caller))
        {
            ChatCommandAPI.Logger.LogWarning(
                $"Server command sent by invalid player {playerId}: {chatMessage}"
            );
            return true;
        }

        if (
            ChatCommandAPI.Instance.ParseCommand(
                chatMessage,
                out var command,
                out var args,
                out var kwargs
            )
        )
        {
            var sb = new StringBuilder(
                $"<< Parsed command: {command}({(caller == null ? "null" : $"#{caller.playerClientId} {caller.playerUsername}")}{(args.Length > 0 || kwargs.Count > 0 ? ", " : "")}"
            );
            if (args.Length > 0)
            {
                sb.Append(args.Join());
                if (kwargs.Count > 0)
                    sb.Append(", ");
            }

            sb.Append(kwargs.Select(kvp => $"{kvp.Key}: {kvp.Value}").Join());
            ChatCommandAPI.Logger.LogInfo(sb + ")");

            if (!ChatCommandAPI.Instance.RunCommand(caller!, command, args, kwargs, out var error))
            {
                ChatCommandAPI.Logger.LogWarning($"   Error running command: {error ?? "null"}");
                if (caller != null && error != null)
                    ChatCommandAPI.PrintCommandError(caller, error);
            }

            return false;
        }

        ChatCommandAPI.Logger.LogInfo("<< Invalid command");
        if (caller != null)
            ChatCommandAPI.PrintError(caller, (string)"Invalid command");
        return false;
    }
}
