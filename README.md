## This is a mod for developers

You probably won't find much on this page as a player.

**Instead, try checking out the page of the mod that brought you here**

If you encounter any issues while playing or creating a mod with this API,
or you would like to request a new feature,
please [open an issue on GitHub](https://github.com/baerchen201/LethalChatCommands/issues).

This mod is open-source, you can contribute to it
by [opening a pull request](https://github.com/baerchen201/LethalChatCommands/pulls)

## Getting started

### 1. Include API in your project

Simply run `dotnet add package baer1.ChatCommandAPI` or add the following line to your csproj file:

```msbuild
<PackageReference Include="baer1.ChatCommandAPI" Version="*"/>
```

Additionally, you should reference this mod in both your main plugin class and your `manifest.json`:

```csharp
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(ChatCommandAPI.MyPluginInfo.PLUGIN_GUID)]
public class ExampleMod : BaseUnityPlugin;
```

```json
{
  "dependencies": [
    "BepInEx-BepInExPack-5.4.2305",
    "baer1-ChatCommandAPI-1.0.0"
  ]
}
```

### 2. Create Command Subclass

Simply create a non-static class that inherits the `ChatCommandAPI.Command` class.
The `Invoke` method contains the logic that will be run.

The default command name is the class name you assign to it, `ExampleCommand` in the example below.
You can run this command through the in-game chat. \(See [below](#customizing-the-command) for changing it\)

This mod is case-insensitive \(`/eXaMpLeCoMmAnD` is valid\)

Example:

```csharp
using ChatCommandAPI;

namespace ExampleMod;

public class ExampleCommand : Command
{
    public override void Invoke(string args)
    {
        // Put your code here
        return;
    }
}
```

### 3. Instantiate the Subclass _once_

```csharp
using BepInEx;

namespace ExampleMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(ChatCommandAPI.MyPluginInfo.PLUGIN_GUID)]
public class ExampleMod : BaseUnityPlugin
{
    private void Awake()
    {
        _ = new ExampleCommand();
    }
}
```

### 4. Done!

You can now use the command `/examplecommand` in-game to run the function defined
in [Step 2](#2-create-command-subclass)

## Advanced usage

### Customizing the command

You can overwrite several properties of the `Command` class to customize your command:

```csharp
using ChatCommandAPI;
using ChatCommandAPI.Utils;

namespace ExampleMod;

public class ExampleCommand : Command
{
    public override string Name => "My command"; // Display name
    public override string Description => "Prints Hello World [amount] times"; // Short description

    public override string Command => "examplecommand"; // Primary command
    public override string[] Aliases => ["ex", "example"]; // Aliases (an alias may become the primary to avoid duplicates)
    public override string[] Syntax => ["[amount]"]; // All valid syntaxes for this command (only for help, not validated)
    public override bool Hidden => false; // Whether to hide this command from the help list (useful for debugging, default: false)

    public override void Invoke(string args)
    {
        ushort amount = 1;
        if (args.Length > 0)
            if (!ushort.TryParse(args, out amount))
                throw new InvalidArgumentsException(); // Report "Invalid arguments"
        while (amount > 0)
        {
            Chat.Print("Hello, World!");
            amount--;
        }
    }
}
```

The above results in the following being displayed on the help list:

```
My command - Prints Hello World [amount] times
/examplecommand [amount]
```

### Server commands

Server commands are commands that anyone on the server can use, as long as the host has them installed.

They are very similar to the client commands.

Example (simple implementation of the ShipLoot mod as a server command):

```csharp
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;
using ChatCommandAPI;
using ChatCommandAPI.Utils;

namespace ExampleMod;

public class ShipLoot : ServerCommand
{
    public override string Description => "Displays the value of all loot on the ship";

    public override void Invoke(
        PlayerControllerB caller, // Player that sent the command
        string args
    )
    {
        // We need the ship to calculate the value of the loot on it
        var ship = GameObject.Find("/Environment/HangarShip");
        if (ship == null)
            throw new CommandException("Ship not found"); // Report "Ship not found"

        Chat.Print(
            caller, // only prints text to this player
            $"Ship: {ItemValue(ship)}"
        );
    }

    // returns value of loot on ship
    private static int ItemValue(GameObject ship) =>
        ship.GetComponentsInChildren<GrabbableObject>()
            .Where(obj => obj.itemProperties.isScrap && obj is not RagdollGrabbableObject)
            .Sum(scrap => scrap.scrapValue);
}
```

### Using ToggleCommand

If you are creating a command that is supposed to act as a toggle (on or off), you can use the `ToggleCommand` class to
make this easier.

You can use the `CurrentValue` property to access it externally or in harmony patches:

```csharp
using GameNetcodeStuff;
using HarmonyLib;
using ChatCommandAPI;

namespace ExampleMod;

public class ExampleDisableJump : ToggleCommand
{
    public override string Name => "Disable Jump";
    public override string Description => "Disables your jump key";
    public override string Command => "disablejump";

    public override bool CurrentValue
    {
        get => DisableJump;
        set => DisableJump = value;
    }

    internal static bool DisableJump;

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Jump_performed))]
    private static class PlayerControllerB_Jump_performed
    {
        private static bool Prefix()
        {
            return !DisableJump;
        }
    }
}
```

### Argument parsing

If you need multiple arguments, you should use the `Args.Parse` function:

```csharp
using ChatCommandAPI;
using ChatCommandAPI.Utils;

namespace ExampleMod;

public class ExampleAdd : Command
{
    public override string Name => "Add";
    public override string Description => "Adds numbers";
    public override string[] Syntax => ["[number] ..."];

    public override void Invoke(string args)
    {
        var _args = Args.Parse(args);
        var result = 0;
        foreach (var arg in _args)
        {
            if (!int.TryParse(arg, out var i))
                throw new InvalidArgumentsException();
            result += i;
        }
        Chat.Print(result.ToString());
    }
}
```

### Player arguments

If you want to allow a player name as an argument, you should use the `Player.TryGetPlayer` function:

```csharp
using ChatCommandAPI;
using ChatCommandAPI.Utils;

namespace ExampleMod;

public class ExamplePlayerCommand : Command
{
    public override string Name => "IsPlayerDead";
    public override string Description => "Shows if a player is dead";
    public override string[] Syntax => ["<player>"];

    public override void Invoke(string args)
    {
        if (Player.TryGetPlayer(args, out var player))
            Chat.Print(
                player.isPlayerDead
                    ? $"Player {player.PlayerString()} is dead."
                    : $"Player {player.PlayerString()} is not dead."
            );
        else
            throw new Player.UnknownPlayerException(args); // Report "Unknown player '...'"
    }
}
```

### Position arguments

If you want to allow a position coordinates as an argument, you should use the `Pos.TryParse` function:

```csharp
using System;
using System.Globalization;
using GameNetcodeStuff;
using UnityEngine;
using ChatCommandAPI;
using ChatCommandAPI.Utils;

namespace ExampleMod;

public class ExampleTeleport : Command
{
    public override string Name => "Teleport";
    public override string Description => "Teleports you to the given position";
    public override string Command => "tp";
    public override string[] Aliases => [Name.ToLowerInvariant()];
    public override string[] Syntax => ["<x> <y> <z>"];

    public override void Invoke(string args)
    {
        var localPlayer = StartOfRound.Instance.localPlayerController;
        if (!Pos.TryParse(args, out var position, localPlayer))
            throw new InvalidArgumentsException();
        Teleport(localPlayer, position);
    }

    public static void Teleport(PlayerControllerB localPlayer, Vector3 position)
    {
        Chat.Print(
            $"Teleported {Math.Round(Vector3.Distance(localPlayer.transform.position, position), 2).ToString(CultureInfo.InvariantCulture)}m"
        );
        localPlayer.TeleportPlayer(position);
    }
}
```
