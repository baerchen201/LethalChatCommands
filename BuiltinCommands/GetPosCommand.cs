using System.Collections.Generic;
using GameNetcodeStuff;

namespace ChatCommandAPI.BuiltinCommands;

public class Position : Command
{
    public override string[] Commands => ["pos", "getpos", "showpos", Name.ToLower()];
    public override string Description => "Shows the current position of [player] or yourself";
    public override string[] Syntax => ["[player]"];

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = null!;

        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (args.Length > 0)
            if (!Utils.GetPlayer(args[0], out player))
                return false;

        error = "This player is dead";
        if (player.isPlayerDead)
            return false;

        ChatCommandAPI.Print(
            $"Position of player <noparse>{player.playerUsername}:\n"
                + player.transform.position
                + (
                    player.transform.parent != null
                        ? $"\nrelative to {player.transform.parent.name}:\n{player.transform.parent.InverseTransformPoint(player.transform.position)}"
                        : ""
                )
                + "</noparse>"
        );
        ChatCommandAPI.Logger.LogInfo(
            $"Position of player"
                + $"{player.playerUsername}: {player.transform.position}{(player.transform.parent != null ? $" - ({player.transform.parent.name})({player.transform.parent.InverseTransformPoint(player.transform.position)})" : "")}"
        );
        return true;
    }
}
