using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChatCommandAPI.BuiltinCommands;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using Position = ChatCommandAPI.BuiltinCommands.Position;

namespace ChatCommandAPI;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class ChatCommandAPI : BaseUnityPlugin
{
    internal static ConfirmationRequest? confirmationRequest;
    internal static Dictionary<ulong, ConfirmationRequest> confirmationRequests = [];

    internal static ulong? targetClientId;
    internal ConfigEntry<bool> builtInCommands = null!;
    private List<ServerCommand> builtInServerCommandList = null!;
    private List<Command> commandList = null!;

    private ConfigEntry<string> commandPrefix = null!;

    private ConfigEntry<bool> enableServerMode = null!;
    internal List<ServerCommand> serverCommandList = null!;
    private ConfigEntry<string> serverCommandPrefix = null!;
    private ConfigEntry<string> serverWelcomeMessage = null!;
    public static ChatCommandAPI Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }
    public string CommandPrefix => commandPrefix.Value;
    public IReadOnlyList<Command> CommandList => commandList;
    public bool EnableServerMode => enableServerMode.Value;
    public string ServerCommandPrefix => serverCommandPrefix.Value;

    public IReadOnlyList<ServerCommand> ServerCommandList =>
        builtInCommands.Value
            ? builtInServerCommandList.Concat(serverCommandList).ToList()
            : serverCommandList;

    public string? ServerWelcomeMessage =>
        serverWelcomeMessage.Value.IsNullOrWhiteSpace()
            ? null
            : string.Format(serverWelcomeMessage.Value.Trim(), ServerCommandPrefix);

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        commandPrefix = Config.Bind("General", "CommandPrefix", "/", "Client Command Prefix");
        enableServerMode = Config.Bind(
            "Server",
            "EnableServerMode",
            true,
            "Whether to enable server functionality (if you are host, anyone can use commands, even without the mods)"
        );
        serverCommandPrefix = Config.Bind(
            "Server",
            "ServerCommandPrefix",
            "!",
            "Sever Command Prefix"
        );
        builtInCommands = Config.Bind(
            "Server",
            "BuiltInCommands",
            true,
            "Enables 'status' and 'servermods' commands"
        );
        serverWelcomeMessage = Config.Bind(
            "Server",
            "ServerWelcomeMessage",
            "This server has available commands.\nType {0}help for more information",
            "A welcome message that is displayed to any player that joins (clear to disable). {0} is replaced with ServerCommandPrefix"
        );

        RegisterCommands();
        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        return;

        void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);
            Logger.LogDebug("Patching...");
            Harmony.PatchAll();
            Logger.LogDebug("Finished patching!");
        }
    }

    private void RegisterCommands()
    {
        commandList = [];
        _ = new Help();
        _ = new Position();
        _ = new PlayerList();
        _ = new Confirm();
        _ = new Deny();
        _ = new ErrorCommand();

        serverCommandList = [];
        builtInServerCommandList = [];
        _ = new ServerHelp();
        _ = new ServerStatus();
        _ = new ServerMods();
        _ = new ServerConfirm();
        _ = new ServerDeny();
    }

    public bool RegisterCommand(Command command)
    {
        if (commandList.Any(i => i.GetType() == command.GetType()))
            return false;
        commandList.Add(command);
        return true;
    }

    public bool RegisterServerCommand(ServerCommand command)
    {
        if (serverCommandList.Any(i => i.GetType() == command.GetType()))
            return false;
        serverCommandList.Add(command);
        return true;
    }

    public bool RegisterBuiltInServerCommand(ServerCommand command)
    {
        if (builtInServerCommandList.Any(i => i.GetType() == command.GetType()))
            return false;
        builtInServerCommandList.Add(command);
        return true;
    }

    public bool IsCommand(string command)
    {
        return command.StartsWith(CommandPrefix);
    }

    public bool IsServerCommand(string command)
    {
        return command.StartsWith(ServerCommandPrefix);
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

        var match = new Regex(
            $"""(?:{Regex.Escape(CommandPrefix)}|{Regex.Escape(ServerCommandPrefix)})([a-z]+)(?: ([^ ="]+|(?:"[^"]*?")))*?(?: ([^ ="]+=[^ "]+|[^ ="]+="[^"]*?"))*\s*$""",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        ).Match(input);
        if (!match.Success)
            return false;

        command = match.Groups[1].Value;
        List<string> _args = [];
        foreach (Capture c in match.Groups[2].Captures)
            _args.Add(c.Value.Replace("\"", ""));
        var _kwargs = new Dictionary<string, string>();
        foreach (Capture c in match.Groups[3].Captures)
            _kwargs.Add(c.Value.Split('=')[0], c.Value.Split('=')[1].Replace("\"", ""));

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

    public bool RunCommand(
        PlayerControllerB caller,
        string command,
        string[] args,
        Dictionary<string, string> kwargs,
        out string? error
    )
    {
        var matches = ServerCommandList
            .Where(i => i.Commands.Select(s => s.ToLower()).Contains(command.ToLower()))
            .ToArray();
        switch (matches.Length)
        {
            case 0:
                error = $"Command {command} not found";
                return false;
            case > 1:
                Logger.LogWarning(
                    $"Server command {command} has multiple matches: {matches.Select(i => $"{i.Name} ({i.GetType().Assembly.FullName})").Join()}"
                );
                break;
        }

        try
        {
            return matches[0].Invoke(caller, args, kwargs, out error);
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
            error = e.ToString();
            return false;
        }
    }

    public static void AskConfirm(string action, Action<bool> callback)
    {
        confirmationRequest = new ConfirmationRequest { action = action, callback = callback };
    }

    public static void AskConfirm(Action<bool> callback)
    {
        confirmationRequest = new ConfirmationRequest { callback = callback };
    }

    public static Task<bool> AskConfirmAsync(string action)
    {
        var tcs = new TaskCompletionSource<bool>();
        confirmationRequest = new ConfirmationRequest { action = action, callback = tcs.SetResult };
        return tcs.Task;
    }

    public static Task<bool> AskConfirmAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        confirmationRequest = new ConfirmationRequest { callback = tcs.SetResult };
        return tcs.Task;
    }

    public static void AskConfirm(PlayerControllerB caller, string action, Action<bool> callback)
    {
        confirmationRequests[caller.actualClientId] = new ConfirmationRequest
        {
            action = action,
            callback = callback,
        };
    }

    public static void AskConfirm(PlayerControllerB caller, Action<bool> callback)
    {
        confirmationRequests[caller.actualClientId] = new ConfirmationRequest
        {
            callback = callback,
        };
    }

    public static Task<bool> AskConfirmAsync(PlayerControllerB caller, string action)
    {
        var tcs = new TaskCompletionSource<bool>();
        confirmationRequests[caller.actualClientId] = new ConfirmationRequest
        {
            action = action,
            callback = tcs.SetResult,
        };
        return tcs.Task;
    }

    public static Task<bool> AskConfirmAsync(PlayerControllerB caller)
    {
        var tcs = new TaskCompletionSource<bool>();
        confirmationRequests[caller.actualClientId] = new ConfirmationRequest
        {
            callback = tcs.SetResult,
        };
        return tcs.Task;
    }

    private static void UpdateChat()
    {
        HUDManager.Instance.chatText.text = string.Join(
            "\n",
            HUDManager.Instance.ChatMessageHistory
        );
        HUDManager.Instance.PingHUDElement(HUDManager.Instance.Chat, 4f);
    }

    public static void Print(string text)
    {
        HUDManager.Instance.ChatMessageHistory.Add($"<color=#00ffff>{text}</color>");
        UpdateChat();
    }

    public static void Print(string text, Color color)
    {
        HUDManager.Instance.ChatMessageHistory.Add(
            $"<color=#{(byte)(color.r * 255):x2}{(byte)(color.g * 255):x2}{(byte)(color.b * 255):x2}>{text}</color>"
        );
        UpdateChat();
    }

    [Obsolete]
    public static void Print(string text, Tuple<byte, byte, byte> color)
    {
        HUDManager.Instance.ChatMessageHistory.Add(
            $"<color=#{color.Item1:x2}{color.Item2:x2}{color.Item3:x2}>{text}</color>"
        );
        UpdateChat();
    }

    public static void Print(string text, (byte, byte, byte) color)
    {
        HUDManager.Instance.ChatMessageHistory.Add(
            $"<color=#{color.Item1:x2}{color.Item2:x2}{color.Item3:x2}>{text}</color>"
        );
        UpdateChat();
    }

    public static void Print(string text, byte r, byte g, byte b)
    {
        HUDManager.Instance.ChatMessageHistory.Add($"<color=#{r:x2}{g:x2}{b:x2}>{text}</color>");
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

    public static void PrintCommandError(string? error)
    {
        PrintError(
            $"Error running command{(error.IsNullOrWhiteSpace() ? "" : $": <noparse>{error}</noparse>")}"
        );
    }

    public static void Print(PlayerControllerB caller, string text)
    {
        SendToPlayer($"<color=#00ffff>{text}</color>", caller.actualClientId);
    }

    public static void Print(PlayerControllerB caller, string text, Color color)
    {
        SendToPlayer(
            $"<color=#{(byte)(color.r * 255):x2}{(byte)(color.g * 255):x2}{(byte)(color.b * 255):x2}>{text}</color>",
            caller.actualClientId
        );
    }

    [Obsolete]
    public static void Print(PlayerControllerB caller, string text, Tuple<byte, byte, byte> color)
    {
        SendToPlayer(
            $"<color=#{color.Item1:x2}{color.Item2:x2}{color.Item3:x2}>{text}</color>",
            caller.actualClientId
        );
    }

    public static void Print(PlayerControllerB caller, string text, (byte, byte, byte) color)
    {
        SendToPlayer(
            $"<color=#{color.Item1:x2}{color.Item2:x2}{color.Item3:x2}>{text}</color>",
            caller.actualClientId
        );
    }

    public static void Print(PlayerControllerB caller, string text, byte r, byte g, byte b)
    {
        SendToPlayer($"<color=#{r:x2}{g:x2}{b:x2}>{text}</color>", caller.actualClientId);
    }

    public static void PrintWarning(PlayerControllerB caller, string text)
    {
        SendToPlayer($"<color=#ffff00>{text}</color>", caller.actualClientId);
    }

    public static void PrintError(PlayerControllerB caller, string text)
    {
        SendToPlayer($"<color=#ff0000>{text}</color>", caller.actualClientId);
    }

    public static void PrintCommandError(PlayerControllerB caller, string? error)
    {
        PrintError(
            caller,
            $"Error running command{(error.IsNullOrWhiteSpace() ? "" : $": <noparse>{error}</noparse>")}"
        );
    }

    private static void SendToPlayer(string text, ulong clientId)
    {
        targetClientId = clientId;
        HUDManager.Instance.AddTextMessageClientRpc(text);
        targetClientId = null;
    }

    internal struct ConfirmationRequest
    {
        public string? action;
        public Action<bool> callback;
    }
}
