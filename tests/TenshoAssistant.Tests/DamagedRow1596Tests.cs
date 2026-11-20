using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1596 (X-652, user decision D-188 b of 20/11, with D-190 a): behaviour tests (R28.14). A row of your discoveries damaged in the base: at
// the start it is set aside (kept apart, never erased) and the sending to your gist resumes in the same session, the gist's copy of that fact
// read back first; no other fact lost. A set-aside that fails, or a row not listed, keeps the block.
internal static class DamagedRow1596Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=DateTimeOffset.UtcNow.AddDays(-2);

    /// <summary>A fake GitHub: no common set (404); the player's gist g1 listed whole (no cut file); PATCH recorded.</summary>
    private sealed class Server : HttpMessageHandler
    {
        public IReadOnlyDictionary<string,string> Files=new Dictionary<string,string>();
        public readonly List<string> Patches=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();
            if(request.Method==HttpMethod.Get && address=="https://api.github.com/gists/g1")
            {
                var files=new JsonObject();
                foreach(var (name,content) in Files)files[name]=new JsonObject{["content"]=content,["truncated"]=false};
                return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(new JsonObject{["id"]="g1",["files"]=files}.ToJsonString())};
            }
            if(request.Method==HttpMethod.Get)return new HttpResponseMessage(HttpStatusCode.NotFound);
            var body=request.Content is null ? "":await request.Content.ReadAsStringAsync(token);
            lock(Patches)Patches.Add(body);
            return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{}")};
        }
    }

    private static GitHubExchange Connected(string root,Server server)
    {
        File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="tester",GistId="g1",Issue=7,ClientId=GitHubSignIn.Current.ClientId}));
        File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
        var exchange=new GitHubExchange(root,server);
        Check(exchange.Connected,"connected");
        return exchange;
    }

    private static ExchangeMessage Run(GitHubExchange exchange,DiscoveryStore store)
    {
        Check(exchange.Exchange(store,true),"the exchange starts");
        for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is { Key:"ExchangeDone" },$"the exchange ends ({exchange.Message?.Key} {string.Join(", ",exchange.Message?.Args ?? [])})");
        return exchange.Message!;
    }

    private static Dictionary<string,DiscoveryFact> Sent(Server server)
    {
        var files=JsonNode.Parse(server.Patches[^1])!["files"]!.AsObject();
        var index=ExchangeV3.ParseIndex(files[ExchangeV3.IndexName]!["content"]!.GetValue<string>())!;
        var facts=new Dictionary<string,DiscoveryFact>();
        foreach(var entry in index.Files)
            foreach(var fact in ExchangeV3.ReadKind(new MemoryStream(Convert.FromBase64String(files[entry.Name+ExchangeV3.GistSuffix]!["content"]!.GetValue<string>())),entry).Facts)
                facts[fact.Key]=fact;
        return facts;
    }

    /// <summary>Your base with three facts (A, B, C) sent to your gist before; then B's row damaged. Gives the database, the gist and the keys.</summary>
    private static (DiscoveryDatabase Database,IReadOnlyDictionary<string,string> Gist,string A,string B,string C,int BSeen) Base(string root,bool damage=true)
    {
        var seen=new DiscoveryStore();
        seen.SeeSource(DiscoveryKind.Venture,5,7,2,At);
        for(var n=0;n<3;n++)seen.SeeSource(DiscoveryKind.Venture,6,7,2,At.AddHours(n));
        seen.SeeSource(DiscoveryKind.Venture,8,7,2,At);
        var facts=seen.Snapshot(At,false).Facts;
        var a=facts.Single(fact=>fact.Subject==5).Key;var b=facts.Single(fact=>fact.Subject==6);var c=facts.Single(fact=>fact.Subject==8).Key;
        var database=new DiscoveryDatabase(Path.Combine(root,DiscoveryDatabase.FileName));
        database.Write(facts,[],false);
        var gist=ExchangeV3.GistFiles(ExchangeV3.Build(CommunityDiscoveries.Mine(seen.Snapshot(At,false))));
        if(damage)
        {
            using var open=new SqliteConnection($"Data Source={database.Path};Pooling=false");
            open.Open();
            using var command=open.CreateCommand();command.CommandText="UPDATE facts SET fact='{ damaged' WHERE key=$key;";command.Parameters.AddWithValue("$key",b.Key);
            Check(command.ExecuteNonQuery()==1,"B's row damaged");
        }
        return (database,gist,a,b.Key,c,b.Seen);
    }

    /// <summary>The start of Plugin.Discoveries.cs: read (incomplete on a damaged row), set aside, rows listed for the read-back, then Resume.</summary>
    private static (DiscoveryStore Store,int Moved,bool Resumed) Start(DiscoveryDatabase database,Func<IReadOnlyCollection<string>,int>? setAside=null)
    {
        var store=new DiscoveryStore();
        var unreadable=new HashSet<string>();
        store.Load(database.ReadMine(DiscoveryStart.Incomplete(store,(key,_)=>unreadable.Add(key))));
        var moved=0;
        if(unreadable.Count>0)
            try{moved=(setAside ?? (keys=>database.SetAside(keys,DateTimeOffset.UtcNow)))(unreadable);}
            catch(Exception){}
        if(database.AsidePending() is { Count:>0 } pending)store.Aside=new(pending,kept=>database.RestoreAside(pending,kept,DateTimeOffset.UtcNow));
        return (store,moved,DiscoveryStart.Resume(store,unreadable,moved));
    }

    private static string AsideText(DiscoveryDatabase database,string key)
    {
        using var open=new SqliteConnection($"Data Source={database.Path};Pooling=false");
        open.Open();
        using var command=open.CreateCommand();command.CommandText="SELECT fact FROM facts_aside WHERE key=$key;";command.Parameters.AddWithValue("$key",key);
        return command.ExecuteScalar() as string ?? "";
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Damaged row 1596: X-652, a damaged row at start: set aside and kept, the send resumes the same session, its copy read back, no other fact lost",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,a,b,c,bSeen)=Base(root);
            var (store,moved,resumed)=Start(database);
            Check(moved==1 && resumed && !store.MineIncomplete && store.Aside is { } rows && rows.Keys.Contains(b),"set aside, listed, the sending resumed");
            Check(AsideText(database,b)=="{ damaged" && database.CountAside()==1,"the damaged text kept apart, never erased");
            var server=new Server{Files=gist};
            using var exchange=Connected(root,server);
            var said=Run(exchange,store);
            Check(said.Args[3]=="Sent" && server.Patches.Count==1,"sent this session, not « ShareBlocked »: "+string.Join(", ",said.Args));
            var sent=Sent(server);
            Check(sent.ContainsKey(a) && sent.ContainsKey(c) && sent.TryGetValue(b,out var copy) && copy.Seen==bSeen,"A and C sent, B's copy read back from the gist and sent with its sightings");
            Check(database.AsidePending().Count==0 && AsideText(database,b)=="{ damaged","B settled, its damaged text still kept");
            Check(AsideGist1561Tests.Row(database,b)?.Seen==bSeen && AsideGist1561Tests.Row(database,a) is not null && AsideGist1561Tests.Row(database,c) is not null,"the base holds A, B (from its copy) and C");
        })),
        ("Damaged row 1596: X-652, the set-aside fails: the block stays this session, nothing sent; the row is left as it was",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,_,b,_,_)=Base(root);
            var (store,moved,resumed)=Start(database,_=>throw new IOException("database held"));
            Check(moved==0 && !resumed && store.MineIncomplete,"not set aside: still incomplete");
            var server=new Server{Files=gist};
            using var exchange=Connected(root,server);
            Check(Run(exchange,store).Args[3]=="ShareBlocked" && server.Patches.Count==0,"nothing sent");
            Check(database.CountAside()==0 && database.Count().Mine==3,"the damaged row still in facts, tried again at the next start");
        })),
        ("Damaged row 1596: X-652, Resume: only when every unreadable row was moved and is listed for the read-back",() =>
        {
            DiscoveryStore Store(params string[] listed)=>new(){MineIncomplete=true,Aside=listed.Length==0 ? null:new(listed,_=>[])};
            var store=Store("b");
            Check(DiscoveryStart.Resume(store,["b"],1) && !store.MineIncomplete,"moved and listed: resumed");
            store=Store("b");
            Check(!DiscoveryStart.Resume(store,["b","d"],1) && store.MineIncomplete,"one row not moved (written again meanwhile): blocked");
            store=Store("d");
            Check(!DiscoveryStart.Resume(store,["b"],1) && store.MineIncomplete,"moved but not listed (settled before): blocked");
            store=Store();
            Check(!DiscoveryStart.Resume(store,["b"],1) && store.MineIncomplete,"not listed (the listing failed): blocked");
            store=Store("b");
            Check(!DiscoveryStart.Resume(store,[],0) && store.MineIncomplete,"no damaged row: Resume never clears a block of another cause");
        }),
        ("Damaged row 1596: X-652 wiring: Resume after the set-aside and the listing, its outcome logged",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs")).Replace("\r\n","\n");
            var moved=plugin.IndexOf("moved=opened.SetAside(aside,DateTimeOffset.UtcNow);",StringComparison.Ordinal);
            var listed=plugin.IndexOf("discoveries.Aside=new(pending,",StringComparison.Ordinal);
            var resume=plugin.IndexOf("if(Discoveries.DiscoveryStart.Resume(discoveries,aside,moved))",StringComparison.Ordinal);
            Check(moved>0 && listed>moved && resume>listed && resume<plugin.IndexOf("mainWindow.DiscoveriesLoaded=true;",resume,StringComparison.Ordinal),"set aside, listed, then resumed, before the page is said loaded");
            Check(plugin.Contains("the sending to your gist resumes this session",StringComparison.Ordinal) && plugin.Contains("nothing is sent to your gist this session.\",aside.Length,moved);",StringComparison.Ordinal),"the log says it");
        }),
    ];
}
