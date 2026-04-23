using System.Linq;
using System.Text;
using BepInEx;
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
        if (
            !context.performed
            || text.IsNullOrWhiteSpace()
            || !ChatCommandAPI.Instance.IsCommand(text)
        )
            return true;

        __instance.localPlayer.isTypingChat = false;
        __instance.chatTextField.text = "";
        EventSystem.current.SetSelectedGameObject(null);
        __instance.PingHUDElement(__instance.Chat);
        __instance.typingIndicator.enabled = false;

        ChatCommandAPI.Logger.LogInfo($">> Parsing command: {text}");

        if (
            ChatCommandAPI.Instance.ParseCommand(
                text,
                out var command,
                out var args,
                out var kwargs
            )
        )
        {
            var sb = new StringBuilder($"<< Parsed command: {command}(");
            if (args.Length > 0)
            {
                sb.Append(args.Join());
                if (kwargs.Count > 0)
                    sb.Append(", ");
            }

            sb.Append(kwargs.Select(kvp => $"{kvp.Key}: {kvp.Value}").Join());
            ChatCommandAPI.Logger.LogInfo(sb + ")");

            if (!ChatCommandAPI.Instance.RunCommand(command, args, kwargs, out var error))
            {
                ChatCommandAPI.Logger.LogWarning($"   Error running command: {error ?? "null"}");
                if (error != null)
                    ChatCommandAPI.PrintCommandError(error);
            }

            return false;
        }

        ChatCommandAPI.Logger.LogInfo("<< Invalid command");
        ChatCommandAPI.PrintError((string)"Invalid command");
        return false;
    }
}
