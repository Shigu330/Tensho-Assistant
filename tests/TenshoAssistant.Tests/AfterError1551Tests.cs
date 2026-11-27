using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Beastmaster;
using TenshoAssistant.Discoveries;
using TenshoAssistant.Inventory;
using TenshoAssistant.Market;

// 0.1551 (X-587, external audit of 18/11, family « the player's data after an error »): behaviour tests (R28.14), each the case of the audit
// replayed: A16 a base not read whole then an exchange, A03 a plans' file held or damaged then a save, A14 a gil write that fails then a
// character change, A20 the plugin's base damaged under a readable copy, A15 a file at the size bound written then read, A27 a look at the
// board during the start's load.
internal static class AfterError1551Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=DateTimeOffset.UtcNow.AddHours(-1);
    // X-672 (0.1645): the writes are waited for on their own signal (the writer's queue done, the exchange no longer busy); this guard only
    // stops a test that really hangs. A bound of 5 s failed A14 once in CI under load: the writer's tasks run on the thread pool, which a
    // loaded machine starts seconds late.
    private static readonly TimeSpan Guard=TimeSpan.FromMinutes(2);

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-1551-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{SqliteConnection.ClearAllPools();try{Directory.Delete(root,true);}catch(IOException){}}
    }

    /// <summary>A fake GitHub: no common set served (404), the API calls recorded and answered « {} ».</summary>
    private sealed class Server : HttpMessageHandler
    {
        public readonly List<string> Asked=[];
        public readonly List<(string Method,string Address)> Calls=[];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();
            lock(Asked)Asked.Add(address);
            if(request.Method==HttpMethod.Get)return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            lock(Calls)Calls.Add((request.Method.Method,address));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{}")});
        }
    }

    private static ExchangeMessage Run(GitHubExchange exchange,DiscoveryStore store)
    {
        Check(exchange.Exchange(store,true),"the exchange starts");
        SpinWait.SpinUntil(()=>!exchange.Busy,Guard);
        Check(!exchange.Busy && exchange.Message is { Key:"ExchangeDone" },$"the exchange ends ({exchange.Message?.Key} {string.Join(", ",exchange.Message?.Args ?? [])})");
        return exchange.Message!;
    }

    private static GilEntry Gil(int minutes,long delta,long balance)=>new(DateTimeOffset.UtcNow.AddMinutes(-minutes),delta,balance,GilSource.Character,GilCategory.Other);
    private static void Warn(Exception error,string message){}

    private static SeenMarketEntry Seen(uint item,DateTimeOffset at,decimal price)=>
        new(){World=40,Item=item,Listings=[new SeenListing(price,false,1)],ListingsAt=at,ListingsRequest=1,Sales=[new SeenSale(price,false,1,at)],SalesAt=at};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("After error 1551: A16, a base not read whole sends nothing to the gist; the common data are still asked; once whole, it is sent",() => InTemp(root =>
        {
            File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="tester",GistId="g1",Issue=7,ClientId=GitHubSignIn.Current.ClientId}));
            File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
            var server=new Server();
            using var exchange=new GitHubExchange(root,server);
            Check(exchange.Connected,"connected");
            // The start of Plugin.Discoveries.cs when the database exists but cannot be opened: the store holds this session's sightings only.
            var store=new DiscoveryStore{MineIncomplete=true};
            store.SeeSource(DiscoveryKind.Venture,5,7,2,At);
            var message=Run(exchange,store);
            Check(message.Args[3]=="ShareBlocked" && server.Calls.Count==0,"nothing sent while the base is not read whole: "+string.Join(", ",message.Args)+" / "+server.Calls.Count);
            Check(server.Asked.Contains(CommunityDiscoveries.AddressV3+ExchangeV3.IndexName),"the common index still asked");
            store.MineIncomplete=false;
            Check(Run(exchange,store).Args[3]=="Sent" && server.Calls.Count==1 && server.Calls[0].Method=="PATCH","read whole: sent to the gist");
        })),
        ("After error 1551: A16 wiring: the start marks the base incomplete before it is said loaded; the exchange checks it before sending",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs")).Replace("\r\n","\n");
            var blocked=plugin.IndexOf("discoveries.MineIncomplete=true;\n                    log.Error(error,\"The discoveries database could not be opened",StringComparison.Ordinal);
            Check(blocked>0 && blocked<plugin.IndexOf("mainWindow.DiscoveriesLoaded=true;",blocked,StringComparison.Ordinal),"database not opened: incomplete, then loaded");
            Check(plugin.Contains("if(read.Unsafe || read.SetAside is not null)discoveries.MineIncomplete=true;",StringComparison.Ordinal),"JSON base set aside or unsafe: incomplete");
            var exchange=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs"));
            Check(exchange.IndexOf("store.MineIncomplete)sent=\"ShareBlocked\"",StringComparison.Ordinal) is var at && at>0
                && at<exchange.IndexOf("CommunityDiscoveries.Mine(store.Snapshot(",StringComparison.Ordinal),"checked before the snapshot is sent");
        }),
        ("After error 1551: A03, a plans' file held by another program is neither read as empty nor written over; read once let go",() => InTemp(root =>
        {
            var file=Path.Combine(root,"BeastTeams.json");
            var plans=new BeastTeams.SavedPlans([],new(){[1]="key"},new(){[3]=12});
            File.WriteAllText(file,BeastTeams.Write(plans));
            var before=File.ReadAllText(file);
            var reader=new BeastPlansFile();
            BeastTeams.SavedPlans? read;
            using(new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.None))read=reader.ReadIfDue(file,At);
            Check(read is null && reader.Blocked && !reader.MayWrite,"held: nothing read, nothing may be written");
            // The window's save after a ranks read (MainWindow.SaveBeastPlans) asks MayWrite first: nothing goes to the file.
            Check(reader.ReadIfDue(file,At.AddSeconds(1)) is null && reader.Blocked,"not read again before RetryEvery");
            Check(File.ReadAllText(file)==before,"the player's file untouched");
            read=reader.ReadIfDue(file,At+BeastPlansFile.RetryEvery);
            Check(read is { } again && again.Keys[1]=="key" && again.Ranks![3]==12 && !reader.Blocked && reader.MayWrite,"let go: read whole, then writes allowed");
        })),
        ("After error 1551: A03, a damaged plans' file is set aside under a dated name before a new file is written; absent, new plans",() => InTemp(root =>
        {
            var file=Path.Combine(root,"BeastTeams.json");
            var reader=new BeastPlansFile();
            Check(reader.ReadIfDue(file,At) is null && reader.MayWrite && reader.SetAside is null,"absent: new plans, writes allowed");
            File.WriteAllText(file,"{ damaged");
            reader.Reset();
            string? said=null;
            Check(reader.ReadIfDue(file,At,(moved,_)=>said=moved) is null && reader.MayWrite,"damaged: set aside, writes allowed");
            Check(reader.SetAside is { } aside && aside==said && File.ReadAllText(aside)=="{ damaged" && !File.Exists(file),"the damaged file kept aside, whole");
            var writer=new BackgroundWriter((error,message)=>throw new Exception(message,error));
            BeastTeams.SaveLater(writer,file,new BeastTeams.SavedPlans([],[]));
            Check(writer.Flush(Guard) && BeastTeams.Read(File.ReadAllText(file)) is not null && File.ReadAllText(reader.SetAside!)=="{ damaged","the new file beside the one set aside");
            var wiring=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(wiring.Contains("if (!beastFile.MayWrite) return;",StringComparison.Ordinal) && wiring.Contains("if (!beastPlansLoaded || beastFile.Blocked)",StringComparison.Ordinal),"the window saves only once the file may be written");
        })),
        ("After error 1551: A14, a gil journal whose write failed stays to save, and is written after a character change without a new gil change",() => InTemp(root =>
        {
            var store=new GilJournalStore(Path.Combine(root,"GilJournal"),new TenshoDataFile(Path.Combine(root,TenshoDataDatabase.FileName),Warn));
            store.Save(1,[Gil(60,100,1100)]);
            var writer=new BackgroundWriter((_,_)=>{});
            var saves=new GilSaves();
            saves.Switch();
            // Character 1: one more entry; the data file refuses the write (an error inside its transaction).
            var copy=new[]{Gil(60,100,1100),Gil(30,-50,1050)};
            saves.Changed();
            store.DuringSave=()=>throw new IOException("disk full");
            writer.Enqueue("save",saves.Done(1,copy,saves.Stamp,()=>store.Save(1,copy)));
            Check(writer.Flush(Guard) && saves.Due && saves.Failed(1)==copy,"failed: still to save, its copy kept");
            Check(store.Load(1).Count==1,"the data file keeps its last whole save");
            // The character changes before any new gil change: the old code had marked the journal clean, the entry was lost.
            store.DuringSave=null;
            saves.Switch();
            foreach(var (id,kept) in saves.Again(2,false))writer.Enqueue("again",saves.Done(id,kept,null,()=>store.Save(id,kept)));
            Check(writer.Flush(Guard) && saves.FailedCount==0 && store.Load(1).Count==2,"written again after the change of character");
        })),
        ("After error 1551: A14, acknowledged only once written; a change during the write stays to save; the failed copy is the journal given back",() =>
        {
            var writer=new BackgroundWriter((_,_)=>{});
            var saves=new GilSaves();
            saves.Switch();saves.Changed();
            var stamp=saves.Stamp;
            var gate=new ManualResetEventSlim();
            writer.Enqueue("save",saves.Done(1,[Gil(5,1,1)],stamp,()=>gate.Wait(Guard)));
            Check(saves.Due,"queued, not yet written: still to save");
            saves.Changed();
            gate.Set();
            Check(writer.Flush(Guard) && saves.Due,"a change during the write stays to save");
            writer.Enqueue("save",saves.Done(1,[Gil(5,1,1),Gil(4,1,2)],saves.Stamp,()=>{}));
            Check(writer.Flush(Guard) && !saves.Due,"written whole: clean");
            GilEntry[] lost=[Gil(3,7,9)];
            saves.Changed();
            writer.Enqueue("save",saves.Done(1,lost,saves.Stamp,()=>throw new IOException("held")));
            Check(writer.Flush(Guard),"the failed write done");
            // A write of this character made before the switch and ending after it acknowledges nothing for the next character.
            var late=saves.Done(1,lost,saves.Stamp,()=>{});
            saves.Switch();
            Check(saves.Failed(1)==lost && saves.Again(1,true).Length==0 && saves.Again(1,false).Length==1,"the failed copy given back to its character; not written again while a newer copy goes");
            saves.Changed();
            writer.Enqueue("late",late);
            Check(writer.Flush(Guard) && saves.Due && saves.FailedCount==0,"a late write of the character before: written, nothing acknowledged for the next one");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/GilWatcher.Dalamud.cs"));
            Check(!watcher.Contains("dirty=false;",StringComparison.Ordinal) && watcher.Contains("pending.Done(id,copy,pending.Stamp,()=>store.Save(id,copy))",StringComparison.Ordinal)
                && watcher.Contains("Saves.Changed();",StringComparison.Ordinal),"the watcher marks its journal written only through GilSaves");
        }),
        ("After error 1551: A20, the plugin's base damaged: the desktop copy, readable, is kept; a damaged copy is made again beside it",() => InTemp(root =>
        {
            var source=Path.Combine(root,"plugin",TenshoDataDatabase.FileName);var target=Path.Combine(root,"mirror",TenshoDataDatabase.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(source)!);
            new GilJournalStore(Path.Combine(root,"plugin","GilJournal"),new TenshoDataFile(source,Warn)).Save(1,[Gil(10,5,5)]);
            new TenshoDataFile(source,Warn).Checkpoint();
            Check(TenshoDataDatabase.CopyForDesktop(source,target).Copied==1,"copied");
            SqliteConnection.ClearAllPools();
            foreach(var path in new[]{source+"-wal",source+"-shm"})if(File.Exists(path))File.Delete(path);
            File.WriteAllText(source,"this is not a database, this is not a database, this is not a database, this is not a database");
            var copy=TenshoDataDatabase.CopyForDesktop(source,target);
            Check(copy.Copied==0 && copy.Problems.Count==1 && copy.Problems[0].Contains("damaged",StringComparison.Ordinal),"refused, said: "+string.Join("; ",copy.Problems));
            SqliteConnection.ClearAllPools();
            Check(File.Exists(target) && new GilJournalStore(Path.Combine(root,"mirror","GilJournal"),new TenshoDataFile(target,Warn)).Load(1).Count==1,"the last readable copy kept whole");
        })),
        ("After error 1551: A15, a file at the size bound is written within it and read back; one over it is set aside, never written over",() => InTemp(root =>
        {
            var path=Path.Combine(root,"MarketSeen.json");
            var entries=Enumerable.Range(1,400).Select(n=>Seen((uint)n,At.AddSeconds(n),n)).ToList();
            var whole=Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(entries));
            var max=(long)(whole*0.6);
            SeenPriceStore.Write(path,entries,max);
            Check(new FileInfo(path).Length<=max,"written within the bound: "+new FileInfo(path).Length+" > "+max);
            var back=SeenPriceStore.Read(path,max);
            Check(back is { Count:>100 and <400 } && back.Any(entry=>entry.Item==400) && back.All(entry=>entry.Item>400-back.Count),"read back by the reader's bound, the most recent kept: "+back?.Count);
            // A file over the bound (written by an older version): set aside whole, a new file in its place.
            var big=JsonSerializer.Serialize(entries);File.WriteAllText(path,big);
            SeenPriceStore.Write(path,[Seen(9001,At,5)],max);
            var aside=Directory.GetFiles(root,"MarketSeen.json.broken-*");
            Check(aside.Length==1 && File.ReadAllText(aside[0])==big && SeenPriceStore.Read(path,max)!.Single().Item==9001,"the file over the bound kept aside, whole");
            Check(SeenPriceStore.MaxFileBytes==64L*1024*1024,"the bound of before, for reading and writing");
        })),
        ("After error 1551: A27, a look at the board during the start's load keeps the newer offers, and the file's sales are joined",() =>
        {
            var store=new SeenPriceStore();
            var now=DateTimeOffset.UtcNow;
            store.SeeListings(40,5057,9,[new SeenListing(100,false,3)],now);
            store.Load([new SeenMarketEntry{World=40,Item=5057,Listings=[new SeenListing(999,false,1)],ListingsAt=now.AddHours(-2),ListingsRequest=1,
                Sales=[new SeenSale(800,false,1,now.AddHours(-3))],SalesAt=now.AddHours(-3)},Seen(5058,now.AddHours(-1),50)]);
            var kept=store.Get(40,5057)!;
            Check(kept.Listings.Single().Price==100 && kept.ListingsAt==now,"the board's newer offers kept, not the file's older ones");
            Check(kept.Sales.Single().Price==800,"the file's sales joined");
            Check(store.Get(40,5058) is { } other && other.Listings.Single().Price==50,"an item only in the file loaded as before");
        }),
    ];
}
