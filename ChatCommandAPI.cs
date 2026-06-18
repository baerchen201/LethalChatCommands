using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChatCommandAPI.Commands;
using ChatCommandAPI.ServerCommands;
using ChatCommandAPI.Utils;
using HarmonyLib;
using Color = UnityEngine.Color;
using Help = ChatCommandAPI.Commands.Help;

namespace ChatCommandAPI;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalModUtils.MyPluginInfo.PLUGIN_GUID)]
public class ChatCommandAPI : BaseUnityPlugin
{
    internal const string COMMAND_NAME_REGEX = @"\S+";

    internal readonly Dictionary<string, Command> commands = [];
    internal readonly Dictionary<string, ServerCommand> serverCommands = [];
    private ConfigEntry<string> commandPrefix = null!;
    private ConfigEntry<bool> compatibilityModeEnabled = null!;

    private ConfigEntry<Color> defaultMessageColor = null!;
    private ConfigEntry<string> serverCommandPrefix = null!;
    private ConfigEntry<bool> serverCommandsEnabled = null!;
    private ConfigEntry<string> serverWelcomeMessage = null!;
    public static ChatCommandAPI Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    public System.Drawing.Color DefaultMessageColor => defaultMessageColor.Value.ToSystem();
    public char CommandPrefix => commandPrefix.Value[0];
    public bool CompatibilityModeEnabled => compatibilityModeEnabled.Value;
    public bool ServerCommandsEnabled => serverCommandsEnabled.Value;
    public char ServerCommandPrefix => serverCommandPrefix.Value[0];

    public string ServerWelcomeMessage =>
        string.Format(serverWelcomeMessage.Value.Trim(), ServerCommandPrefix);

    /// <summary>
    ///     A list of all registered commands and their primary commands (avoids duplicates)
    /// </summary>
    public IReadOnlyDictionary<string, Command> Commands => commands;

    /// <summary>
    ///     A list of all registered server commands and their primary commands (avoids duplicates)
    /// </summary>
    public IReadOnlyDictionary<string, ServerCommand> ServerCommands => serverCommands;

