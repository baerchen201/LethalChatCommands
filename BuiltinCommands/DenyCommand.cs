using System.Collections.Generic;

namespace ChatCommandAPI.BuiltinCommands;

public class Deny : Command
{
    public override string[] Commands => [Name.ToLower(), "d", "den"];

    public override string Description =>
        ChatCommandAPI.confirmationRequest == null
        || ChatCommandAPI.confirmationRequest.Value.action == null
            ? "Cancels an action"
            : $"Cancels {ChatCommandAPI.confirmationRequest.Value.action}";

    public override bool Hidden => ChatCommandAPI.confirmationRequest == null;

    public override bool Invoke(string[] args, Dictionary<string, string> kwargs, out string error)
    {
        error = "No action currently needs confirmation";
        if (ChatCommandAPI.confirmationRequest == null)
            return false;
        ChatCommandAPI.confirmationRequest.Value.callback.Invoke(false);
        ChatCommandAPI.confirmationRequest = null;
        return true;
    }
}
