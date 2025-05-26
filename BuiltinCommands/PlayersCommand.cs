using System.Collections.Generic;
using System.Linq;

namespace ChatCommandAPI.BuiltinCommands;

public class PlayerList : Command
{
    public override string[] Commands => ["players", Name];
    public override string Description => "Lists all active players";

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = "No players connected";
        if (!StartOfRound.Instance.allPlayerScripts.Any(Utils.IsPlayerControlled))
            return false;
        ChatCommandAPI.Print(
            "<noparse>"
                + string.Join(
                    '\n',
                    StartOfRound
                        .Instance.allPlayerScripts.Where(Utils.IsPlayerControlled)
                        .Select(i => $"#{i.playerClientId}: {i.playerUsername}")
                )
                + "</noparse>"
        );
        return true;
    }
}
