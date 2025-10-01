using System.Collections.Generic;

namespace ChatCommandAPI.BuiltinCommands;

public class Confirm : Command
{
    public override string[] Commands => [Name.ToLower(), "c", "con"];

    public override string Description =>
        ChatCommandAPI.confirmationRequest == null
        || ChatCommandAPI.confirmationRequest.Value.action == null
            ? "Confirms an action"
            : $"Confirms {ChatCommandAPI.confirmationRequest.Value.action}";

    public override bool Hidden => ChatCommandAPI.confirmationRequest == null;

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = "No action currently needs confirmation";
        if (ChatCommandAPI.confirmationRequest == null)
            return false;
        ChatCommandAPI.confirmationRequest.Value.callback.Invoke(true);
        ChatCommandAPI.confirmationRequest = null;
        return true;
    }
}
