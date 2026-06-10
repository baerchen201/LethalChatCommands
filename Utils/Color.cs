using System.Runtime.CompilerServices;
using UnityEngine;

namespace ChatCommandAPI.Utils;

// fuck dealing with 0-1 color
// all my homies hate 0-1 color
public static class Color
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Drawing.Color ToSystem(this UnityEngine.Color color)
    {
        return ((Color32)color).ToSystem();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Drawing.Color ToSystem(this Color32 color)
    {
        return System.Drawing.Color.FromArgb(color.a, color.r, color.g, color.b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnityEngine.Color ToUnity(this System.Drawing.Color color)
    {
        return new UnityEngine.Color(
            color.R / 255f,
            color.G / 255f,
            color.B / 255f,
            color.A / 255f
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color32 ToUnity32(this System.Drawing.Color color)
    {
        return new Color32(color.R, color.G, color.B, color.A);
    }
}
