using System.Numerics;
using TenshoAssistant.Maps;

// 0.373 (g, M-03): chat map links — session history (bounded, repeats folded, filter by channel), nearest aetheryte among the
// teleportable ones, reader on ChatMessageUnhandled cleared at logout, teleport only through the usual confirmation.
internal static class ChatMapLinks373Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static readonly DateTimeOffset At = new(2026,11,3,12,0,0,TimeSpan.Zero);
    private static ChatMapLink Link(float x,ChatLinkChannel channel=ChatLinkChannel.Shout,string sender="A",int seconds=0,uint map=10)
        => new(At.AddSeconds(seconds),channel,sender,100,map,x,20,"Place");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat map links: newest first, the same point from the same sender within a minute folded, at most 100",() =>
        {
            var history = new ChatMapLinkHistory();
            history.Add(Link(10));history.Add(Link(12,seconds:5));history.Add(Link(10,seconds:30));
            Check(history.Links.Count==2 && history.Links[0].X==10 && history.Links[0].At==At.AddSeconds(30),"repeat moved to the top");
            history.Add(Link(10,seconds:200));
            Check(history.Links.Count==3,"after a minute it is a new call");
            history.Add(Link(10,sender:"B",seconds:201));
            Check(history.Links.Count==4,"another sender is kept");
            history.Add(Link(float.NaN,seconds:202));history.Add(Link(5,map:0));
            Check(history.Links.Count==4,"invalid links ignored");
            for (var index = 0; index < 150; index++) history.Add(Link(1+index*0.1f,seconds:300+index));
            Check(history.Links.Count==ChatMapLinkHistory.Max,"bounded");
            history.Clear();Check(history.Links.Count==0,"cleared");
        }),
        ("Chat map links: filter by channel (null = every channel)",() =>
        {
            IReadOnlyList<ChatMapLink> links = [Link(1,ChatLinkChannel.Shout),Link(2,ChatLinkChannel.FreeCompany),Link(3,ChatLinkChannel.Linkshell)];
            Check(ChatMapLinkHistory.Filter(links,null).Count==3,"all");
            var kept = ChatMapLinkHistory.Filter(links,new HashSet<ChatLinkChannel>{ChatLinkChannel.FreeCompany,ChatLinkChannel.Linkshell});
            Check(kept.Count==2 && kept.All(link=>link.Channel!=ChatLinkChannel.Shout),"filtered");
        }),
        ("Chat map links: nearest aetheryte of the map, only teleportable ones when the teleport list is known",() =>
        {
            (uint,float,float)[] markers = [(1,100,100),(2,1000,1000),(3,1900,1900),(0,999,999)];
            Check(ChatMapLinks.NearestAetheryte(markers,new Vector2(950,950),null)==2,"nearest of all");
            Check(ChatMapLinks.NearestAetheryte(markers,new Vector2(950,950),new Dictionary<uint,uint>{[1]=100,[3]=300})==1,"only attuned");
            Check(ChatMapLinks.NearestAetheryte(markers,new Vector2(950,950),new Dictionary<uint,uint>())==null,"none attuned");
            Check(ChatMapLinks.NearestAetheryte([],new Vector2(1,1),null)==null,"no aetheryte");
        }),
        ("Chat map links: read after the other plugins, cleared at logout, never saved, teleport only through the question",() =>
        {
            var watcher = Read("Maps/ChatMapLinkWatcher.Dalamud.cs");
            Check(watcher.Contains("()=>chat.ChatMessageUnhandled+=OnMessage,",StringComparison.Ordinal) && watcher.Contains("()=>chat.ChatMessageUnhandled-=OnMessage)",StringComparison.Ordinal)
                && watcher.Contains("private void OnLogout(int type,int code)=>History.Clear();",StringComparison.Ordinal),"subscription owned and cleaned");
            Check(!Read("Configuration.cs").Contains("ChatMapLink",StringComparison.Ordinal),"history never in the configuration");
            var page = Read("MainWindow.ChatMapLinks.cs");
            // 0.399: a direct teleport only for a request marked Direct (« Go there » setting, on its click), and only with a known cost.
            Check(page.Contains("mapTeleportRequested=true;",StringComparison.Ordinal) && page.Split("Teleport(").Length==2
                && page.Contains("if(request.Direct && !failed && costs?.ContainsKey(found)==true){mapLive?.Teleport(found);return;}",StringComparison.Ordinal),"question by default, direct only when asked");
            Check(Read("MainWindow.MapView.cs").Contains("ResolveNearestAetheryte(map);",StringComparison.Ordinal),"resolved by the map canvas");
        }),
    ];
}
