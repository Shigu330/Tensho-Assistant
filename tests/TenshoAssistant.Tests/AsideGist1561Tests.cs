using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1561 (X-621, review of 0.1560, I1): behaviour tests (R28.14). A row of yours damaged and set aside at a start, then a send to your gist:
// the copy your gist held is read back and sent again, never dropped; the gist not reached sends nothing; sightings made since the set-aside
// are added, never counted twice (two clients, a save before or after).
internal static class AsideGist1561Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=DateTimeOffset.UtcNow.AddDays(-2);

    internal static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-1561-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{SqliteConnection.ClearAllPools();try{Directory.Delete(root,true);}catch(IOException){}}
    }

    /// <summary>A fake GitHub: no common set (404); the player's gist listed as <see cref="Gist"/> says (its kind files cut, read from their raw address); PATCH recorded.</summary>
    private sealed class Server : HttpMessageHandler
    {
        public IReadOnlyDictionary<string,string> Files=new Dictionary<string,string>();
        public HttpStatusCode Gist=HttpStatusCode.OK;
        public readonly List<string> Asked=[];
        public readonly List<string> Patches=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();
            lock(Asked)Asked.Add(address);
            if(request.Method==HttpMethod.Get && address=="https://api.github.com/gists/g1")
            {
                if(Gist!=HttpStatusCode.OK)return new HttpResponseMessage(Gist);
                var files=new JsonObject();
                foreach(var (name,content) in Files)
                    files[name]=name==ExchangeV3.IndexName ? new JsonObject{["content"]=content,["truncated"]=false}
                        :new JsonObject{["content"]=content[..Math.Min(8,content.Length)],["truncated"]=true,["raw_url"]=GistAside.RawHost+"tester/g1/raw/abc/"+name};
                return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(new JsonObject{["id"]="g1",["files"]=files}.ToJsonString())};
            }
            if(request.Method==HttpMethod.Get && address.StartsWith(GistAside.RawHost+"tester/g1/raw/abc/",StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(Files[address[(GistAside.RawHost+"tester/g1/raw/abc/").Length..]])};
            if(request.Method==HttpMethod.Get)return new HttpResponseMessage(HttpStatusCode.NotFound);
            var body=request.Content is null ? "":await request.Content.ReadAsStringAsync(token);
            lock(Patches)Patches.Add(body);
            return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{}")};
        }
    }

    private static ExchangeMessage Run(GitHubExchange exchange,DiscoveryStore store)
    {
        Check(exchange.Exchange(store,true),"the exchange starts");
        for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is { Key:"ExchangeDone" },$"the exchange ends ({exchange.Message?.Key} {string.Join(", ",exchange.Message?.Args ?? [])})");
        return exchange.Message!;
    }

    private static GitHubExchange Connected(string root,Server server)
    {
        File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="tester",GistId="g1",Issue=7,ClientId=GitHubSignIn.Current.ClientId}));
        File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
        var exchange=new GitHubExchange(root,server);
        Check(exchange.Connected,"connected");
        return exchange;
    }

    /// <summary>
    /// Your base with two facts (A, B), both sent to your gist before; B's row then damaged and set aside at a start (the start of
    /// Plugin.Discoveries.cs). Gives the database, the gist's files and B's key and sightings on the gist.
    /// </summary>
    internal static (DiscoveryDatabase Database,IReadOnlyDictionary<string,string> Gist,string A,string B,int BSeen) Damaged(string root)
    {
        var seen=new DiscoveryStore();
        seen.SeeSource(DiscoveryKind.Venture,5,7,2,At);
        for(var n=0;n<3;n++)seen.SeeSource(DiscoveryKind.Venture,6,7,2,At.AddHours(n));
        var facts=seen.Snapshot(At,false).Facts;
        var a=facts.Single(fact=>fact.Subject==5).Key;var b=facts.Single(fact=>fact.Subject==6);
        Check(b.Seen>=1,"B seen");
        var database=new DiscoveryDatabase(Path.Combine(root,DiscoveryDatabase.FileName));
        database.Write(facts,[],false);
        var gist=ExchangeV3.GistFiles(ExchangeV3.Build(CommunityDiscoveries.Mine(seen.Snapshot(At,false))));
        using(var open=new SqliteConnection($"Data Source={database.Path};Pooling=false"))
        {
            open.Open();
            using var damage=open.CreateCommand();damage.CommandText="UPDATE facts SET fact='{ damaged' WHERE key=$key;";damage.Parameters.AddWithValue("$key",b.Key);
            Check(damage.ExecuteNonQuery()==1,"B's row damaged");
        }
        var unreadable=new List<string>();
        database.ReadMine((key,_)=>unreadable.Add(key));
        Check(unreadable.SequenceEqual([b.Key]) && database.SetAside(unreadable,DateTimeOffset.UtcNow)==1,"B set aside at the start");
        return (database,gist,a,b.Key,b.Seen);
    }

    /// <summary>The next start, read whole: your facts loaded, the rows set aside and not settled given to the store (Plugin.Discoveries.cs).</summary>
    internal static DiscoveryStore Start(DiscoveryDatabase database)
    {
        var store=new DiscoveryStore();
        store.Load(database.ReadMine((key,error)=>throw new Exception("unreadable "+key,error)));
        if(database.AsidePending() is { Count:>0 } pending)store.Aside=new(pending,kept=>database.RestoreAside(pending,kept,DateTimeOffset.UtcNow));
        return store;
    }

    /// <summary>The facts of the last PATCH sent to the gist, by key.</summary>
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

    internal static DiscoveryFact? Row(DiscoveryDatabase database,string key)=>database.ReadMine().Facts.SingleOrDefault(fact=>fact.Key==key);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Aside gist 1561: I1, a row of yours set aside, then a send: its copy on your gist is read back and sent again, never dropped",() => InTemp(root =>
        {
            var (database,gist,a,b,bSeen)=Damaged(root);
            var server=new Server{Files=gist};
            using var exchange=Connected(root,server);
            var store=Start(database);
            Check(store.Aside is { Keys.Count:1 } && Row(database,b) is null,"B pending, missing from your facts");
            Check(Run(exchange,store).Args[3]=="Sent","sent");
            var sent=Sent(server);
            Check(sent.ContainsKey(a) && sent.TryGetValue(b,out var kept) && kept.Seen==bSeen,"the gist keeps A and B, B with its sightings: "+string.Join(", ",sent.Keys));
            Check(server.Asked.Any(address=>address.StartsWith(GistAside.RawHost,StringComparison.Ordinal)),"a cut kind file read from its raw address");
            Check(Row(database,b) is { } row && row.Seen==bSeen && store.Find(DiscoveryKind.Venture,6,10).Single().Seen==bSeen,"B back in your base, database and memory");
            Check(database.AsidePending().Count==0 && database.CountAside()==1 && store.Aside is null,"settled; the damaged text still kept aside");
            var asked=server.Asked.Count(address=>address=="https://api.github.com/gists/g1");
            Check(Run(exchange,store).Args[3]=="SentSame" && server.Asked.Count(address=>address=="https://api.github.com/gists/g1")==asked,"the next exchange: nothing read back again, nothing new to send");
            Check(Start(database).Aside is null,"the start after: nothing pending");
        })),
        ("Aside gist 1561: I1, your gist not reached: nothing sent (the gist keeps B), tried again at the next exchange",() => InTemp(root =>
        {
            var (database,gist,a,b,bSeen)=Damaged(root);
            var server=new Server{Files=gist,Gist=HttpStatusCode.ServiceUnavailable};
            using var exchange=Connected(root,server);
            var store=Start(database);
            Check(Run(exchange,store).Args[3]=="ShareAsideWaiting" && server.Patches.Count==0,"nothing sent while the gist cannot be read back");
            Check(database.AsidePending().SequenceEqual([b]) && store.Aside is not null,"B still pending");
            server.Gist=HttpStatusCode.OK;
            Check(Run(exchange,store).Args[3]=="Sent" && Sent(server).TryGetValue(b,out var kept) && kept.Seen==bSeen,"reached: B read back and sent");
        })),
        ("Aside gist 1561: I1, B seen again after the set-aside: the sightings added once, the save before or after the read-back",() => InTemp(root =>
        {
            var (database,gist,a,b,bSeen)=Damaged(root);
            var server=new Server{Files=gist};
            using var exchange=Connected(root,server);
            // Seen once more and saved before the exchange, then once more unsaved.
            var store=Start(database);
            store.SeeSource(DiscoveryKind.Venture,6,7,2,DateTimeOffset.UtcNow.AddHours(-1));
            var changes=store.ChangesToSave(out var stamp);database.Write(changes.Write,changes.Remove,changes.All,changes.Bases);store.MarkSaved(stamp);
            store.SeeSource(DiscoveryKind.Venture,6,7,2,DateTimeOffset.UtcNow);
            var memory=store.Find(DiscoveryKind.Venture,6,10).Single().Seen;
            Check(Row(database,b)?.Seen==memory-1,"one sighting saved, one not");
            Check(Run(exchange,store).Args[3]=="Sent" && Sent(server)[b].Seen==bSeen+memory,"sent: the gist's sightings and the new ones");
            changes=store.ChangesToSave(out stamp);database.Write(changes.Write,changes.Remove,changes.All,changes.Bases);store.MarkSaved(stamp);
            Check(Row(database,b)?.Seen==bSeen+memory && store.Find(DiscoveryKind.Venture,6,10).Single().Seen==bSeen+memory,"saved after: counted once ("+Row(database,b)?.Seen+")");
        })),
        ("Aside gist 1561: I1, two clients settle the same rows: the second joins nothing twice; a gist gone or without B settles with nothing kept",() => InTemp(root =>
        {
            var (database,gist,a,b,bSeen)=Damaged(root);
            var pending=database.AsidePending();
            var kept=GistAside.Read(()=>Task.FromResult<JsonNode?>(JsonNode.Parse(new JsonObject{["files"]=new JsonObject{[ExchangeV3.IndexName]=new JsonObject{["content"]=gist[ExchangeV3.IndexName]},
                ["kind-"+((int)DiscoveryKind.Venture).ToString("00")+".jsonl.gz.b64"]=new JsonObject{["content"]=gist["kind-"+((int)DiscoveryKind.Venture).ToString("00")+".jsonl.gz.b64"]}}}.ToJsonString())),
                _=>throw new Exception("no raw read"),pending).GetAwaiter().GetResult();
            Check(kept.Count==1 && kept[0].Key==b,"only B kept from the gist");
            Check(database.RestoreAside(pending,kept,DateTimeOffset.UtcNow).Count==1 && database.RestoreAside(pending,kept,DateTimeOffset.UtcNow).Count==0,"the second client finds B settled");
            Check(Row(database,b)?.Seen==bSeen,"B counted once");
            var (other,otherGist,_,ob,_)=Damaged(Directory.CreateDirectory(Path.Combine(root,"two")).FullName);
            var gone=new Server{Files=otherGist,Gist=HttpStatusCode.NotFound};
            using var exchange=Connected(Path.Combine(root,"two"),gone);
            var store=Start(other);
            Check(Run(exchange,store).Args[3]=="Sent" && !Sent(gone).ContainsKey(ob) && other.AsidePending().Count==0,"a gist gone: nothing to keep, settled, sent");
        })),
        ("Aside gist 1561: I1 wiring: the start lists the rows set aside after the read; the exchange reads them back before the snapshot is sent",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs")).Replace("\r\n","\n");
            var setAside=plugin.IndexOf("opened.SetAside(aside,DateTimeOffset.UtcNow)",StringComparison.Ordinal);
            var listed=plugin.IndexOf("discoveries.Aside=new(pending,kept=>opened.RestoreAside(pending,kept,DateTimeOffset.UtcNow));",StringComparison.Ordinal);
            Check(setAside>0 && listed>setAside && listed<plugin.IndexOf("mainWindow.DiscoveriesLoaded=true;",listed,StringComparison.Ordinal),"listed after the set-aside, before the page is said loaded");
            Check(plugin.Contains("catch(Exception error){discoveries.MineIncomplete=true;log.Warning(error,\"Discoveries: the rows set aside could not be listed",StringComparison.Ordinal),"not listed: nothing sent");
            var exchange=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs"));
            Check(exchange.IndexOf("!await KeepAside(store).ConfigureAwait(false))sent=\"ShareAsideWaiting\"",StringComparison.Ordinal) is var at && at>0
                && at<exchange.IndexOf("CommunityDiscoveries.Mine(store.Snapshot(",StringComparison.Ordinal),"read back before the snapshot is sent");
        }),
    ];
}
