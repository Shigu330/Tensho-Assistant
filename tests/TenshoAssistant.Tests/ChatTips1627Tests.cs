using TenshoAssistant.Chat;

// 0.1627 (X-473, part 1): a player's name in the chat window shows their world, and their job and level only when the game shows them; an item
// link shows its icon and description.
internal static class ChatTips1627Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string W(string who,string where)=>who+" ("+where+")";
    private static string J(string job,int level)=>job+", level "+level;
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat tips 1627: the name without its world; the tooltip's lines, nothing guessed",()=>
        {
            Check(ChatCopy.NameOf("Ann Doe@Moogle")=="Ann Doe" && ChatCopy.NameOf(" Ann Doe ")=="Ann Doe" && ChatCopy.NameOf(null)=="","name");
            Check(ChatCopy.SenderTip("Ann Doe","Moogle","White Mage",100,W,J)=="Ann Doe (Moogle)\nWhite Mage, level 100","seen");
            Check(ChatCopy.SenderTip("Ann Doe","Moogle",null,0,W,J)=="Ann Doe (Moogle)","not seen: no job");
            Check(ChatCopy.SenderTip("Ann Doe","",null,0,W,J)=="Ann Doe" && ChatCopy.SenderTip("Ann Doe","Moogle","",90,W,J)=="Ann Doe (Moogle)","no world; no job name");
        }),
        ("Chat tips 1627: wiring: on hover only, never in capture mode, read from the party or nearby, nothing kept; the item's icon and description",()=>
        {
            var chat=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(chat.Contains("if(!chatLinesBlocked && run.Role==ChatRole.Sender && !options.CaptureNames && ChatChannels.Spoken(record.Channel))if(TipHovered())TipText(ChatSenderTip(record));"),"the name's tooltip");
            Check(chat.Contains("DrawGameIcon(item.IconId,") && chat.Contains("if(ChatPlayers?.Description(item.Id) is { Length:>0 } description)"),"the item's");
            var actions=Read("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs");
            Check(actions.Contains("public (string Job,int Level)? Seen(ChatSource source)") && actions.Contains("foreach(var member in party)") && actions.Contains("InRange(source) is { } character"),"party, then nearby");
            Check(!actions.Contains("seen[") && !actions.Contains("Seen.Add"),"nothing kept of the player");
        }),
    ];
}
