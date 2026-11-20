using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1597 (X-640, bench X-631 review of 0.1564 M1; user decision D-190 a confirmed on 20/11): behaviour tests (R28.14). Rows of yours set
// aside as damaged wait for their copy on your gist; GitHub refusing that copy's raw address durably (404, 401, 403…) is said to the player
// and counted across exchanges and restarts; at the third exchange in a row the send goes on without it (said), nothing else lost, the
// damaged text kept. « Not reached » (5xx, network) still waits, without limit, as before.
internal static class GistRefused1597Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string Raw="tester/g1/raw/abc/";

    /// <summary>A fake GitHub: no common set (404); gist g1 listed, its kind files cut (raw address answering <see cref="RawStatus"/>); PATCH recorded.</summary>
    private sealed class Server : HttpMessageHandler
    {
        public IReadOnlyDictionary<string,string> Files=new Dictionary<string,string>();
        public HttpStatusCode RawStatus=HttpStatusCode.OK;
        public int RawAsked;
        public readonly List<string> Patches=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();
            if(request.Method==HttpMethod.Get && address=="https://api.github.com/gists/g1")
            {
                var files=new JsonObject();
                foreach(var (name,content) in Files)
                    files[name]=name==ExchangeV3.IndexName ? new JsonObject{["content"]=content,["truncated"]=false}
                        :new JsonObject{["content"]=content[..Math.Min(8,content.Length)],["truncated"]=true,["raw_url"]=GistAside.RawHost+Raw+name};
                return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(new JsonObject{["id"]="g1",["files"]=files}.ToJsonString())};
            }
            if(request.Method==HttpMethod.Get && address.StartsWith(GistAside.RawHost+Raw,StringComparison.Ordinal))
            {
                Interlocked.Increment(ref RawAsked);
                return RawStatus==HttpStatusCode.OK ? new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(Files[address[(GistAside.RawHost+Raw).Length..]])}
                    :new HttpResponseMessage(RawStatus);
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

    private static int Refused(string root)=>JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(root,"github.json")))!.AsideRefused;

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

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gist refused 1597: X-640, the copy's raw address refused (404): said, counted across a restart, and at the third exchange sent without it, nothing else lost",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,a,b,_)=AsideGist1561Tests.Damaged(root);
            var server=new Server{Files=gist,RawStatus=HttpStatusCode.NotFound};
            var store=AsideGist1561Tests.Start(database);
            using(var exchange=Connected(root,server))
            {
                var said=Run(exchange,store);
                Check(said.Args[3]=="ShareAsideRefused" && server.Patches.Count==0 && Refused(root)==1,"1st refusal: said, nothing sent, counted: "+string.Join(", ",said.Args));
                Check(database.AsidePending().SequenceEqual([b]) && store.Aside is not null,"B still waits");
            }
            using(var restarted=new GitHubExchange(root,server))
            {
                var said=Run(restarted,store);
                Check(said.Args[3]=="ShareAsideRefused" && server.Patches.Count==0 && Refused(root)==2,"2nd refusal after a restart: the count kept: "+string.Join(", ",said.Args));
                said=Run(restarted,store);
                Check(said.Args[3]=="ShareAsideGivenUp" && server.Patches.Count==1,"3rd refusal: sent without B's copy, and said: "+string.Join(", ",said.Args));
                var sent=Sent(server);
                Check(sent.ContainsKey(a) && !sent.ContainsKey(b),"A sent, B left out (its copy refused)");
                Check(database.AsidePending().Count==0 && database.CountAside()==1 && store.Aside is null && Refused(root)==0,"B settled, its damaged text kept in facts_aside, the count back to 0");
                Check(AsideGist1561Tests.Row(database,a) is not null,"A still in the base");
            }
        })),
        ("Gist refused 1597: X-640, a refusal then the copy read: B kept and sent, the count back to 0",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,a,b,bSeen)=AsideGist1561Tests.Damaged(root);
            var server=new Server{Files=gist,RawStatus=HttpStatusCode.Forbidden};
            using var exchange=Connected(root,server);
            var store=AsideGist1561Tests.Start(database);
            Check(Run(exchange,store).Args[3]=="ShareAsideRefused" && Refused(root)==1,"a 403: refused, counted");
            server.RawStatus=HttpStatusCode.OK;
            var said=Run(exchange,store);
            Check(said.Args[3]=="Sent" && Sent(server).TryGetValue(b,out var copy) && copy.Seen==bSeen && Sent(server).ContainsKey(a) && Refused(root)==0,"read: B sent with its sightings, count 0: "+string.Join(", ",said.Args));
        })),
        ("Gist refused 1597: X-640, « not reached » (503, 429) still waits without limit and is never counted as a refusal",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,_,b,_)=AsideGist1561Tests.Damaged(root);
            var server=new Server{Files=gist,RawStatus=HttpStatusCode.ServiceUnavailable};
            using var exchange=Connected(root,server);
            var store=AsideGist1561Tests.Start(database);
            for(var n=0;n<GistAside.RefusalLimit+1;n++)
            {
                server.RawStatus=n%2==0 ? HttpStatusCode.ServiceUnavailable:HttpStatusCode.TooManyRequests;
                Check(Run(exchange,store).Args[3]=="ShareAsideWaiting" && server.Patches.Count==0 && Refused(root)==0,$"try {n+1}: waiting, nothing sent, nothing counted");
            }
            Check(database.AsidePending().SequenceEqual([b]) && server.RawAsked>GistAside.RefusalLimit,"B still waits");
        })),
        ("Gist refused 1597: X-640, GistAside: durable statuses, and SkipRefused keeps the other kinds, nothing from a refused index",() =>
        {
            Check(new[]{HttpStatusCode.BadRequest,HttpStatusCode.Unauthorized,HttpStatusCode.Forbidden,HttpStatusCode.NotFound,HttpStatusCode.Gone}.All(GistAside.Durable),"4xx: durable");
            Check(!new[]{HttpStatusCode.RequestTimeout,HttpStatusCode.TooManyRequests,HttpStatusCode.InternalServerError,HttpStatusCode.BadGateway,HttpStatusCode.ServiceUnavailable}.Any(GistAside.Durable),"408, 429, 5xx: not durable");
            var at=DateTimeOffset.UtcNow.AddDays(-1);
            var seen=new DiscoveryStore();
            seen.SeeSource(DiscoveryKind.Venture,5,7,2,at);
            seen.SeeSource(DiscoveryKind.Desynthesis,9,7,2,at);
            var mine=CommunityDiscoveries.Mine(seen.Snapshot(at,false));
            var files=ExchangeV3.GistFiles(ExchangeV3.Build(mine));
            var index=ExchangeV3.ParseIndex(files[ExchangeV3.IndexName])!;
            Check(index.Files.Count==2,"two kind files");
            var refusedName=index.Files.Single(entry=>entry.Kind==DiscoveryKind.Desynthesis).Name+ExchangeV3.GistSuffix;
            var keys=mine.Facts.Select(fact=>fact.Key).ToArray();
            JsonNode Listing(bool cutIndex)
            {
                var all=new JsonObject();
                foreach(var (name,content) in files)
                    all[name]=name==ExchangeV3.IndexName && !cutIndex ? new JsonObject{["content"]=content}
                        :new JsonObject{["content"]="cut",["truncated"]=true,["raw_url"]=GistAside.RawHost+Raw+name};
                return new JsonObject{["files"]=all};
            }
            Task<string> RawRead(Uri address,string refused)
            {
                var name=address.ToString()[(GistAside.RawHost+Raw).Length..];
                return name==refused ? Task.FromException<string>(new GistAside.RawRefusedException(HttpStatusCode.NotFound)):Task.FromResult(files[name]);
            }
            var threw=false;
            try{GistAside.Read(()=>Task.FromResult<JsonNode?>(Listing(false)),address=>RawRead(address,refusedName),keys).GetAwaiter().GetResult();}
            catch(GistAside.RawRefusedException){threw=true;}
            Check(threw,"without the skip, a refusal ends the read (counted by the exchange)");
            var skipped=0;
            var kept=GistAside.Read(()=>Task.FromResult<JsonNode?>(Listing(false)),GistAside.SkipRefused(address=>RawRead(address,refusedName),()=>skipped++),keys).GetAwaiter().GetResult();
            Check(skipped==1 && kept.Count==1 && kept[0].Kind==DiscoveryKind.Venture,"the refused kind skipped, the other kept");
            skipped=0;var unreadable=false;
            try{GistAside.Read(()=>Task.FromResult<JsonNode?>(Listing(true)),GistAside.SkipRefused(address=>RawRead(address,ExchangeV3.IndexName),()=>skipped++),keys).GetAwaiter().GetResult();}
            catch(InvalidDataException){unreadable=true;}
            Check(unreadable && skipped==1,"the index refused: nothing kept (settled as a gist this version cannot read)");
        }),
        ("Gist refused 1597: X-640, the messages name the limit in both languages",() =>
        {
            foreach(var language in new[]{"en","fr"})
            {
                var texts=JsonNode.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")))!["DiscoveriesUi"]!;
                var limit=GistAside.RefusalLimit.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Check(texts["ShareAsideRefused"]?.GetValue<string>() is { } refused && refused.Contains(limit,StringComparison.Ordinal) && refused.Contains("gist.githubusercontent.com",StringComparison.Ordinal),language+": the refusal says its host and the limit");
                Check(texts["ShareAsideGivenUp"]?.GetValue<string>() is { } given && given.Contains(limit,StringComparison.Ordinal),language+": the give-up says the limit");
            }
        }),
    ];
}
