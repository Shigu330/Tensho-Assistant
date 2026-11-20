using TenshoAssistant.Chat;

// 0.1658 (X-679, the user's report of 20/11 around 21:50): a player's menu in the chat window showed none of the game's entries for messages
// received right after a reload: the chat history put back (ChatStore.Restore, 10 to 30 s after the load) numbers every line again, this
// session's too, and the senders kept by the old numbers (ChatSources) were lost. They follow their lines now.
internal static class ChatSenders1658Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private const ushort Say=10,Yell=30;
    private const ulong Own=9;

    private static ChatRecord Add(ChatStore store,int kind,string sender,DateTimeOffset at)
        =>store.Add(at,kind,sender,[new ChatSegment("hello")],1000);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat senders 1658: behaviour: the history put back after a reload numbers the lines again; the senders and their identifiers follow",()=>
        {
            var store=new ChatStore();
            var sources=new ChatSources();
            store.Renumbered+=sources.Renumber;
            var now=DateTimeOffset.UtcNow;
            var said=Add(store,Say,"Aya Lune",now);
            sources.Expect(said.Id,"Aya Lune",73,Say);
            Check(sources.Resolve(1001,2001,73,Say,new ChatLogLine(Say,"Aya Lune",73),Own)==ChatSourceMatch.Given,"given before the history comes back");
            var yell=Add(store,Yell,"Bo Ran",now.AddSeconds(1));
            sources.Expect(yell.Id,"Bo Ran",74,Yell);   // waiting for its identifiers when the history comes back
            var older=new List<ChatRecord>{new(1,now.AddMinutes(-5),Say,ChatChannels.From(Say),"Old Friend",[new ChatSegment("before")])};
            Check(store.Restore(older,1000)==1,"one line put back");
            var lines=store.After(0);
            var newSaid=lines.Single(line=>line.Sender=="Aya Lune").Id;
            var newYell=lines.Single(line=>line.Sender=="Bo Ran").Id;
            Check(newSaid!=said.Id && newYell!=yell.Id,"numbered again");
            Check(sources.For(newSaid) is { ContentId:1001,AccountId:2001,World:73 } && sources.For(said.Id) is null,"the identifiers given follow the line");
            Check(sources.Resolve(1002,2002,74,Yell,new ChatLogLine(Yell,"Bo Ran",74),Own)==ChatSourceMatch.Given && sources.For(newYell) is { ContentId:1002 },
                "the message that waited gets its identifiers under its new number");
            Check(lines.Single(line=>line.Sender=="Old Friend") is var old && sources.For(old.Id) is null,"a line read back from disk has no sender (never kept on disk)");
            var menu=ChatPlayerMenu.Entries(sources.For(newYell),ChatChannel.Yell,false,false);
            Check(menu.Count>0 && menu.First().Action==ChatPlayerAction.Tell && menu.First().Enabled,"the game's entries back, Tell enabled");
        }),
        ("Chat senders 1658: behaviour: numbers unknown to the senders change nothing; the bound and the order are kept",()=>
        {
            var sources=new ChatSources();
            sources.Expect(5,"Aya Lune",73,Say);
            sources.Renumber(new Dictionary<long,long>{[99]=100});
            Check(sources.For(5) is { Name:"Aya Lune" } && sources.Count==1,"an unknown number: nothing moved");
            sources.Renumber(new Dictionary<long,long>{[5]=50});
            Check(sources.For(50) is { Name:"Aya Lune" } && sources.For(5) is null && sources.Count==1,"moved once, never twice");
            for(var id=1000L;id<1000+ChatSources.MaximumKept+5;id++)sources.Expect(id,"P",1,Say);
            Check(sources.Count==ChatSources.MaximumKept && sources.For(50) is null,"the oldest still goes first after a renumbering");
        }),
        ("Chat senders 1658: wiring: the plugin follows the store's renumbering; each outcome said, NotWaiting and a failure too, and the first call's delay",()=>
        {
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("registrations.Register(()=>mainWindow.ChatMessages.Renumbered+=chatSenders.Renumber,()=>mainWindow.ChatMessages.Renumbered-=chatSenders.Renumber);",StringComparison.Ordinal),"subscribed and released");
            var store=Read("src/TenshoAssistant/Chat/ChatStore.cs");
            Check(store.Contains("if(ids.Count>0)Renumbered?.Invoke(ids);",StringComparison.Ordinal) && store.IndexOf("Renumbered?.Invoke(ids)",StringComparison.Ordinal)<store.IndexOf("private int Put(",StringComparison.Ordinal),
                "said outside the store's lock");
            var actions=Read("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs");
            Check(!actions.Contains("match==ChatSourceMatch.NotWaiting ||",StringComparison.Ordinal) && !actions.Contains("catch(Exception){}",StringComparison.Ordinal)
                  && actions.Contains("first call {0:0.0} s after the load",StringComparison.Ordinal),"NotWaiting said, no silent failure, the delay measured");
        }),
    ];
}
