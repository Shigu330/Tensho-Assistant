using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1825 (X-822, audit of 22/11 checked by the master): GitHubExchange.SaveMeta swallowed any failed write of github.json, which since X-804
// holds the gist in use and the issue: the next exchange could make a new gist and issue and leave the old gist undeleted. Now the failure is
// said once in the log and stops the exchange (« ExchangeMetaUnsaved » on the Discoveries page) before any gist or issue is made. And a row
// of the discoveries database that cannot be read, met by a save or by the read-back of a copy, was written over without a word: now its
// text is set aside (facts_aside, kept) and said in the log. Behaviour tests (R28.14): github.json that cannot be written (a folder in its
// place) against a fake GitHub; damaged rows in a real database.
internal static class MetaUnsaved1825Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string Gists="https://api.github.com/gists";
    private const string Issues="https://api.github.com/repos/"+GitHubExchange.Repository+"/issues";

    /// <summary>A fake GitHub: no common set, gists made (g2, g3…) and patched, issues opened (8). <see cref="OnGist"/> runs when a gist is made.</summary>
    private sealed class Server : HttpMessageHandler
    {
        public Action? OnGist;
        public readonly List<(string Method,string Address)> Asked=[];
        private int made=1;
        public int Count(string method,string address){lock(Asked)return Asked.Count(a=>a.Method==method && a.Address==address);}
        public int Count(string method){lock(Asked)return Asked.Count(a=>a.Method==method);}
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();var method=request.Method.Method;
            lock(Asked)Asked.Add((method,address));
            if(method=="POST" && address==Gists)
            {
                var id="g"+Interlocked.Increment(ref made);
                OnGist?.Invoke();
                return Task.FromResult(Json(new JsonObject{["id"]=id,["history"]=new JsonArray(new JsonObject{["version"]="r"+id[1..]})}));
            }
            if(method=="PATCH" && address.StartsWith(Gists+"/",StringComparison.Ordinal))
                return Task.FromResult(Json(new JsonObject{["id"]=address[(Gists.Length+1)..],["history"]=new JsonArray(new JsonObject{["version"]="rp"})}));
            if(method=="POST" && address==Issues)return Task.FromResult(Json(new JsonObject{["number"]=8}));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
        private static HttpResponseMessage Json(JsonObject value)=>new(HttpStatusCode.OK){Content=new StringContent(value.ToJsonString())};
    }

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-meta-1825-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try{run(root);}
        finally{SqliteConnection.ClearAllPools();try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    private static DiscoveryFact Fact(uint subject,int seen=1)
    {
        var x=10f+60f*subject;var z=-20f;var (cellX,cellZ)=DiscoveryStore.Cell(x,z);
        var day=new DateTimeOffset(2026,11,1,0,0,0,TimeSpan.Zero);
        return new DiscoveryFact{Kind=DiscoveryKind.MonsterPlace,Subject=subject,Territory=148,Map=4,CellX=cellX,CellZ=cellZ,X=x,Z=z,LevelMin=5,LevelMax=6,Seen=seen,First=day,Last=day};
    }
    private static DiscoveryStore Store(params uint[] subjects)
    {
        var store=new DiscoveryStore();
        store.Load(new DiscoveryFile{Facts=[..subjects.Select(subject=>Fact(subject))]});
        return store;
    }
    /// <summary>Connected as « tester », no gist nor issue yet (the first send makes both).</summary>
    private static GitHubExchange Connected(string root,Server server,List<string> log)
    {
        File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="tester",ClientId=GitHubSignIn.Current.ClientId}));
        File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
        var exchange=new GitHubExchange(root,server){Log=line=>{lock(log)log.Add(line);}};
        Check(exchange.Connected,"connected");
        return exchange;
    }
    /// <summary>github.json made unwritable: a folder in its place (the write's move over it fails, as on a locked file or a full disk).</summary>
    private static void Block(string root){var meta=Path.Combine(root,"github.json");File.Delete(meta);Directory.CreateDirectory(meta);}
    private static void Free(string root){var meta=Path.Combine(root,"github.json");Directory.Delete(meta,true);}
    private static ExchangeMessage Run(GitHubExchange exchange,DiscoveryStore store)
    {
        Check(exchange.Exchange(store,true),"the exchange starts");
        for(var wait=0;exchange.Busy && wait<3000;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is not null,"the exchange ends");
        return exchange.Message!;
    }
    private static int Said(List<string> log,string text){lock(log)return log.Count(line=>line.Contains(text,StringComparison.Ordinal));}

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Meta unsaved 1825: github.json that cannot be written stops the exchange at the gist, before any issue; then nothing is made until it is written, said once",() => InTemp(root =>
        {
            var server=new Server();var log=new List<string>();
            using var exchange=Connected(root,server,log);
            var store=Store(1,2);
            Block(root);
            Check(Run(exchange,store) is { Key:"ExchangeMetaUnsaved" },"stopped, said on the page");
            Check(server.Count("POST",Gists)==1 && server.Count("POST",Issues)==0,"the gist made (noted in the rotation's state), no issue opened");
            Check(Run(exchange,store) is { Key:"ExchangeMetaUnsaved" } && Run(exchange,store) is { Key:"ExchangeMetaUnsaved" },"stopped again at the next exchanges");
            Check(server.Count("POST",Gists)==1 && server.Count("PATCH")==0 && server.Count("POST",Issues)==0,"nothing more made nor written on GitHub");
            Check(Said(log,"github.json could not be written")==1,$"said once in the log ({string.Join(" | ",log)})");
            Free(root);
            var said=Run(exchange,store);
            Check(said is { Key:"ExchangeDone" } && said.Args[3]=="Sent",$"written again: sent ({said.Key} {string.Join(", ",said.Args)})");
            Check(server.Count("POST",Gists)==1 && server.Count("PATCH",Gists+"/g2")==1 && server.Count("POST",Issues)==1 && Said(log,"github.json written again")==1,
                "the same gist written again, never a second one; one issue");
            var meta=JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(root,"github.json")))!;
            Check(meta is { GistId:"g2", Issue:8 },"the gist and issue noted");
        })),
        ("Meta unsaved 1825: github.json lost right after the gist is made: no issue opened; the next exchange writes that gist again, never a second one",() => InTemp(root =>
        {
            var server=new Server();var log=new List<string>();
            using var exchange=Connected(root,server,log);
            var store=Store(1,2);
            server.OnGist=()=>{Block(root);server.OnGist=null;};
            Check(Run(exchange,store) is { Key:"ExchangeMetaUnsaved" },"stopped once the gist could not be noted");
            Check(server.Count("POST",Gists)==1 && server.Count("POST",Issues)==0,"the gist made, no issue opened");
            Check(GistRotation.Load(Path.Combine(root,GistRotation.FileName)) is { Known:true, Gist:"g2" },"the gist noted in the rotation's state");
            Free(root);
            var said=Run(exchange,store);
            Check(said is { Key:"ExchangeDone" } && said.Args[3]=="Sent",$"written again: sent ({said.Key} {string.Join(", ",said.Args)})");
            Check(server.Count("POST",Gists)==1 && server.Count("PATCH",Gists+"/g2")==1 && server.Count("POST",Issues)==1,"g2 written again, no second gist, its issue opened");
        })),
        ("Meta unsaved 1825: a damaged row met by a save is set aside (text kept, settled, said), then written with this client's fact",() => InTemp(root =>
        {
            var log=new List<string>();
            var database=new DiscoveryDatabase(Path.Combine(root,DiscoveryDatabase.FileName)){Log=line=>{lock(log)log.Add(line);}};
            var a=Fact(1,2);var b=Fact(2,1);
            database.Write([a,b],[],false);
            Damage(database,b.Key);
            var newer=Fact(2,3);
            database.Write([newer],[],false,new Dictionary<string,DiscoveryFact?>{[b.Key]=Fact(2,1)});
            Check(database.CountAside()==1 && AsideText(database,b.Key)=="{ damaged","the damaged text kept in facts_aside");
            Check(database.AsidePending().Count==0,"settled: no copy read back on top of the fact written");
            Check(database.ReadMine().Facts.Single(fact=>fact.Key==b.Key).Seen==3 && database.Count().Mine==2,"the row holds this client's fact");
            Check(Said(log,$"a row that could not be read ({b.Key}) was set aside")==1,"said in the log");
        })),
        ("Meta unsaved 1825: a damaged row met by the read-back of a copy is set aside too, and the read-back still settles its key",() => InTemp(root =>
        {
            var log=new List<string>();
            var database=new DiscoveryDatabase(Path.Combine(root,DiscoveryDatabase.FileName)){Log=line=>{lock(log)log.Add(line);}};
            var b=Fact(2,1);
            database.Write([b],[],false);
            Damage(database,b.Key);
            Check(database.SetAside([b.Key],DateTimeOffset.UtcNow)==1 && database.AsidePending().SequenceEqual([b.Key]),"set aside at a start, to read back");
            database.Write([Fact(2,1)],[],false);   // another client wrote the key again
            Damage(database,b.Key);                 // and that row is damaged too
            var copy=Fact(2,4);
            var joined=database.RestoreAside([b.Key],[copy],DateTimeOffset.UtcNow);
            Check(joined.Count==1 && database.CountAside()==2,"both damaged texts kept");
            Check(database.AsidePending().Count==0 && database.ReadMine().Facts.Single().Seen==4,"settled, the copy written");
            Check(Said(log,"could not be read")==1,"said in the log");
        })),
        ("Meta unsaved 1825: wired (the plugin gives the database its log; every gist or issue step keeps github.json first)",() =>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs")).ReplaceLineEndings("\n");
            Check(code.Contains("else{if(MetaUnsaved)KeepMeta();sent=await Rotate(mine).ConfigureAwait(false);}",StringComparison.Ordinal)
                && code.Contains("if(!SaveMeta())throw new ExchangeRefused(\"ExchangeMetaUnsaved\",false);",StringComparison.Ordinal)
                && !code.Contains("catch(Exception){}\n    }\n\n    /// <summary>X-181 (0.1171, night review N8)",StringComparison.Ordinal),"the exchange stops on a failed write");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs")).Contains("new Discoveries.DiscoveryDatabase(databasePath){Log=message=>log.Warning(message)};",StringComparison.Ordinal),
                "the database's log");
        }),
    ];

    private static void Damage(DiscoveryDatabase database,string key)
    {
        using var open=new SqliteConnection($"Data Source={database.Path};Pooling=false");open.Open();
        using var damage=open.CreateCommand();damage.CommandText="UPDATE facts SET fact='{ damaged' WHERE key=$key;";damage.Parameters.AddWithValue("$key",key);
        Check(damage.ExecuteNonQuery()==1,"the row damaged");
    }
    private static string? AsideText(DiscoveryDatabase database,string key)
    {
        using var open=new SqliteConnection($"Data Source={database.Path};Pooling=false");open.Open();
        using var select=open.CreateCommand();select.CommandText="SELECT fact FROM facts_aside WHERE key=$key;";select.Parameters.AddWithValue("$key",key);
        return select.ExecuteScalar() as string;
    }
}
