using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using GameNetcodeStuff;

namespace ChatCommandAPI.BuiltinCommands;

public class ServerHelp : ServerCommand
{
    public override string Name => "Help";
    public override bool Hidden => true;
    public override string Description => "Displays all available commands on the server";

    public override bool Invoke(
        ref PlayerControllerB caller,
        string[] args,
        Dictionary<string, string> kwargs,
        out string? error
    )
    {
        error = "This server has no available commands";
        if (
            ChatCommandAPI.Instance.ServerCommandList == null!
            || ChatCommandAPI.Instance.ServerCommandList.Count(i => !i.Hidden) == 0
        )
            return false;

        ChatCommandAPI.Print(
            caller,
            Help.SEPARATOR
                + string.Join(
                    Help.SEPARATOR,
                    ChatCommandAPI
                        .Instance.ServerCommandList.Where(i => !i.Hidden)
                        .Select(i =>
                        {
                            StringBuilder sb = new StringBuilder(
                                $"{i.Name}{(i.Description == null ? "" : $" - {i.Description}")}\n"
                            );
                            foreach (string usage in i.Syntax ?? [null!])
                            {
                                sb.Append(
                                    $"<color=#ffff00>{ChatCommandAPI.Instance.ServerCommandPrefix}{i.Commands[0]}{(usage.IsNullOrWhiteSpace() ? "" : " ")}</color><color=#dddd00><noparse>{usage}</noparse></color>\n"
                                );
                            }

                            return sb.ToString();
                        })
                )
                + Help.SEPARATOR.Trim()
        );
        return true;
    }
}
