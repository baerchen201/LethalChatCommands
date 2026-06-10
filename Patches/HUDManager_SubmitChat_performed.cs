using System;
using System.Runtime.CompilerServices;
using ChatCommandAPI.Utils;
using HarmonyLib;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ChatCommandAPI.Patches;

[HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
internal static class HUDManager_SubmitChat_performed
{
    private static bool Prefix(ref HUDManager __instance, ref InputAction.CallbackContext context)
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
                Chat.PrintError(string.IsNullOrWhiteSpace(e.Message) ? $"An unspecified error occurred while executing command '{name}'" : $"An error occurred while executing command '{name}': {e.Message.Trim()}");
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
