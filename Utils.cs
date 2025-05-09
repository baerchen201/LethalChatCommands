using System;
using System.Linq;
using GameNetcodeStuff;

namespace ChatCommandAPI;

public static class Utils
{
    public static bool IsPlayerControlled(PlayerControllerB? player) =>
        player != null
        && player is { disconnectedMidGame: false, isTestingPlayer: false }
        && (player.isPlayerControlled || player.isPlayerDead);

    public static PlayerControllerB? GetPlayer(string id, bool strict = false)
    {
        uint playerId;
        PlayerControllerB[] players;
        id = id.Trim();

        switch (id[0])
        {
            case '#':
                if (
                    uint.TryParse(id.TrimStart('#'), out playerId)
                    && playerId < StartOfRound.Instance.allPlayerScripts.Length
                    && IsPlayerControlled(StartOfRound.Instance.allPlayerScripts[playerId])
                )
                    return StartOfRound.Instance.allPlayerScripts[playerId];
                ChatCommandAPI.PrintError("Invalid player id");
                return null;
            case '@':
                players = StartOfRound
                    .Instance.allPlayerScripts.Where(i =>
                        IsPlayerControlled(i)
                        && string.Equals(
                            i.playerUsername,
                            id.TrimStart('@'),
                            StringComparison.CurrentCultureIgnoreCase
                        )
                    )
                    .ToArray();
                if (players.Length == 0 && !strict)
                    players = StartOfRound
                        .Instance.allPlayerScripts.Where(i =>
                            IsPlayerControlled(i)
                            && i.playerUsername.ToLower().StartsWith(id.ToLower().TrimStart('@'))
                        )
                        .ToArray();
                goto players;
        }

        if (
            uint.TryParse(id, out playerId)
            && playerId < StartOfRound.Instance.allPlayerScripts.Length
            && IsPlayerControlled(StartOfRound.Instance.allPlayerScripts[playerId])
        )
            return StartOfRound.Instance.allPlayerScripts[playerId];

        players = StartOfRound
            .Instance.allPlayerScripts.Where(i =>
                IsPlayerControlled(i)
                && string.Equals(i.playerUsername, id, StringComparison.CurrentCultureIgnoreCase)
            )
            .ToArray();
        if (players.Length == 0 && !strict)
            players = StartOfRound
                .Instance.allPlayerScripts.Where(i =>
                    IsPlayerControlled(i) && i.playerUsername.ToLower().StartsWith(id.ToLower())
                )
                .ToArray();
        players:
        if (players.Length > 1)
            ChatCommandAPI.PrintWarning(
                "Multiple players with this name exist, selecting first..."
            );
        if (players.Length > 0)
            return players[0];
        if (id is "@s" or "@me")
            return GameNetworkManager.Instance.localPlayerController;
        ChatCommandAPI.PrintError("No player with username found");
        return null;
    }

    public static bool GetPlayer(string id, out PlayerControllerB player, bool strict = false)
    {
        player = GameNetworkManager.Instance.localPlayerController;
        PlayerControllerB? _player = GetPlayer(id, strict);
        if (_player == null)
            return false;
        player = _player;
        return true;
    }
}
