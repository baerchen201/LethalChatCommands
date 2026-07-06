using GameNetcodeStuff;
using HarmonyLib;

namespace ChatCommandAPI.Patches;

[HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
internal static class PlayerControllerB_Update
{
    [HarmonyPriority(-1)] // no idea if this is needed but if it aint broke dont fix it
    private static void Postfix(ref PlayerControllerB __instance) // it pains me to run this every frame but NiceChat does it so i really have no choice here
    {
        var chatTextField = HUDManager.Instance.chatTextField;
        if (chatTextField.text.StartsWith(ChatCommandAPI.Instance.CommandPrefix))
            chatTextField.characterLimit = 0;
    }
}
