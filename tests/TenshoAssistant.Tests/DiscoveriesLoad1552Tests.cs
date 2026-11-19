using TenshoAssistant.Discoveries;

// 0.1552 (X-588, external audit of 18/11, family « discoveries: loading and export »): behaviour tests (R28.14). A01 the start's read that
// throws, tried again then given up cleanly; A17 a shared kind whose read fails, kept waiting and read again; an import that needs it stops
// before joining anything; A18 an export during the reading reads the kinds first and names the missing ones; A19 the summary cached.
internal static class DiscoveriesLoad1552Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,10,0,0,0,TimeSpan.Zero);

    private static DiscoveryFact Venture(uint subject)=>new(){Kind=DiscoveryKind.Venture,Subject=subject,Detail=7,LevelMin=1,LevelMax=1,Imported=1,First=At.AddDays(-3),Last=At.AddDays(-1)};
    private static DiscoveryFile Shared(params DiscoveryFact[] facts)=>new(){Written=At,Facts=[..facts]};
    private static void Until(Func<bool> done){for(var wait=0;!done() && wait<300;wait++)Thread.Sleep(10);}

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries load 1552: A01, the start's read tried again, each failure reported; still failing, it gives up cleanly; stopped, it ends",() =>
        {
            var calls=0;var said=new List<int>();
            var read=DiscoveryStart.ReadWithRetries(()=>++calls<3 ? throw new InvalidOperationException("database busy"):42,5,TimeSpan.FromMilliseconds(1),(_,attempt)=>said.Add(attempt),CancellationToken.None).Result;
            Check(read==(true,42) && said.SequenceEqual([1,2]),"read at the third try, two failures reported: "+read+" "+string.Join(",",said));
            said.Clear();
            var never=DiscoveryStart.ReadWithRetries<int>(()=>throw new IOException("damaged"),3,TimeSpan.FromMilliseconds(1),(_,attempt)=>said.Add(attempt),CancellationToken.None).Result;
            Check(never==(false,0) && said.SequenceEqual([1,2,3]),"given up after the last try, never thrown on");
            using var stop=new CancellationTokenSource();stop.Cancel();
            Check(DiscoveryStart.ReadWithRetries<int>(()=>throw new IOException("x"),5,TimeSpan.FromSeconds(30),(_,_)=>{},stop.Token).Result==(false,0),"the plugin unloading ends the wait");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs"));
            var retry=plugin.IndexOf("Discoveries.DiscoveryStart.ReadWithRetries(",StringComparison.Ordinal);
            Check(retry>0 && plugin.IndexOf("opened.ReadMine(",StringComparison.Ordinal)>retry && plugin.IndexOf("opened.SharedKinds()",StringComparison.Ordinal)>retry
                && plugin.Contains("else discoveries.MineIncomplete=true;",StringComparison.Ordinal),"the reads inside the retry; given up: nothing of yours sent");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs"));
            Check(page.IndexOf("DiscoveriesUi.ReadRetrying",StringComparison.Ordinal) is var said2 && said2>0 && said2<page.IndexOf("LoadingText(\"DiscoveriesUi.Loading\")",StringComparison.Ordinal),"said on the page while it loads");
        }),
        ("Discoveries load 1552: A17, a shared kind whose read fails stays waiting (never written over), is said, and is read again later",() =>
        {
            var store=new DiscoveryStore{SharedRetryMs=100_000};
            var reads=0;
            store.UseSharedSource(kind=>++reads==1 ? throw new IOException("held"):Shared(Venture(5)),[DiscoveryKind.Venture]);
            store.Find(DiscoveryKind.Venture,5u,5);
            Until(()=>store.SharedKindsFailed==1);
            Check(store.SharedKindsFailed==1 && store.SharedKindsWaiting==1 && !store.SharedKindsHeld().Contains(DiscoveryKind.Venture),"failed: still waiting, its file never written from memory");
            store.Find(DiscoveryKind.Venture,5u,5);Thread.Sleep(50);
            Check(reads==1,"not read again at once (no read at each frame)");
            store.SharedRetryMs=0;
            Until(()=>{store.Find(DiscoveryKind.Venture,5u,5);return store.SharedKindsWaiting==0;});
            Check(reads==2 && store.SharedKindsFailed==0 && store.Find(DiscoveryKind.Venture,5u,5).Count==1,"read again later: its facts there");
        }),
        ("Discoveries load 1552: A17, an import that needs a kind that cannot be read stops before joining anything",() =>
        {
            var store=new DiscoveryStore();
            store.UseSharedSource(_=>throw new IOException("held"),[DiscoveryKind.Venture]);
            var stopped=false;
            try{store.Merge([Venture(9)]);}
            catch(IOException){stopped=true;}
            Check(stopped && store.Summary().Count==0 && !store.Dirty && store.SharedKindsWaiting==1,"nothing joined, the kind still waiting");
        }),
        ("Discoveries load 1552: A18, an export reads the shared kinds still waiting first, and names those that cannot be read",() =>
        {
            var store=new DiscoveryStore();
            store.SeeSource(DiscoveryKind.Venture,5,7,2,At);
            store.UseSharedSource(kind=>kind==DiscoveryKind.Venture ? Shared(Venture(6)):throw new IOException("damaged"),[DiscoveryKind.Venture,DiscoveryKind.FishCatch]);
            var missing=store.ReadAllShared();
            var exported=store.Snapshot(At,false);
            Check(missing.SequenceEqual([DiscoveryKind.FishCatch]),"the kind that cannot be read is named: "+string.Join(",",missing));
            Check(exported.Facts.Count==2 && exported.Facts.Any(fact=>fact.Subject==6),"the shared kind read in time is in the export: "+exported.Facts.Count);
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs"));
            Check(page.IndexOf("var missing=store.ReadAllShared();",StringComparison.Ordinal) is var first && first>0
                && first<page.IndexOf("DiscoveryFiles.Write(path,store.Snapshot(DateTimeOffset.UtcNow,false));",StringComparison.Ordinal)
                && page.Contains("\"DiscoveriesUi.ExportPartial\"",StringComparison.Ordinal),"the export reads first and says what is missing");
        }),
        ("Discoveries load 1552: A19, the summary is worked out again only when the base changed",() =>
        {
            var store=new DiscoveryStore();
            store.SeeSource(DiscoveryKind.Venture,5,7,2,At);
            var first=store.Summary();
            Check(ReferenceEquals(first,store.Summary()),"nothing changed: the same rows, no new grouping");
            store.SeeSource(DiscoveryKind.Venture,8,7,2,At);
            var second=store.Summary();
            Check(!ReferenceEquals(first,second) && second.Single().Total==2 && second.Single().Seen==2,"a new sighting: worked out again");
            store.LoadShared(Shared(Venture(11)));
            Check(store.Summary().Single() is { Total:3, ImportedOnly:1 },"a shared kind read: worked out again");
        }),
    ];
}
