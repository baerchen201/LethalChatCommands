using System.Linq;
using ChatCommandAPI.Utils;
using GameNetcodeStuff;

namespace ChatCommandAPI.ServerCommands;

#if DEBUG
public class ExampleWhisper : ServerCommand
{
    public override string Name => "Whisper";

    public override string Description => "Sends a message to only one player";

    public override string Command => "w";

    public override string[] Aliases => ["whisper", "tell"];

    public override string[] Syntax => ["<player> ..."];

    public override void Invoke(PlayerControllerB caller, string args)
    {
        var _args = Args.Parse(args, 1).ToArray();
        if (_args.Length != 2)
            throw new InvalidArgumentsException();
        if (!Player.TryGetPlayer(_args[0], out var target))
            throw new Player.UnknownPlayerException(_args[0]);
        Whisper(caller, target, _args[1]);
    }

    public static void Whisper(PlayerControllerB caller, PlayerControllerB target, string message)
    {
        Chat.Print(
            target,
            $"<color=#ff0000>{caller.playerUsername}</color> <color=#aaaaaa>(Whisper)</color>: <color=#ffff00>'{message}'</color>"
        );
        Chat.Print(
            caller,
            $"<color=#aaaaaa>To</color> <color=#ff0000>{target.playerUsername}</color>: <color=#ffff00>'{message}'</color>"
        );
    }
}
#endif
