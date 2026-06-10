using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using GameNetcodeStuff;
using UnityEngine;

namespace ChatCommandAPI.Utils;

public static class Pos
{
    private static readonly Regex POSITION_REGEX = new(
        @"(~)?(-?(?:(?:\d+)(?:.\d+)?|(?:\d+)?(?:.\d+)))",
        RegexOptions.Compiled
    );

    public static bool TryParse(
        string pos,
        out Vector3 result,
        Vector3? playerPosition = null,
        Quaternion? cameraAngles = null
    )
    {
        result = default;

        var args = Args.Parse(pos).ToArray();
        if (args.Length != 3)
            return false;

        return TryParse(args[0], args[1], args[2], out result, playerPosition, cameraAngles);
    }

    public static bool TryParse(
        string x,
        string y,
        string z,
        out Vector3 result,
        Vector3? playerPosition = null,
        Quaternion? cameraAngles = null
    )
    {
        result = default;

        float fX,
            fY,
            fZ;

        if (x.StartsWith('^'))
        {
            if (
                !y.StartsWith('^')
                || !z.StartsWith('^')
                || playerPosition == null
                || cameraAngles == null
            )
                return false;

            if (string.IsNullOrWhiteSpace(x = x[1..]))
                fX = 0;
            else if (!float.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out fX))
                return false;

            if (string.IsNullOrWhiteSpace(y = y[1..]))
                fY = 0;
            else if (!float.TryParse(y, NumberStyles.Float, CultureInfo.InvariantCulture, out fY))
                return false;

            if (string.IsNullOrWhiteSpace(z = z[1..]))
                fZ = 0;
            else if (!float.TryParse(z, NumberStyles.Float, CultureInfo.InvariantCulture, out fZ))
                return false;

            result =
                playerPosition.Value
                + cameraAngles.Value * Vector3.forward * fZ
                + cameraAngles.Value * Vector3.up * fY
                + cameraAngles.Value
                    * Vector3.right
                    * -fX /* left, not right */
            ;
            return true;
        }

        if (y.StartsWith('^') || z.StartsWith('^'))
            return false;

        var xRel = false;
        var yRel = false;
        var zRel = false;
        if (x.StartsWith('~'))
        {
            if (playerPosition == null)
                return false;
            xRel = true;
            x = x[1..];
            if (string.IsNullOrWhiteSpace(x))
            {
                fX = 0;
                goto y;
            }
        }

        if (!float.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out fX))
            return false;

        y:
        if (y.StartsWith('~'))
        {
            if (playerPosition == null)
                return false;
            yRel = true;
            y = y[1..];
            if (string.IsNullOrWhiteSpace(y))
            {
                fY = 0;
                goto z;
            }
        }

        if (!float.TryParse(y, NumberStyles.Float, CultureInfo.InvariantCulture, out fY))
            return false;

        z:
        if (z.StartsWith('~'))
        {
            if (playerPosition == null)
                return false;
            zRel = true;
            z = z[1..];
            if (string.IsNullOrWhiteSpace(z))
            {
                fZ = 0;
                goto assemble;
            }
        }

        if (!float.TryParse(z, NumberStyles.Float, CultureInfo.InvariantCulture, out fZ))
            return false;

        assemble:
        result = new Vector3(
            xRel ? fX + playerPosition!.Value.x : fX,
            yRel ? fY + playerPosition!.Value.y : fY,
            zRel ? fZ + playerPosition!.Value.z : fZ
        );
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(string pos, out Vector3 result, PlayerControllerB player)
    {
        return TryParse(
            pos,
            out result,
            player.transform.position,
            player.gameplayCamera.transform.rotation
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(
        string x,
        string y,
        string z,
        out Vector3 result,
        PlayerControllerB player
    )
    {
        return TryParse(
            x,
            y,
            z,
            out result,
            player.transform.position,
            player.gameplayCamera.transform.rotation
        );
    }
}
