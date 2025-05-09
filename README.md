## Getting started

### 1. Include API in your project

Simply run `dotnet package add baer1.ChatCommandAPI` or add the following line to your csproj file:

```msbuild
<PackageReference Include="baer1.ChatCommandAPI" Version="0.*"/>
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
        error = null!;
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
        ["MyCommand", Name, "ExampleCommand", "Command", "HelloWorld"]; // Aliases (first entry is displayed on help, default: Name)
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