using TenshoAssistant.Chat;

// 0.1665 (X-697 M3, review of 0.1664: « Reply in the same mode » stayed greyed on yell, shout, free company, linkshells and novice network,
// while the game offers it there; the user's own example was a « Yell »): each spoken channel has its typing mode, a linkshell its index.
// docs/evidence/0.1665-chat-reply-modes.md.
internal static class ChatReplyModes1665Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat reply modes 1665: every spoken channel but tells has its mode; a linkshell its index from 0; nothing for the others",()=>
        {
            Check(ChatPlayerMenu.ReplyChannel(ChatChannel.Yell)==4 && ChatPlayerMenu.ReplyChannel(ChatChannel.Shout)==5 && ChatPlayerMenu.ReplyChannel(ChatChannel.FreeCompany)==6
                  && ChatPlayerMenu.ReplyChannel(ChatChannel.Novice)==8,"yell, shout, free company, novice network");
            Check(ChatPlayerMenu.ReplyChannel(ChatChannel.Cwls1)==9 && ChatPlayerMenu.ReplyChannel(ChatChannel.Cwls8)==16 && ChatPlayerMenu.ReplyLinkshell(ChatChannel.Cwls3)==2,"cross-world linkshells");
            Check(ChatPlayerMenu.ReplyChannel(ChatChannel.Ls1)==19 && ChatPlayerMenu.ReplyChannel(ChatChannel.Ls8)==26 && ChatPlayerMenu.ReplyLinkshell(ChatChannel.Ls1)==0 && ChatPlayerMenu.ReplyLinkshell(ChatChannel.Ls8)==7,"linkshells");
            Check(ChatPlayerMenu.ReplyLinkshell(ChatChannel.Yell)==0 && ChatPlayerMenu.ReplyChannel(ChatChannel.System) is null && ChatPlayerMenu.ReplyChannel(ChatChannel.TellIn) is null,"the others");
            foreach(var channel in ChatChannels.All.Where(ChatChannels.Spoken).Where(c=>c is not (ChatChannel.TellIn or ChatChannel.TellOut)))
                Check(ChatPlayerMenu.ReplyChannel(channel) is not null,"a mode for "+channel);
        }),
        ("Chat reply modes 1665: the click gives the linkshell's index with the mode; only the typing box changes",()=>
        {
            var actions=Read("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs");
            Check(actions.Contains("ChangeChatChannel(mode,ChatPlayerMenu.ReplyLinkshell(channel),null,true);"),"index given");
        }),
    ];
}