    private void Awake()
    {
        const string SECTION_GENERAL = "General";
        const string SECTION_CLIENT = "Client";
        const string SECTION_SERVER = "Server";

        Logger = base.Logger;
        Instance = this;

        defaultMessageColor = Config.Bind(
            SECTION_GENERAL,
            nameof(DefaultMessageColor),
            System.Drawing.Color.Aqua.ToUnity(),
            "The default color to use for chat responses"
        );

        commandPrefix = Config.Bind(
            SECTION_CLIENT,
            nameof(CommandPrefix),
            '/'.ToString(),
            "The prefix to use for client-side commands"
        );
        compatibilityModeEnabled = Config.Bind(
            SECTION_CLIENT,
            nameof(CompatibilityModeEnabled),
            false,
            "Allows non-API chat commands to run (may cause the command you run to be sent in chat)"
        );

        serverCommandsEnabled = Config.Bind(
            SECTION_SERVER,
            nameof(ServerCommandsEnabled),
            false,
            "Enables or disables server commands"
        );
        serverCommandPrefix = Config.Bind(
            SECTION_SERVER,
            nameof(ServerCommandPrefix),
            '!'.ToString(),
            "The prefix to use for server-side commands"
        );
        serverWelcomeMessage = Config.Bind(
            SECTION_SERVER,
            nameof(ServerWelcomeMessage),
            "This server supports chat commands.\nType {0}help for more information",
            $"A welcome message that is displayed to any player that joins (clear to disable). {{0}} is replaced with {nameof(ServerCommandPrefix)}"
        );

        Config.SettingChanged += verifyConfigValues;
        verifyConfigValues(null!, null!);

        _ = new Help();
        _ = new ServerCommands.Help();

#if DEBUG
        Logger.LogWarning(
            "You are running a development version of this mod, which contains some additional commands for testing."
        );
        Logger.LogWarning("If you downloaded this mod from an official source, please report this");

        _ = new ExampleCommand();
        _ = new ExampleErrorCommand();
        _ = new ExampleExceptionCommand();
        _ = new ExampleTeleport();
        _ = new ExampleDisableJump();

        _ = new ExampleAdd();
        _ = new ExampleIsPlayerDead();
        _ = new ExampleShipLoot();
        _ = new ExampleWhisper();
#endif

        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        return;

        void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);
            Logger.LogDebug("Patching...");
            Harmony.PatchAll();
            Logger.LogDebug("Finished patching!");
        }

        void verifyConfigValues(object args, object sender)
        {
            switch (commandPrefix.Value.Length)
            {
                case 1:
                    break;
                case <= 0:
                    Logger.LogWarning(
                        $"Invalid {nameof(CommandPrefix)} value '{CommandPrefix}', value expected..."
                    );
                    commandPrefix.Value = (string)commandPrefix.DefaultValue;
                    goto serverCommandPrefix;
                default:
                    Logger.LogWarning(
                        $"{nameof(CommandPrefix)} '{commandPrefix.Value}' contains more than one character, only the first one will be used"
                    );
                    break;
            }

            if (!char.IsSymbol(CommandPrefix) && !char.IsPunctuation(CommandPrefix))
            {
                Logger.LogWarning(
                    $"Invalid {nameof(CommandPrefix)} value '{CommandPrefix}', symbol expected..."
                );
                commandPrefix.Value = (string)commandPrefix.DefaultValue;
            }

            serverCommandPrefix:
            switch (serverCommandPrefix.Value.Length)
            {
                case 1:
                    break;
                case <= 0:
                    Logger.LogWarning(
                        $"Invalid {nameof(ServerCommandPrefix)} value '{ServerCommandPrefix}', symbol expected..."
                    );
                    serverCommandPrefix.Value = (string)serverCommandPrefix.DefaultValue;
                    goto compare;
                default:
                    Logger.LogWarning(
                        $"{nameof(ServerCommandPrefix)} '{serverCommandPrefix.Value}' contains more than one character, only the first one will be used"
                    );
                    break;
            }

            if (!char.IsSymbol(ServerCommandPrefix) && !char.IsPunctuation(ServerCommandPrefix))
            {
                Logger.LogWarning(
                    $"Invalid {nameof(ServerCommandPrefix)} value '{ServerCommandPrefix}', symbol expected..."
                );
                serverCommandPrefix.Value = (string)serverCommandPrefix.DefaultValue;
            }

            compare:
            if (ServerCommandPrefix == CommandPrefix)
                Logger.LogWarning(
                    $"{nameof(ServerCommandPrefix)} is the same as {nameof(CommandPrefix)}, this configuration will prioritize client commands and is not supported"
                );
        }
    }

    public static bool TryParseCommand(
        char prefix,
        string cmdline,
        out string name,
        out string args
    )
    {
        name = null!;
        args = null!;
        var match = Regex.Match(
            cmdline,
            $"^{Regex.Escape(prefix.ToString())}({COMMAND_NAME_REGEX})(?: (.*))?$"
        );
        if (!match.Success)
            return false;

        var _args = match.Groups[2];
        name = match.Groups[1].Value;
        args = _args.Success ? _args.Value : string.Empty;
        return true;
    }

    public bool TryParseCommand(string cmdline, out string name, out string args)
    {
        return TryParseCommand(CommandPrefix, cmdline, out name, out args);
    }

    public bool TryParseServerCommand(string cmdline, out string name, out string args)
    {
        return TryParseCommand(ServerCommandPrefix, cmdline, out name, out args);
    }

    public bool TryGetCommand(string name, out Command command, out string primaryName)
    {
        primaryName = name = name.ToLowerInvariant();
        var commands = Commands;
        if (commands.TryGetValue(name, out command))
            return true;

        foreach (var kvp in commands)
            if (
                string.Equals(kvp.Value.FullName, name, StringComparison.InvariantCultureIgnoreCase)
            )
            {
                primaryName = kvp.Key;
                command = kvp.Value;
                return true;
            }

        foreach (var kvp in commands)
            if (string.Equals(kvp.Value.Command, name, StringComparison.InvariantCultureIgnoreCase))
            {
                primaryName = kvp.Key;
                command = kvp.Value;
                return true;
            }

        foreach (var kvp in commands)
            if (kvp.Value.Aliases.Contains(name, StringComparer.InvariantCultureIgnoreCase))
            {
                primaryName = kvp.Key;
                command = kvp.Value;
                return true;
            }

        return false;
    }

    public bool TryGetServerCommand(string name, out ServerCommand command, out string primaryName)
    {
        primaryName = name = name.ToLowerInvariant();
        var commands = ServerCommands;
        if (commands.TryGetValue(name, out command))
            return true;

        foreach (var kvp in commands)
            if (
                string.Equals(kvp.Value.FullName, name, StringComparison.InvariantCultureIgnoreCase)
            )
            {
                primaryName = kvp.Key;
                command = kvp.Value;
                return true;
            }

        foreach (var kvp in commands)
            if (string.Equals(kvp.Value.Command, name, StringComparison.InvariantCultureIgnoreCase))
            {
                primaryName = kvp.Key;
                command = kvp.Value;
                return true;
            }

        foreach (var kvp in commands)
            if (kvp.Value.Aliases.Contains(name, StringComparer.InvariantCultureIgnoreCase))
            {
                primaryName = kvp.Key;
                command = kvp.Value;
                return true;
            }

        return false;
    }
}
