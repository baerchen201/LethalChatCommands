## This mod is unfinished.

**There may be major breaking changes before the first full release.**

If you encounter any issues while playing or creating a mod with this API, please [report them on GitHub](https://github.com/baerchen201/LethalChatCommands/issues).

This mod is open-source, you can contribute to it by [opening a pull request](https://github.com/baerchen201/LethalChatCommands/pulls)

## Getting started

### 1. Include API in your project

Simply run `dotnet add package baer1.ChatCommandAPI` or add the following line to your csproj file:

```msbuild
<PackageReference Include="baer1.ChatCommandAPI" Version="0.2.*"/>
```

Additionally, you should reference this mod in both your main plugin class and your manifest.json \(replace `<VERSION>` with the actual version you are using\):

```csharp
...
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("baer1.ChatCommandAPI", BepInDependency.DependencyFlags.HardDependency)]
public class ExampleMod : BaseUnityPlugin
...
```
```json
...
"dependencies": [
"BepInEx-BepInExPack-5.4.2100",
"baer1-ChatCommandAPI-<VERSION>",
...
]
...
```

### 2. Create Command Subclass

Simply create a non-static class that inherits the `ChatCommandAPI.Command` class.
The `Invoke` method contains the logic that will be run.

The default command name is the class name you assign to it, `ExampleCommand` in the example below.
You can run this command through the in-game chat. \(See [below](#customizing-the-command) for changing it\)

This mod is case-insensitive \(`/eXaMpLeCoMmAnD` is valid\)

Example:
```csharp
using System.Collections.Generic;
using ChatCommandAPI;

namespace ExampleMod;

public class ExampleCommand : Command
{
    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = null!; // No error message
        // Put your code here
        return true;
    }
}
```

### 3. Instantiate the Subclass _once_

```csharp
namespace ExampleMod;

public class ChatCommandAPI : BaseUnityPlugin
{
    private void Awake()
    {
        _ = new ExampleCommand();
    }
}
```

### 4. Done!

You can now use the command `/examplecommand` in-game to run the function defined in [Step 2](#2-create-command-subclass)

## Advanced usage

### Customizing the command

You can overwrite several properties of the `Command` class to customize your command:

```csharp
using System.Collections.Generic;
using ChatCommandAPI;

namespace ExampleMod;

public class ExampleCommand : Command
{
    public override string Name => "Example"; // Command name (default: Class name)
    public override string[] Commands =>
        ["MyCommand", Name, "ExampleCommand", "Command", "HelloWorld"]; // Aliases (first entry is displayed on help, default: Name.ToLower())
    public override string Description => "Prints Hello World [amount] times"; // Short description of this command
    public override string[] Syntax => ["", "[amount]"]; // All valid syntaxes for this command (only for help, not validated)
    public override bool Hidden => false; // Whether to hide this command from the help list (useful for debugging, default: false)

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = "Invalid argument"; // Set error for return value false
        ushort amount = 1;
        if (args.Length > 0)
            if (!ushort.TryParse(args[0], out amount))
                return false; // Return value false: reports error to user
        while (amount > 0)
        {
            ChatCommandAPI.ChatCommandAPI.Instance.Print("Hello, World!");
            amount--;
        }
        return true; // Return value true: success, doesn't do anything
    }
}
```

The above results in the following being displayed on the help list:

```
Example - Prints Hello World [amount] times
/MyCommand
/MyCommand [amount]
```

### Server commands

Server commands are commands that anyone on the server can use, as long as the host has them installed.

They are very similar to the client commands.

Example (simple implementation of the ShipLoot mod as a server command):

```csharp
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace ChatCommandAPI.BuiltinCommands;

public class ShipLoot : ServerCommand
{
    public override string Description => "Displays the value of all loot on the ship"; // Command description (for !help)

    public override bool Invoke(
        ref PlayerControllerB? caller, // Player that sent the command (can be spoofed, this may be fixed in the future)
        string[] args,
        Dictionary<string, string> kwargs,
        out string error
    )
    {
        error = "caller is null"; // error message is not reported to anyone, but it is logged
        if (caller == null) // We need a player to respond to
            return false;

        error = "Ship not found"; // We need the ship to calculate the value of the loot on it
        var ship = GameObject.Find("/Environment/HangarShip");
        if (ship == null)
            return false;

        ChatCommandAPI.Print(
            caller, // Only prints text to this player
            $"Ship: {ItemValue(ship)}"
        );
        return true;
    }

    // returns value of loot on ship
    private static int ItemValue(GameObject ship) =>
        ship.GetComponentsInChildren<GrabbableObject>()
            .Where(obj => obj.itemProperties.isScrap && obj is not RagdollGrabbableObject)
            .Sum(scrap => scrap.scrapValue);
}
```

### Using ToggleCommand

If you are creating a command that is supposed to act as a toggle (on or off), you can use the `ToggleCommand` class to make this easier.

You can override the `Value` property to access it externally or in harmony patches:

```csharp
using GameNetcodeStuff;
using HarmonyLib;
using ChatCommandAPI;

namespace ExampleMod;

public class ExampleToggleCommand : ToggleCommand
{
    public override string Name => "BlockJump"; // Command name
    public override string ToggleDescription => "Blocks jump inputs"; // Use ToggleDescription instead of Description
    public override string EnabledString => "blocked"; // String to use when Value=false
    public override string DisabledString => "unblocked"; // String to use when Value=true

    public override void PrintValue() => ChatCommandAPI.Print($"Jump inputs {ValueString}"); // Called after value is updated, for user feedback

    public override bool Value // Redirect reads and writes to static property
    {
        get => BlockJump;
        set => BlockJump = value;
    }
    public static bool BlockJump { get; internal set; } // Static property for external access

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Jump_performed))]
    internal class Patch
    {
        private static bool Prefix() => !BlockJump; // Block jump
    }
}
```

### Player arguments

If you want to allow a player name as an argument, you should use the `Utils.GetPlayer` function:

```csharp
using System.Collections.Generic;
using ChatCommandAPI;
using GameNetcodeStuff;

namespace ExampleMod;

public class ExamplePlayerCommand : Command
{
    public override string Name => "IsPlayerDead"; // Command name
    public override string Description => "Shows if a player is dead"; // Command description
    public override string[] Syntax => ["[player]"];

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = null!; // Don't set error message like "Player not found", this error is reported by the GetPlayer function automatically
        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (args.Length > 0)
            if (!Utils.GetPlayer(args[0], out error, out player))
                return false; // Report failure, no error message, prevents further execution
        ChatCommandAPI.ChatCommandAPI.Print(
            player.isPlayerDead
                ? $"Player {player.playerUsername} is dead."
                : $"Player {player.playerUsername} is not dead."
        );
        return true; // Report success
    }
}
```

### Position arguments

If you want to allow a position coordinates as an argument, you should use the `Utils.ParsePosition` function:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ChatCommandAPI.BuiltinCommands;

public class Teleport : Command
{
    public override string[] Commands => ["tp", Name];
    public override string Description =>
        "Teleports you to the specified coordinates\n"
        + "<color=#ffff00>Can only be used in singleplayer</color>"; // Yellow warning about only usable in singleplayer

    public override string[] Syntax => ["<x> <y> <z>", "<x,y,z>"]; // Simple syntax for position

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        // Only allow this command when you are the only player (and host) in the game, to prevent cheating in multiplayer
        error = "You can only teleport in singleplayer";
        if (
            StartOfRound.Instance.allPlayerScripts.Count(Utils.IsPlayerControlled) > 1
            || !GameNetworkManager.Instance.localPlayerController.isHostPlayerObject
        )
            return false;

        error = "Invalid coordinates";
        Vector3 position = GameNetworkManager.Instance.localPlayerController.transform.position; // Origin for relative and local coordinates
        Vector3 newPosition; // Initialize new position variable
        switch (args.Length)
        {
            case 1: // If only one argument, it should be the comma notation (x,y,z)
                if ( // If the position input is invalid, ...
                    !Utils.ParsePosition(
                        position,
                        GameNetworkManager.Instance.localPlayerController.transform.rotation, // Player rotation for local coordinates
                        args[0],
                        out newPosition // Automatically saves position to the correct variable
                    )
                )
                    return false; // ... return error
                break;

            case 3: // If three arguments, it should be the space notation (x y z)
                if ( // If the position input is invalid, ...
                    !Utils.ParsePosition(
                        position,
                        GameNetworkManager.Instance.localPlayerController.transform.rotation, // Player rotation for local coordinates
                        args[0],
                        args[1],
                        args[2],
                        out newPosition // Automatically saves position to the correct variable
                    )
                )
                    return false; // ... return error
                break;

            default: // If not 1 or 3 arguments, position is invalid
                error = "Invalid arguments";
                return false;
        }
        
        GameNetworkManager.Instance.localPlayerController.TeleportPlayer(newPosition); // Teleport player
        GameNetworkManager.Instance.localPlayerController.isInsideFactory = IsInsideFactory( // Fix lighting
            position
        );
        ChatCommandAPI.Print(
            $"Teleported to {newPosition} (distance:{Math.Round(Vector3.Distance(position, newPosition), 2)})"
        );
        return true;
    }
    
    // Checks if position is inside the facility (for lighting)
    private static bool IsInsideFactory(Vector3 position)
    {
        return Object
            .FindObjectsOfType<OutOfBoundsTrigger>()
            .Any(i => position.y < i.gameObject.transform.position.y);
    }
}
```
