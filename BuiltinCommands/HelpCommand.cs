using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;

namespace ChatCommandAPI.BuiltinCommands;

public class Help : Command
{
    public override bool Hidden => true;
    public override string Description => "Displays all available commands";

    private const string SEPARATOR = "<color=#00FFFF>===============</color>\n";

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = "No commands have been registered yet";
        if (
            ChatCommandAPI.Instance.CommandList == null!
            || ChatCommandAPI.Instance.CommandList.Count == 0
        )
            return false;

        ChatCommandAPI.Print(
            SEPARATOR
                + string.Join(
                    SEPARATOR,
                    ChatCommandAPI
                        .Instance.CommandList.Where(i => !i.Hidden)
                        .Select(i =>
                        {
                            StringBuilder sb = new StringBuilder(
                                $"{i.Name}{(i.Description == null ? "" : $" - {i.Description}")}\n"
                            );
                            foreach (string usage in i.Syntax ?? [null!])
                            {
                                sb.Append(
                                    $"<color=#ffff00>{ChatCommandAPI.Instance.CommandPrefix}{i.Commands[0]}{(usage.IsNullOrWhiteSpace() ? "" : " ")}</color><color=#dddd00><noparse>{usage}</noparse></color>\n"
                                );
                            }

                            return sb.ToString();
                        })
                )
                + SEPARATOR.Trim()
        );
        return true;
    }
}
