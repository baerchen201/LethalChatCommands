using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ChatCommandAPI.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using LethalModUtils;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ChatCommandAPI.Patches;

internal static class BasePatches
{
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddTextMessageClientRpc))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> HUDManager_AddTextMessageClientRpc(
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
            .Insert(CodeInstruction.Call(typeof(BasePatches), nameof(RedirectMessageToClient)))
            .InstructionEnumeration();
    }

    private static ClientRpcParams RedirectMessageToClient(ClientRpcParams clientRpcParams)
    {
        return Chat.targetClientId == null
            ? clientRpcParams
            : new ClientRpcParams { Send = { TargetClientIds = [Chat.targetClientId.Value] } };
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void PlayerControllerB_Update(ref PlayerControllerB __instance) // it pains me to run this every frame but NiceChat does it so i really have no choice here
    {
        var chatTextField = HUDManager.Instance.chatTextField;
        if (chatTextField.text.StartsWith(ChatCommandAPI.Instance.CommandPrefix))
            chatTextField.characterLimit = 0;
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool HUDManager_SubmitChat_performed(
        ref HUDManager __instance,
        ref InputAction.CallbackContext context
    )
    {
        var text = __instance.chatTextField.text;
        if (!context.performed || string.IsNullOrWhiteSpace(text))
            return true;

        if (!ChatCommandAPI.Instance.TryParseCommand(text, out var name, out var args))
            return true;

        if (ChatCommandAPI.Instance.TryGetCommand(name, out var command, out _))
        {
            resetChat(ref __instance);

            ChatCommandAPI.Logger.LogInfo($"Executing command: {name} ({command.FullName})");
            try
            {
                command.Invoke(args);
                ChatCommandAPI.Logger.LogInfo("Command executed successfully");
            }
            catch (CommandException e)
            {
                Chat.PrintError(
                    string.IsNullOrWhiteSpace(e.Message)
                        ? $"An unspecified error occurred while executing command '{name}'"
                        : $"An error occurred while executing command '{name}': {e.Message.Trim()}"
                );
                ChatCommandAPI.Logger.LogError(e);
            }
            catch (Exception e)
            {
                Chat.PrintError(
                    $"An unexpected {e.GetType().Name} error occurred while executing command '{name}'\nCheck the logs for more details"
                );
                ChatCommandAPI.Logger.LogError(e);
            }

            return false;
        }

        Chat.PrintError($"Unknown command: '{name}'");

        if (ChatCommandAPI.Instance.CompatibilityModeEnabled)
            return true;

        resetChat(ref __instance);
        return false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void resetChat(ref HUDManager __instance)
        {
            __instance.localPlayer.isTypingChat = false;
            __instance.chatTextField.text = "";
            EventSystem.current.SetSelectedGameObject(null);
            __instance.PingHUDElement(__instance.Chat);
            __instance.typingIndicator.enabled = false;
        }
    }
}
