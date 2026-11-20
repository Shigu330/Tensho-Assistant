using TenshoAssistant.Chat;

// 0.1638 (X-447 B8, the user's choice of 15/11): search in the tab shown: the messages whose sender or text holds what was typed, case and
// accents aside; a view of their own, kept by the store and dropped with its other copies; nothing saved.
internal static class ChatSearch1638Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,18,0,0,TimeSpan.Zero);
    private static ChatRecord Said(long id,string sender,params string[] pieces)=>new(id,At,10,ChatChannel.Say,sender,[..pieces.Select(piece=>new ChatSegment(piece))]);
    private static ChatTabSettings Say()=>new(){Name="Say",Channels=[ChatChannel.Say]};
    private static void Put(ChatStore store,string sender,string text)=>store.Add(At,10,sender,[new ChatSegment(text)],100);
    // 0.1640 (X-670 M1): the search's view is filled on a task (ChatStore.Searched): asked until it is ready.
    private static ChatTabView Ready(ChatStore store,ChatTabSettings tab,string query)
    {
        ChatTabView? view=null;
        Check(SpinWait.SpinUntil(()=>{var (found,ready)=store.Searched(tab,query,(fresh,stale)=>fresh.Refresh(store,tab,100,null,null,record=>ChatSearch.Matches(record,query)));view=found;return ready;},
            TimeSpan.FromSeconds(30)),"filled: "+query);
        return view!;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat search 1638: a message matches by its sender or its text, case and accents aside, across its pieces; empty: all; the text trimmed",()=>
        {
            var party=Said(1,"Ann Doe@Moogle","Une fête ce ","soir ?");
            Check(ChatSearch.Matches(party,"fete") && ChatSearch.Matches(party,"FÊTE") && ChatSearch.Matches(party,"ce soir"),"text, case and accents aside, across pieces");
            Check(ChatSearch.Matches(party,"ann doe") && !ChatSearch.Matches(party,"Bob"),"sender");
            Check(ChatSearch.Matches(party,"") && ChatSearch.Query("  fete ")=="fete" && ChatSearch.Query(null)=="","empty and trimmed");
        }),
        ("Chat search 1638: behaviour: the search view holds only the tab's messages holding the text, new ones included; the same view while the text stays",()=>
        {
            var store=new ChatStore();
            var tab=Say();
            Put(store,"Ann Doe","hello there");
            Put(store,"Bob Roe","good night");
            Put(store,"Cid Moe","Hello again");
            var view=Ready(store,tab,"hello");
            Check(view.Count==2 && view.RecordAt(0).Sender=="Ann Doe" && view.RecordAt(1).Sender=="Cid Moe","two of three");
            Put(store,"Dee Lin","hello!");
            var same=Ready(store,tab,"hello");
            same.Refresh(store,tab,100,null,null,record=>ChatSearch.Matches(record,"hello"));
            Check(ReferenceEquals(same,view) && same.Count==3,"the same view, the new message in");
            Check(!ReferenceEquals(Ready(store,tab,"night"),view),"another text: another view");
            var all=store.ViewOf(tab);
            all.Refresh(store,tab,100);
            Check(all.Count==4,"the tab itself keeps all its messages: "+all.Count);
        }),
        ("Chat search 1638: behaviour: a clear or a tab restarted drops the search view (no copy of the messages left)",()=>
        {
            var store=new ChatStore();
            var tab=Say();
            Put(store,"Ann Doe","hello");
            var view=Ready(store,tab,"hello");
            store.Clear();
            var after=Ready(store,tab,"hello");
            Check(!ReferenceEquals(after,view) && after.Count==0,"cleared: a new, empty view");
            store.Restart(tab);
            Check(!ReferenceEquals(Ready(store,tab,"hello"),after),"restarted: a new view");
        }),
        ("Chat search 1638: wiring: the magnifier at the end of the tab bar, the search line over the lines, the texts in both languages",()=>
        {
            var chat=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(chat.Contains("GlyphTabButton(\"###ChatSearch\",Dalamud.Interface.FontAwesomeIcon.Search,",StringComparison.Ordinal),"the button");
            Check(chat.Contains("if(chatSearchOpen)view=DrawChatSearchLine(tabs[shown],view,options);",StringComparison.Ordinal),"the line before the lines");
            Check(chat.Contains("var (found,ready)=ChatMessages.Searched(tab,",StringComparison.Ordinal),"the store's view (0.1640: filled off the game's thread)");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                foreach(var key in new[]{"SearchTip","SearchCloseTip","SearchHint","SearchBoxTip","SearchFound"})Check(text.Contains($"\"{key}\":",StringComparison.Ordinal),language+" "+key);
            }
        }),
    ];
}
