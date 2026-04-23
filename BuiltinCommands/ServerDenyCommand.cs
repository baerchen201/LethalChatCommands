using System.Collections.Generic;
using GameNetcodeStuff;

namespace ChatCommandAPI.BuiltinCommands;

public class ServerDeny() : ServerCommand(true)
{
    public override string[] Commands => [nameof(Deny).ToLower(), "d", "den"];

    public override string Description =>
        ChatCommandAPI.confirmationRequest == null
        || ChatCommandAPI.confirmationRequest.Value.action == null
            ? "Cancels an action"
            : $"Cancels {ChatCommandAPI.confirmationRequest.Value.action}";

    public override bool Hidden => ChatCommandAPI.confirmationRequest == null;

    public override bool Invoke(
        ref PlayerControllerB? caller,
        string[] args,
        Dictionary<string, string> kwargs,
        out string error
    )
    {
        error = "No action currently needs confirmation";
        if (
            !ChatCommandAPI.confirmationRequests.TryGetValue(
                caller.actualClientId,
                out var confirmationRequest
            )
        )
            return false;
        confirmationRequest.callback.Invoke(false);
        ChatCommandAPI.confirmationRequests.Remove(caller.actualClientId);
        return true;
    }
}
