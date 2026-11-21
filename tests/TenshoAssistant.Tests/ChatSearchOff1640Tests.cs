using TenshoAssistant.Chat;

// 0.1640 (X-670, review of 0.1638): the chat search goes through the lines kept off the game's thread (M1: 11 to 17 ms a letter on it at
// 10 000 lines); in capture mode it finds a sender by its stand-in, as shown (M3); a member has one summary (M2).
internal static class ChatSearchOff1640Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly TimeSpan Long=TimeSpan.FromSeconds(30);
    private static readonly DateTimeOffset At=new(2026,11,20,19,0,0,TimeSpan.Zero);
    private static ChatTabSettings Say()=>new(){Name="Say",Channels=[ChatChannel.Say]};
    private static void Put(ChatStore store,string sender,string text)=>store.Add(At,10,sender,[new ChatSegment(text)],100);
    private static Action<ChatTabView,Func<bool>> Fill(ChatStore store,ChatTabSettings tab,string query)
        =>(fresh,stale)=>fresh.Refresh(store,tab,100,null,null,record=>stale() ? throw new OperationCanceledException():ChatSearch.Matches(record,query));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat search off 1640: behaviour: the lines kept are gone through on another thread; nothing shown before it ends, then that view, the same after",()=>
        {
            var store=new ChatStore();
            var tab=Say();
            Put(store,"Ann Doe","hello");Put(store,"Bob Roe","night");
            using var gate=new ManualResetEventSlim();
            var thread=-1;
            void Blocked(ChatTabView fresh,Func<bool> stale){thread=Environment.CurrentManagedThreadId;gate.Wait(Long);Fill(store,tab,"hello")(fresh,stale);}
            var (first,ready)=store.Searched(tab,"hello",Blocked);
            Check(first is null && !ready,"nothing yet");
            Check(SpinWait.SpinUntil(()=>Volatile.Read(ref thread)>=0,Long) && thread!=Environment.CurrentManagedThreadId,"another thread");
            Check(store.Searched(tab,"hello",Blocked) is (null,false),"still being filled: not asked twice, nothing shown");
            gate.Set();
            ChatTabView? view=null;
            Check(SpinWait.SpinUntil(()=>store.Searched(tab,"hello",Blocked) is ({ } found,true) && (view=found) is not null,Long),"then ready");
            Check(view!.Count==1 && view.RecordAt(0).Sender=="Ann Doe","its messages");
            Check(store.Searched(tab,"hello",Blocked) is ({ } same,true) && ReferenceEquals(same,view),"the same view after");
        }),
        ("Chat search off 1640: behaviour: a newer text stops the older search, whose view is never shown; meanwhile the last results of the tab as they were, none for another tab",()=>
        {
            var store=new ChatStore();
            var tab=Say();
            Put(store,"Ann Doe","hello");Put(store,"Cid Moe","help");
            ChatTabView? hel=null;
            Check(SpinWait.SpinUntil(()=>store.Searched(tab,"hel",Fill(store,tab,"hel")) is ({ } found,true) && (hel=found) is not null,Long),"first text ready");
            using var gate=new ManualResetEventSlim();
            var stopped=false;
            var (shown,ready)=store.Searched(tab,"hell",(fresh,stale)=>{gate.Wait(Long);stopped=stale();});
            Check(ReferenceEquals(shown,hel) && !ready,"while « hell » is searched, the results of « hel » as they were");
            _=store.Searched(tab,"hello",Fill(store,tab,"hello"));
            gate.Set();
            Check(SpinWait.SpinUntil(()=>Volatile.Read(ref stopped),Long),"the older search told it is useless");
            ChatTabView? hello=null;
            Check(SpinWait.SpinUntil(()=>store.Searched(tab,"hello",Fill(store,tab,"hello")) is ({ } found,true) && (hello=found) is not null,Long) && hello!.Count==1,"the newest text's view");
            var (elsewhere,elsewhereReady)=store.Searched(new ChatTabSettings{Name="Other",Channels=[ChatChannel.Say]},"hello",(fresh,stale)=>Thread.Sleep(200));
            Check(elsewhere is null && !elsewhereReady,"another tab: not this tab's results");
        }),
        ("Chat search off 1640: behaviour: a clear or the search closed while a search runs: its view is never shown, no copy kept, asked again: a new search",()=>
        {
            var store=new ChatStore();
            var tab=Say();
            Put(store,"Ann Doe","hello");
            using var gate=new ManualResetEventSlim();
            var fills=0;
            void Counted(ChatTabView fresh,Func<bool> stale){Interlocked.Increment(ref fills);gate.Wait(Long);Fill(store,tab,"hello")(fresh,stale);}
            store.Searched(tab,"hello",Counted);
            Check(SpinWait.SpinUntil(()=>Volatile.Read(ref fills)==1,Long),"first search running");
            store.Clear();
            gate.Set();
            Thread.Sleep(100);
            Check(store.Searched(tab,"hello",Fill(store,tab,"hello")) is (null,false),"the cleared search's view never shown");
            Check(SpinWait.SpinUntil(()=>store.Searched(tab,"hello",Fill(store,tab,"hello")) is (_,true),Long),"a new search, ready");
            store.EndSearch();
            Check(store.Searched(tab,"hello",(fresh,stale)=>Thread.Sleep(200)) is (null,false),"closed: dropped, a new search asked");
        }),
        ("Chat search off 1640: in capture mode a sender is found by its stand-in as shown, not by its name; a story character by its name; the lines take the same sender",()=>
        {
            var said=new ChatRecord(1,At,10,ChatChannel.Say,"Ann Doe@Moogle",[new ChatSegment("hello")]);
            var stand=ChatFormat.StandIn("Ann Doe");
            Check(ChatSearch.Matches(said,stand,capture:true) && ChatSearch.Matches(said,stand.ToLowerInvariant(),capture:true),"the code shown, case aside: "+stand);
            Check(!ChatSearch.Matches(said,"Ann",capture:true) && ChatSearch.Matches(said,"Ann") && !ChatSearch.Matches(said,stand),"the name only without capture mode");
            Check(ChatSearch.Matches(said,"hello",capture:true),"the text as it is");
            var npc=new ChatRecord(2,At,61,ChatChannel.Npc,"Cid Moe",[new ChatSegment("hi")]);
            Check(ChatSearch.Matches(npc,"Cid",capture:true),"a character of the story keeps its name");
            var line=string.Concat(ChatFormat.Runs(said,new ChatSettings{ShowTimestamps=false,ShowChannel=false,CaptureNames=true},_=>"").Select(run=>run.Text));
            Check(line==ChatFormat.ShownSender(said,true)+": hello" && ChatFormat.ShownSender(said,false)=="Ann Doe@Moogle","the lines write the same sender: "+line);
        }),
        ("Chat search off 1640: a member has one summary: none twice in the chat's code; in the whole source, no more than the 18 left",()=>
        {
            static IEnumerable<string> Doubles(string file)
            {
                var lines=File.ReadAllLines(file);
                var count=0;var start=0;
                for(var at=0;at<lines.Length;at++)
                {
                    var line=lines[at].Trim();
                    if(line.StartsWith("///",StringComparison.Ordinal)){if(line.Contains("<summary>",StringComparison.Ordinal) && count++==0)start=at;continue;}
                    if(count>1)yield return Path.GetFileName(file)+":"+(start+1);
                    count=0;
                }
            }
            var all=Directory.GetFiles(Path.GetFullPath("src"),"*.cs",SearchOption.AllDirectories)
                .Where(file=>!file.Contains(Path.DirectorySeparatorChar+"obj"+Path.DirectorySeparatorChar,StringComparison.Ordinal)
                    && !file.Contains(Path.DirectorySeparatorChar+"bin"+Path.DirectorySeparatorChar,StringComparison.Ordinal))
                .SelectMany(Doubles).ToArray();
            var chat=all.Where(place=>place.StartsWith("Chat",StringComparison.Ordinal) || place.StartsWith("MainWindow.Chat",StringComparison.Ordinal)).ToArray();
            Check(chat.Length==0,"two summaries on one member: "+string.Join(", ",chat));
            Check(all.Length<=18,"no new member with two summaries ("+all.Length+"): "+string.Join(", ",all));
        }),
        ("Chat search off 1640: wiring: the window asks the store's search with the capture mode in its key, refreshes only a ready view, says « Searching… », drops the view when closed",()=>
        {
            var chat=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(chat.Contains("bool Holds(ChatRecord record)=>ChatSearch.Matches(record,query,capture);",StringComparison.Ordinal),"capture mode");
            Check(chat.Contains("if(ready && found is not null)found.Refresh(ChatMessages,tab,cap,table,options,Holds);",StringComparison.Ordinal),"only a ready view refreshed");
            Check(chat.Contains("localization.Get(\"ChatWindow.Searching\")",StringComparison.Ordinal) && chat.Contains("ChatMessages.EndSearch();",StringComparison.Ordinal),"searching, closed");
            foreach(var language in new[]{"en","fr"})
                Check(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")).Contains("\"Searching\":",StringComparison.Ordinal),language);
        }),
    ];
}
