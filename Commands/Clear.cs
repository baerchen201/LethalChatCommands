namespace ChatCommandAPI.Commands;

public class Clear : Command
{
    public override string Description => "Clears the chat history";

    public override bool Hidden => true;

    public override void Invoke(string args)
    {
        var hudManager = HUDManager.Instance;
        hudManager.lastChatMessage = null!;
        hudManager.ChatMessageHistory.Clear();
        hudManager.chatText.text = string.Empty;
    }
}
