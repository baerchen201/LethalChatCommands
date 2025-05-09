using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChatCommandAPI.BuiltinCommands;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Position = ChatCommandAPI.BuiltinCommands.Position;

namespace ChatCommandAPI;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class ChatCommandAPI : BaseUnityPlugin
{
    public static ChatCommandAPI Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    private ConfigEntry<string> commandPrefix = null!;
    public string CommandPrefix => commandPrefix.Value;

    private List<Command> commandList = null!;
    public IReadOnlyList<Command> CommandList => commandList;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        commandPrefix = Config.Bind("General", "CommandPrefix", "/", "Global Command Prefix");

        RegisterCommands();
        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private void RegisterCommands()
    {
        commandList = [];
        _ = new Help();
        _ = new Position();
        _ = new PlayerList();
    }

    public bool RegisterCommand(Command command)
    {
        if (commandList.Any(i => i.GetType() == command.GetType()))
            return false;
        commandList.Add(command);
        return true;
    }

    public bool IsCommand(string command)
    {
        return command.StartsWith(CommandPrefix);
    }

    public bool ParseCommand(
        string input,
        out string command,
        out string[] args,
        out Dictionary<string, string> kwargs
    )
    {
        command = null!;
        args = null!;
        kwargs = null!;

        Match match = (
            new Regex(
                $@"{CommandPrefix}([a-z]+)(?: ([^ =""]+|(?:""[^""]*?"")))*?(?: ([^ =""]+=[^ ""]+|[^ =""]+=""[^""]*?""))*\s*$",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            )
        ).Match(input);
        if (!match.Success)
            return false;

        command = match.Groups[1].Value;
        List<string> _args = [];
        foreach (Capture c in match.Groups[2].Captures)
        {
            _args.Add(c.Value.Replace("\"", ""));
        }
        var _kwargs = new Dictionary<string, string>();
        foreach (Capture c in match.Groups[3].Captures)
        {
            _kwargs.Add(c.Value.Split('=')[0], c.Value.Split('=')[1].Replace("\"", ""));
        }

        args = _args.ToArray();
        kwargs = _kwargs;
        return true;
    }

    public bool RunCommand(
        string command,
        string[] args,
        Dictionary<string, string> kwargs,
        out string? error
    )
    {
        var matches = CommandList
            .Where(i => i.Commands.Select(s => s.ToLower()).Contains(command.ToLower()))
            .ToArray();
        switch (matches.Length)
        {
            case 0:
                error = $"Command {command} not found";
                return false;
            case > 1:
                Logger.LogWarning(
                    $"Command {command} has multiple matches: {matches.Select(i => $"{i.Name} ({i.GetType().Assembly.FullName})").Join()}"
                );
                break;
        }

        try
        {
            return matches[0].Invoke(args, kwargs, out error);
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
            error = e.ToString();
            return false;
        }
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
    internal class ChatCommandPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            ref HUDManager __instance,
            ref InputAction.CallbackContext context
        )
        {
            string text = __instance.chatTextField.text;
            if (!context.performed || text.IsNullOrWhiteSpace() || !Instance.IsCommand(text))
                return true;

            __instance.localPlayer.isTypingChat = false;
            __instance.chatTextField.text = "";
            EventSystem.current.SetSelectedGameObject(null);
            __instance.PingHUDElement(__instance.Chat);
            __instance.typingIndicator.enabled = false;

            Logger.LogInfo($">> Parsing command: {text}");

            if (Instance.ParseCommand(text, out var command, out var args, out var kwargs))
            {
                StringBuilder sb = new StringBuilder($"<< Parsed command: {command}(");
                if (args.Length > 0)
                {
                    sb.Append(args.Join());
                    if (kwargs.Count > 0)
                        sb.Append(", ");
                }
                sb.Append(kwargs.Select(kvp => $"{kvp.Key}: {kvp.Value}").Join());
                Logger.LogInfo(sb + ")");

                if (!Instance.RunCommand(command, args, kwargs, out var error))
                    PrintError($"Error running command: <noparse>{error}</noparse>");
                return false;
            }

            Logger.LogInfo("<< Invalid command");
            PrintError("Invalid command");
            return false;
        }
    }

    private static void UpdateChat()
    {
        HUDManager.Instance.chatText.text = string.Join(
            "\n",
            HUDManager.Instance.ChatMessageHistory
        );
    }

    public static void Print(string text)
    {
        HUDManager.Instance.ChatMessageHistory.Add($"<color=#00ffff>{text}</color>");
        UpdateChat();
    }

    public static void Print(string text, Color color)
    {
        HUDManager.Instance.ChatMessageHistory.Add(
            $"<color=#{(byte)(color.r * 255):h2}{(byte)(color.g * 255):h2}{(byte)(color.b * 255):h2}>{text}</color>"
        );
        UpdateChat();
    }

    public static void Print(string text, Tuple<byte, byte, byte> color)
    {
        HUDManager.Instance.ChatMessageHistory.Add(
            $"<color=#{color.Item1:h2}{color.Item2:h2}{color.Item3:h2}>{text}</color>"
        );
        UpdateChat();
    }

    public static void Print(string text, byte r, byte g, byte b)
    {
        HUDManager.Instance.ChatMessageHistory.Add($"<color=#{r:h2}{g:h2}{b:h2}>{text}</color>");
        UpdateChat();
    }

    public static void PrintWarning(string text)
    {
        HUDManager.Instance.ChatMessageHistory.Add($"<color=#ffff00>{text}</color>");
        UpdateChat();
    }

    public static void PrintError(string text)
    {
        HUDManager.Instance.ChatMessageHistory.Add($"<color=#ff0000>{text}</color>");
        UpdateChat();
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}
