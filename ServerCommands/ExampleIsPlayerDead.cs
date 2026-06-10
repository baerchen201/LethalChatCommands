using ChatCommandAPI.Utils;
using GameNetcodeStuff;

namespace ChatCommandAPI.ServerCommands;

#if DEBUG
public class ExampleIsPlayerDead : ServerCommand
{
    public override string Name => "IsPlayerDead";

    public override string Description => "Shows if a player is dead";

    public override string[] Syntax => ["<player>"];

    public override void Invoke(PlayerControllerB caller, string args)
    {
        if (!Player.TryGetPlayer(args, out var player))
            throw new Player.UnknownPlayerException(args);
        Chat.Print(
            caller,
            player.isPlayerDead
                ? $"Player {player.PlayerString()} is dead."
                : $"Player {player.PlayerString()} is not dead."
        );
    }
}
#endif
