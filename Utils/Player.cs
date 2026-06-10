using System;
using System.Linq;
using System.Text.RegularExpressions;
using GameNetcodeStuff;

namespace ChatCommandAPI.Utils;

public static class Player
{
    private static readonly Regex PLAYER_IDENTIFIER_REGEX = new(
        @"(?:#?(\d+))(?: (.+))?",
        RegexOptions.Compiled
    );

    public static bool TryGetPlayer(int playerId, out PlayerControllerB result)
    {
        result = null!;

        if (playerId < 0)
            return false;

        var controller = StartOfRound.Instance;
        if (controller == null)
            return false;

        var players = controller.allPlayerScripts;
        if (players.Length <= playerId)
            return false;

        result = players[playerId];
        return result.IsPlayerControlled();
    }

    public static bool TryGetPlayer(string identifier, out PlayerControllerB result)
    {
        result = null!;

        if (string.IsNullOrWhiteSpace(identifier))
            return false;
        identifier = identifier.Trim();

        var controller = StartOfRound.Instance;
        if (controller == null)
            return false;

        if (identifier == "@s")
        {
            result = controller.localPlayerController;
            return true;
        }

        var players = controller.allPlayerScripts.Where(i => i.IsPlayerControlled()).ToArray();
        if (players.Length <= 0)
            return false;

        if (identifier == "@r")
        {
            result = players[new Random().Next(players.Length)];
            return true;
        }

        if (
            result = players.FirstOrDefault(i =>
                string.Equals(
                    i.playerUsername.Trim(),
                    identifier,
                    StringComparison.InvariantCulture
                )
            )!
        )
            return true;
        if (
            result = players.FirstOrDefault(i =>
                string.Equals(
                    i.playerUsername.Trim(),
                    identifier,
                    StringComparison.InvariantCultureIgnoreCase
                )
            )!
        )
            return true;

        var match = PLAYER_IDENTIFIER_REGEX.Match(identifier);
        if (match.Success)
        {
            var playerIdGroup = match.Groups[1];
            if (
                playerIdGroup.Success
                && int.TryParse(playerIdGroup.Value, out var playerId)
                && TryGetPlayer(playerId, out result)
            )
                return true;

            var playerNameGroup = match.Groups[2];
            if (playerNameGroup.Success && TryGetPlayer(playerNameGroup.Value, out result))
                return true;
        }

        foreach (var player in players)
        {
            if (
                !player.playerUsername.StartsWith(
                    identifier,
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
                continue;

            result = player;
            return true;
        }

        return false;
    }

    public static PlayerControllerB[] GetPlayers(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return [];

        var controller = StartOfRound.Instance;
        if (controller == null)
            return [];

        identifier = identifier.Trim();

        if (identifier == "@s")
            return [controller.localPlayerController];

        var match = PLAYER_IDENTIFIER_REGEX.Match(identifier);
        if (match.Success)
        {
            var playerIdGroup = match.Groups[1];
            if (
                playerIdGroup.Success
                && int.TryParse(playerIdGroup.Value, out var playerId)
                && TryGetPlayer(playerId, out var _result)
            )
                return [_result];
        }

        var players = controller.allPlayerScripts.Where(i => i.IsPlayerControlled()).ToArray();
        if (players.Length <= 0)
            return [];

        return identifier switch
        {
            "@a" => players,
            "@r" => [players[new Random().Next(players.Length)]],
            _ => players
                .Where(i =>
                    i.playerUsername.Trim()
                        .StartsWith(identifier, StringComparison.InvariantCultureIgnoreCase)
                )
                .ToArray(),
        };
    }

    public static string PlayerString(this PlayerControllerB player)
    {
        return $"#{player.playerClientId} {player.playerUsername}";
    }

    public static bool IsPlayerControlled(this PlayerControllerB player)
    {
        return player
            && player is { disconnectedMidGame: false, isTestingPlayer: false }
            && (player.isPlayerControlled || player.isPlayerDead);
    }

    public sealed class UnknownPlayerException(string identifier)
        : CommandException($"Unknown player '{identifier}'");
}
