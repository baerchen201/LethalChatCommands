using System;
using System.Linq;
using System.Text.RegularExpressions;
using GameNetcodeStuff;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable UnusedMember.Global

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
        if (id.ToLower() is "@s" or "@me")
            return GameNetworkManager.Instance.localPlayerController;

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

    public static Vector3? ParsePosition(
        Vector3 origin,
        Quaternion rotation,
        string x,
        string y,
        string z
    )
    {
        x = x.Trim();
        y = y.Trim();
        z = z.Trim();
        ChatCommandAPI.Logger.LogDebug($"x:{x} y:{y}, z:{z}");

        if (!x.StartsWith("^") || !y.StartsWith("^") || !z.StartsWith("^"))
            return ParsePosition(origin, x, y, z);

        x = x[1..];
        y = y[1..];
        z = z[1..];

        if (
            (!float.TryParse(x, out var rxf) && x.Length > 0)
            || (!float.TryParse(y, out var ryf) && y.Length > 0)
            || (!float.TryParse(z, out var rzf) && z.Length > 0)
        )
            return null;

        return origin
            + rotation * Vector3.forward * rzf
            + rotation * Vector3.up * ryf
            + rotation * Vector3.right * rxf;
    }

    public static Vector3? ParsePosition(Vector3 origin, string x, string y, string z)
    {
        x = x.Trim();
        y = y.Trim();
        z = z.Trim();

        if (!x.StartsWith("~") || !y.StartsWith("~") || !z.StartsWith("~"))
            return ParsePosition(x, y, z);

        x = x[1..];
        y = y[1..];
        z = z[1..];

        if (
            (!float.TryParse(x, out var rxf) && x.Length > 0)
            || (!float.TryParse(y, out var ryf) && y.Length > 0)
            || (!float.TryParse(z, out var rzf) && z.Length > 0)
        )
            return null;
        return origin + new Vector3(rxf, ryf, rzf);
    }

    public static Vector3? ParsePosition(string x, string y, string z)
    {
        x = x.Trim();
        y = y.Trim();
        z = z.Trim();

        if (
            !float.TryParse(x, out var xf)
            || !float.TryParse(y, out var yf)
            || !float.TryParse(z, out var zf)
        )
            return null;
        return new Vector3(xf, yf, zf);
    }

    public static bool ParsePosition(
        Vector3 origin,
        Quaternion rotation,
        string x,
        string y,
        string z,
        out Vector3 position
    )
    {
        position = default;
        var _position = ParsePosition(origin, rotation, x, y, z);
        if (_position == null)
            return false;
        position = _position.Value;
        return true;
    }

    public static bool ParsePosition(
        Vector3 origin,
        string x,
        string y,
        string z,
        out Vector3 position
    )
    {
        position = default;
        var _position = ParsePosition(origin, x, y, z);
        if (_position == null)
            return false;
        position = _position.Value;
        return true;
    }

    public static bool ParsePosition(string x, string y, string z, out Vector3 position)
    {
        position = default;
        var _position = ParsePosition(x, y, z);
        if (_position == null)
            return false;
        position = _position.Value;
        return true;
    }

    public static Vector3? ParsePosition(Vector3 origin, Quaternion rotation, string input) =>
        !ParseSingleCommaSeparatedPositionString(input, out var x, out var y, out var z)
            ? null
            : ParsePosition(origin, rotation, x, y, z);

    public static Vector3? ParsePosition(Vector3 origin, string input) =>
        !ParseSingleCommaSeparatedPositionString(input, out var x, out var y, out var z)
            ? null
            : ParsePosition(origin, x, y, z);

    public static Vector3? ParsePosition(string input) =>
        !ParseSingleCommaSeparatedPositionString(input, out var x, out var y, out var z)
            ? null
            : ParsePosition(x, y, z);

    public static bool ParsePosition(
        Vector3 origin,
        Quaternion rotation,
        string input,
        out Vector3 position
    )
    {
        position = default;
        return ParseSingleCommaSeparatedPositionString(input, out var x, out var y, out var z)
            && ParsePosition(origin, rotation, x, y, z, out position);
    }

    public static bool ParsePosition(Vector3 origin, string input, out Vector3 position)
    {
        position = default;
        return ParseSingleCommaSeparatedPositionString(input, out var x, out var y, out var z)
            && ParsePosition(origin, x, y, z, out position);
    }

    public static bool ParsePosition(string input, out Vector3 position)
    {
        position = default;
        return ParseSingleCommaSeparatedPositionString(input, out var x, out var y, out var z)
            && ParsePosition(x, y, z, out position);
    }

    internal static bool ParseSingleCommaSeparatedPositionString(
        string input,
        out string x,
        out string y,
        out string z
    )
    {
        x = "0";
        y = "0";
        z = "0";

        Match match = new Regex("^(?:([^,]+),){2}([^,]+)$", RegexOptions.Multiline).Match(input);
        if (!match.Success)
            return false;
        x = match.Groups[1].Captures[0].Value;
        y = match.Groups[1].Captures[1].Value;
        z = match.Groups[2].Captures[0].Value;
        return true;
    }

    public static bool IsInsideFactory(Vector3 position)
    {
        return Object
            .FindObjectsOfType<OutOfBoundsTrigger>()
            .Any(i => position.y < i.gameObject.transform.position.y);
    }
}
