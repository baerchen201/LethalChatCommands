using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChatCommandAPI.BuiltinCommands;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Position = ChatCommandAPI.BuiltinCommands.Position;

// ReSharper disable InvertIf

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

    private ConfigEntry<bool> enableServerMode = null!;
    private ConfigEntry<bool> allowNullCaller = null!;
    private ConfigEntry<bool> builtInCommands = null!;
    public bool EnableServerMode => enableServerMode.Value;
    private ConfigEntry<string> serverCommandPrefix = null!;
    public string ServerCommandPrefix => serverCommandPrefix.Value;
    private List<ServerCommand> serverCommandList = null!;
    public IReadOnlyList<ServerCommand> ServerCommandList => serverCommandList;
    private ConfigEntry<string> serverWelcomeMessage = null!;
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
        allowNullCaller = Config.Bind(
            "Server",
            "AllowNullCaller",
            false,
            "Whether to allow invalid players to run commands"
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
        _ = new ErrorCommand();

        serverCommandList = [];
        _ = new ServerHelp();
        if (builtInCommands.Value)
        {
            _ = new ServerStatus();
            _ = new ServerMods();
        }
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

        Match match = new Regex(
            $"""(?:{Regex.Escape(CommandPrefix)}|{Regex.Escape(ServerCommandPrefix)})([a-z]+)(?: ([^ ="]+|(?:"[^"]*?")))*?(?: ([^ ="]+=[^ "]+|[^ ="]+="[^"]*?"))*\s*$""",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
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

    public bool RunCommand(
        ref PlayerControllerB? caller,
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
            return matches[0].Invoke(ref caller, args, kwargs, out error);
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
                {
                    Logger.LogWarning($"   Error running command: {(error ?? "null")}");
                    if (error != null)
                        PrintError(
                            $"Error running command{(error.IsNullOrWhiteSpace() ? "" : $": <noparse>{error}</noparse>")}"
                        );
                }
                return false;
            }

            Logger.LogInfo("<< Invalid command");
            PrintError("Invalid command");
            return false;
        }
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddPlayerChatMessageServerRpc))]
    internal class ServerCommandPatch
    {
        protected enum __RpcExecStage
        {
            None,
            Server,
            Client,
        }

        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            ref HUDManager __instance,
            ref string chatMessage,
            ref int playerId
        )
        {
            if (
                Traverse.Create(__instance).Field("__rpc_exec_stage").GetValue<__RpcExecStage>()
                    != __RpcExecStage.Server
                || !(__instance.NetworkManager.IsServer || __instance.NetworkManager.IsHost)
                || chatMessage.IsNullOrWhiteSpace()
                || !Instance.IsServerCommand(chatMessage)
            )
                return true;

            Logger.LogInfo($">> Parsing server command by player {playerId}: {chatMessage}");
            PlayerControllerB? caller = null;
            if (playerId >= 0 && playerId < StartOfRound.Instance.allPlayerScripts.Length)
                caller = StartOfRound.Instance.allPlayerScripts[playerId];
            Logger.LogDebug($"   caller: {(caller == null ? "null" : caller.playerUsername)}");
            if (caller == null || !Utils.IsPlayerControlled(caller))
            {
                Logger.LogWarning(
                    $"Server command sent by invalid player {playerId}: {chatMessage}"
                );
                if (!Instance.allowNullCaller.Value)
                    return true;
            }

            if (Instance.ParseCommand(chatMessage, out var command, out var args, out var kwargs))
            {
                StringBuilder sb = new StringBuilder(
                    $"<< Parsed command: {command}({(caller == null ? "null" : $"#{caller.playerClientId} {caller.playerUsername}")}{(args.Length > 0 || kwargs.Count > 0 ? ", " : "")}"
                );
                if (args.Length > 0)
                {
                    sb.Append(args.Join());
                    if (kwargs.Count > 0)
                        sb.Append(", ");
                }
                sb.Append(kwargs.Select(kvp => $"{kvp.Key}: {kvp.Value}").Join());
                Logger.LogInfo(sb + ")");

                if (!Instance.RunCommand(ref caller, command, args, kwargs, out var error))
                {
                    Logger.LogWarning($"   Error running command: {error ?? "null"}");
                    if (caller != null && error != null)
                        PrintError(
                            caller,
                            $"Error running command{(error.IsNullOrWhiteSpace() ? "" : $": <noparse>{error}</noparse>")}"
                        );
                }

                return false;
            }

            Logger.LogInfo("<< Invalid command");
            if (caller != null)
                PrintError(caller, "Invalid command");
            return false;
        }
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

    private static void SendToPlayer(string text, ulong clientId)
    {
        targetClientId = clientId;
        HUDManager.Instance.AddTextMessageClientRpc(text);
        targetClientId = null;
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddTextMessageClientRpc))]
    internal class SendChatPatch
    {
        // ReSharper disable once UnusedMember.Local
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        ) =>
            new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(
                        OpCodes.Call,
                        AccessTools.Method(typeof(NetworkBehaviour), "__endSendClientRpc")
                    )
                )
                .Advance(-1)
                .Insert(CodeInstruction.Call(typeof(SendChatPatch), nameof(a)))
                .InstructionEnumeration();

        [SuppressMessage(
            "Method Declaration",
            "Harmony003:Harmony non-ref patch parameters modified"
        )]
        public static ClientRpcParams a(ClientRpcParams clientRpcParams)
        {
            if (targetClientId == null)
                return clientRpcParams;

            Logger.LogDebug($"Redirecting message to {targetClientId}");
            clientRpcParams = default;
            clientRpcParams.Send.TargetClientIds = [targetClientId.Value];
            return clientRpcParams;
        }
    }

    internal static ulong? targetClientId;

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientConnect))]
    internal class WelcomePatch
    {
        // ReSharper disable once UnusedMember.Local
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        ) =>
            new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(
                        OpCodes.Call,
                        AccessTools.Method(
                            typeof(StartOfRound),
                            nameof(StartOfRound.OnPlayerConnectedClientRpc)
                        )
                    )
                )
                .Advance(1)
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_1),
                    CodeInstruction.Call(typeof(WelcomePatch), nameof(a))
                )
                .InstructionEnumeration();

        internal static void a(ulong clientId)
        {
            Logger.LogDebug(
                $">> WelcomePatch({clientId}) ServerWelcomeMessage:{Instance.ServerWelcomeMessage ?? "null"} IsNullOrWhiteSpace:{Instance.ServerWelcomeMessage.IsNullOrWhiteSpace()}"
            );
            if (
                Instance.ServerWelcomeMessage == null
                || Instance.ServerWelcomeMessage.IsNullOrWhiteSpace()
            )
                return;

            targetClientId = clientId;
            HUDManager.Instance.AddTextMessageClientRpc(
                $"<color=#7069ff>{Instance.ServerWelcomeMessage}</color>"
            );
            targetClientId = null;
        }
    }
}
