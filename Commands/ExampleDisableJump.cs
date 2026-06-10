using GameNetcodeStuff;
using HarmonyLib;

namespace ChatCommandAPI.Commands;

#if DEBUG
public class ExampleDisableJump : ToggleCommand
{
    public override string Name => "Disable Jump";

    public override string Description => "Disables your jump key";

    public override string Command => "disablejump";

    // It is better to invert this because it gets changed way less
    public override bool CurrentValue
    {
        get => !AllowJump;
        set => AllowJump = !value;
    }

    internal static bool AllowJump = true;

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Jump_performed))]
    private static class PlayerControllerB_Jump_performed
    {
        private static bool Prefix()
        {
            return AllowJump;
        }
    }
}
#endif
