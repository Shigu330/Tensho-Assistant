using TenshoAssistant.Chat;

// 0.1654 (X-447 B10, the user's choice of 15/11, after WIM): each player's tells in a tab of their own, in memory only.
internal static class ChatConversations1654Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset Start=new(2026,11,20,22,30,0,TimeSpan.Zero);
    private static ChatRecord Add(ChatStore store,int seconds,int kind,string sender,string text)=>store.Add(Start.AddSeconds(seconds),kind,sender,[new ChatSegment(text)],500);
    private static readonly List<ChatTabSettings> Closed=[];
    private static void Gone(ChatTabSettings tab)=>Closed.Add(tab);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat conversations 1654: behaviour: only tells after the start open a tab, one per player (received or sent), named without the world; others and hidden players open none",()=>
        {
            var store=new ChatStore();
            var talks=new ChatConversations();
            Add(store,0,13,"Old Friend@Ultros","from before");
            Check(talks.Update(store,_=>false,null,0xFFB8DE,Gone).Count==0 && talks.Tabs.Count==0,"the start opens nothing: the tells kept before are not new");
            var first=Add(store,1,13,"Ann Doe@Ultros","hello");
            Add(store,2,12,"Ann Doe@Ultros","hi Ann");
            Add(store,3,10,"Bob Roe","a say opens nothing");
            Add(store,4,13,"Hidden Guy","psst");
            Add(store,5,12,"\uE05DCid Moe","sent first");
            var opened=talks.Update(store,_=>false,"Hidden Guy",0xFFB8DE,Gone);
            Check(opened.Count==2 && opened[0].From==first.Id && talks.Tabs.Select(tab=>tab.Name).SequenceEqual(["Ann Doe","Cid Moe"]),string.Join(", ",talks.Tabs.Select(tab=>tab.Name)));
            var ann=talks.Tabs[0];
            Check(ann.Conversation=="Ann Doe@Ultros" && ann.Channels.SequenceEqual([ChatChannel.TellIn,ChatChannel.TellOut]) && ann.NameColor==0xFFB8DE,"Ann's tab");
            Check(talks.Find("ann doe@ultros")==ann && talks.Find("\uE05DCid Moe")==talks.Tabs[1] && talks.Find("")==null,"found by player, capitals and icons aside");
            Add(store,6,13,"ANN DOE@Ultros","again");
            Check(talks.Update(store,_=>false,null,0,Gone).Count==0 && talks.Tabs.Count==2,"the same player: the same tab");
        }),
        ("Chat conversations 1654: behaviour: a conversation's tab shows the tells with its player both ways, and only those; the tell that opened it counts as new",()=>
        {
            var store=new ChatStore();
            var talks=new ChatConversations();
            talks.Update(store,_=>false,null,0,Gone);
            Add(store,1,13,"Ann Doe@Ultros","hello");
            Add(store,2,13,"Bob Roe","not Ann");
            Add(store,3,12,"Ann Doe@Ultros","hi Ann");
            Add(store,4,10,"Ann Doe@Ultros","a say of Ann");
            var (tab,from)=talks.Update(store,_=>false,null,0,Gone).First(row=>row.Tab.Name=="Ann Doe");
            var view=store.ViewOf(tab);
            view.CountFrom(from);
            view.Refresh(store,tab,500,null,new ChatSettings());
            var texts=Enumerable.Range(0,view.Count).Select(at=>string.Concat(view.RecordAt(at).Body.Select(piece=>piece.Text))).ToArray();
            Check(texts.SequenceEqual(["hello","hi Ann"]),string.Join(" | ",texts));
            Check(view.Unread==1,$"the tell received is new, the one sent is not: {view.Unread}");
            var plain=new ChatTabView();
            plain.Refresh(store,new ChatTabSettings{Channels=[ChatChannel.TellIn]},500);
            Check(plain.Unread==0 && plain.Count==2,"a new view of another tab still takes what is kept as read");
            plain.CountFrom(1);
            Check(plain.Unread==0,"after its first refresh, CountFrom changes nothing");
        }),
        ("Chat conversations 1654: behaviour: at most 6; the least recent one not busy leaves its place (told), all busy: none; close; a logout and the history put back open nothing",()=>
        {
            Closed.Clear();
            var store=new ChatStore();
            var talks=new ChatConversations();
            talks.Update(store,_=>false,null,0,Gone);
            for(var player=0;player<ChatConversations.Maximum;player++)Add(store,player,13,$"Player {player}","hi");
            talks.Update(store,_=>false,null,0,Gone);
            Check(talks.Tabs.Count==ChatConversations.Maximum,"six");
            Add(store,10,13,"Player 0","again");   // Player 0 is the most recent now: Player 1 is the least recent
            Add(store,11,13,"Player 6","hi");
            var keep=talks.Find("Player 2")!;
            talks.Update(store,tab=>tab==talks.Find("Player 1"),null,0,Gone);
            Check(talks.Tabs.Count==6 && talks.Find("Player 6") is not null && talks.Find("Player 2") is null && talks.Find("Player 1") is not null && Closed.SequenceEqual([keep]),
                "Player 1 busy, Player 2 the least recent after it leaves: "+string.Join(", ",talks.Tabs.Select(tab=>tab.Name)));
            Add(store,12,13,"Player 7","hi");
            Check(talks.Update(store,_=>true,null,0,Gone).Count==0 && talks.Find("Player 7") is null,"all busy: no tab (the tell stays in the other tabs)");
            Check(talks.Close(talks.Find("Player 6")!) && talks.Tabs.Count==5 && !talks.Close(new ChatTabSettings()),"closed");
            store.Restore([new ChatRecord(1,Start.AddSeconds(-100),13,ChatChannel.TellIn,"From History",[new ChatSegment("old")])],500);
            Check(talks.Update(store,_=>false,null,0,Gone).Count==0 && talks.Find("From History") is null,"the history put back opens nothing");
            Add(store,20,13,"After Restore","hi");
            Check(talks.Update(store,_=>false,null,0,Gone).Count==1,"a tell after it does");
            Closed.Clear();
            store.Clear();
            talks.Update(store,_=>false,null,0,Gone);
            Check(talks.Tabs.Count==0 && Closed.Count==6,"a logout closes them all, each told");
            Add(store,30,13,"Next One","hi");
            Check(talks.Update(store,_=>false,null,0,Gone).Count==1,"then the tells that come open tabs again");
            talks.Clear(Gone);
            Check(talks.Tabs.Count==0,"cleared");
        }),
        ("Chat conversations 1654: privacy: the tabs are never in the settings' tabs (never saved, R04); capture mode shows the stand-in on the tab (R25)",()=>
        {
            var settings=new ChatSettings();
            var saved=settings.EnsureTabs("General","Battle","Event","Social");
            var store=new ChatStore();
            var talks=new ChatConversations();
            talks.Update(store,_=>false,null,0,Gone);
            Add(store,1,13,"Ann Doe@Ultros","hello");
            talks.Update(store,_=>false,null,0,Gone);
            Check(talks.Tabs.Count==1 && saved.Count==4 && saved.All(tab=>tab.Conversation is null) && settings.Tabs!.All(tab=>tab.Conversation is null),"not in the settings");
            var tab=talks.Tabs[0];
            Check(ChatConversations.Label(tab,capture:false)=="Ann Doe" && ChatConversations.Label(tab,capture:true)==ChatFormat.StandIn("Ann Doe") && !ChatConversations.Label(tab,true).Contains("Ann",StringComparison.Ordinal),"capture mode");
            Check(ChatConversations.Label(saved[0],true)=="General","a tab of the settings keeps its name");
            Check(!new ChatSettings().Conversations,"off by default");
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n","\n");
            Check(!window.Contains("ChatTabs(options).Add",StringComparison.Ordinal) && !window.Contains("options.Tabs.Add",StringComparison.Ordinal)
                && window.Contains("chatWindowTabs.AddRange(ChatTabs(options));",StringComparison.Ordinal)
                && window.Contains("if(options.Conversations)chatWindowTabs.AddRange(chatConversations.Tabs);",StringComparison.Ordinal),"the window shows them beside the settings' tabs, never among them");
        }),
        ("Chat conversations 1654: wiring: updated before the tabs are drawn, the tell counted as new, a menu entry and a tab entry, closed tabs forgotten, the setting and its texts",()=>
        {
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n","\n");
            Check(window.Contains("UpdateChatConversations(options);\n        var tabs=ChatWindowTabs(options);",StringComparison.Ordinal)
                && window.Contains("ChatMessages.ViewOf(opened).CountFrom(from);",StringComparison.Ordinal)
                && window.Contains("private void ChatConversationClosed(ChatTabSettings tab){ChatMessages.Restart(tab);ChatMessages.Unpin(tab);}",StringComparison.Ordinal)
                && window.Contains("localization.Format(\"ChatWindow.MenuConversation\",shown)",StringComparison.Ordinal)
                && window.Contains("localization.Get(\"ChatWindow.CloseConversation\")",StringComparison.Ordinal)
                && window.Contains("BoolSetting(\"ChatWindow.Conversations\",\"ChatWindow.ConversationsTip\"",StringComparison.Ordinal),"the window");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                foreach(var key in new[]{"Conversations","ConversationsTip","MenuConversation","CloseConversation"})Check(text.Contains($"\"{key}\":",StringComparison.Ordinal),language+" "+key);
            }
        }),
    ];
}
