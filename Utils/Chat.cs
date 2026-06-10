using System.Runtime.CompilerServices;
using GameNetcodeStuff;

namespace ChatCommandAPI.Utils;

public static class Chat
{
    public static readonly System.Drawing.Color DEFAULT_CHAT_COLOR = System.Drawing.Color.FromArgb(
        0x70,
        0x69,
        0xFF
    );

    internal static ulong? targetClientId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string Clean(string message)
    {
        return message.Replace("\0", "\\0"); // the chat gets weird w/o this
    }

    private static void Print(string message, System.Drawing.Color color)
    {
        HUDManager.Instance.AddChatMessage(
            $"<color=#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}>{Clean(message)}</color>"
        );
    }

    internal static void Print(ulong targetClientId, string message, System.Drawing.Color color)
    {
        // no thread safety because fuck you
        Chat.targetClientId = targetClientId;
        HUDManager.Instance.AddTextMessageClientRpc(
            $"<color=#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}>{Clean(message)}</color>"
        );
        Chat.targetClientId = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Print(PlayerControllerB target, string message, System.Drawing.Color color)
    {
        Print(target.actualClientId, message, color);
    }

    private static void PrintGlobal(string message, System.Drawing.Color color)
    {
        HUDManager.Instance.AddTextMessageServerRpc(
            $"<color=#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}>{Clean(message)}</color>"
        );
    }

    public static void Print(string message, System.Drawing.Color? color = null)
    {
        ChatCommandAPI.Logger.LogInfo($"Print: {message}");
        Print(message, color ?? ChatCommandAPI.Instance.DefaultMessageColor);
    }

    public static void PrintWarning(string message)
    {
        ChatCommandAPI.Logger.LogWarning($"PrintWarning: {message}");
        Print(message, System.Drawing.Color.Yellow);
    }

    public static void PrintError(string message)
    {
        ChatCommandAPI.Logger.LogError($"PrintError: {message}");
        Print(message, System.Drawing.Color.Red);
    }

    public static void Print(
        PlayerControllerB target,
        string message,
        System.Drawing.Color? color = null
    )
    {
        ChatCommandAPI.Logger.LogInfo($"Print({target.PlayerString()}): {message}");
        Print(target, message, color ?? ChatCommandAPI.Instance.DefaultMessageColor);
    }

    public static void PrintWarning(PlayerControllerB target, string message)
    {
        ChatCommandAPI.Logger.LogWarning($"PrintWarning({target.PlayerString()}): {message}");
        Print(target, message, System.Drawing.Color.Yellow);
    }

    public static void PrintError(PlayerControllerB target, string message)
    {
        ChatCommandAPI.Logger.LogError($"PrintError({target.PlayerString()}): {message}");
        Print(target, message, System.Drawing.Color.Red);
    }

    public static void PrintGlobal(string message, System.Drawing.Color? color = null)
    {
        ChatCommandAPI.Logger.LogInfo($"PrintGlobal: {message}");
        PrintGlobal(message, color ?? ChatCommandAPI.Instance.DefaultMessageColor);
    }

    public static void PrintWarningGlobal(string message)
    {
        ChatCommandAPI.Logger.LogWarning($"PrintWarningGlobal: {message}");
        PrintGlobal(message, System.Drawing.Color.Yellow);
    }

    public static void PrintErrorGlobal(string message)
    {
        ChatCommandAPI.Logger.LogError($"PrintErrorGlobal: {message}");
        PrintGlobal(message, System.Drawing.Color.Red);
    }

    /// <summary>
    ///     Escapes TMP rich text tags
    /// </summary>
    /// <param name="message">The message to escape</param>
    /// <returns>Rich-text free message</returns>
    /// <remarks>Replaces &lt; and &gt; with 0xFF1C '＜' and 0xFF1E '＞' </remarks>
    public static string Escape(string message)
    {
        return message.Replace('<', '\uFF1C').Replace('>', '\uFF1E');
    }
}
