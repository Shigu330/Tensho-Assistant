using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1600 (X-656, review of the block 0.1596 to 0.1598, minor findings M1 and M2): behaviour tests (R28.14).
// M1: « ShareAsideGivenUp » was said only when the send right after the give-up ended in « Sent »: an upload that failed (network, 5xx)
// left the rows settled without the refused copy and the next exchange said « Sent » only. It is now kept (github.json, AsideGivenUp) and
// said by the first send that replaces the gist, even after a restart.
// M2: the count of refusals « in a row » was reset only by a successful read: an exchange « not reached » or « blocked » between two refusals
// kept it, and rows set aside at a later start inherited it. It is now reset by any exchange that does not end in a refusal, and tied to the
// gist and the rows it is about (AsideRefusedFor).
internal static class GistGivenUp1600Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string Raw="tester/g1/raw/abc/";

    /// <summary>A fake GitHub: no common set (404); gist g1 listed, its kind files cut (raw address answering RawStatus); PATCH answering PatchStatus, recorded when 200.</summary>
    private sealed class Server : HttpMessageHandler
    {
        public IReadOnlyDictionary<string,string> Files=new Dictionary<string,string>();
        public HttpStatusCode RawStatus=HttpStatusCode.OK,PatchStatus=HttpStatusCode.OK;
        public int PatchTried;
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
                return RawStatus==HttpStatusCode.OK ? new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(Files[address[(GistAside.RawHost+Raw).Length..]])}
                    :new HttpResponseMessage(RawStatus);
            if(request.Method==HttpMethod.Get)return new HttpResponseMessage(HttpStatusCode.NotFound);
            var body=request.Content is null ? "":await request.Content.ReadAsStringAsync(token);
            Interlocked.Increment(ref PatchTried);
            if(PatchStatus!=HttpStatusCode.OK)return new HttpResponseMessage(PatchStatus);
            lock(Patches)Patches.Add(body);
            return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{}")};
        }
    }

    private static void Meta(string root,int refused=0,string refusedFor="")
        =>File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="tester",GistId="g1",Issue=7,ClientId=GitHubSignIn.Current.ClientId,AsideRefused=refused,AsideRefusedFor=refusedFor}));
    private static GitHubExchange Connected(string root,Server server)
    {
        File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
        var exchange=new GitHubExchange(root,server);
        Check(exchange.Connected,"connected");
        return exchange;
    }
    private static GitHubExchangeMeta Saved(string root)=>JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(root,"github.json")))!;

    /// <summary>One exchange to its end, whatever the end (a failed upload does not end in « ExchangeDone »).</summary>
    private static ExchangeMessage Run(GitHubExchange exchange,DiscoveryStore store)
    {
        Check(exchange.Exchange(store,true),"the exchange starts");
        for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is not null,"the exchange ends");
        return exchange.Message!;
    }
    private static string Said(ExchangeMessage message)=>message.Key=="ExchangeDone" ? message.Args[3]:message.Key;

    private static HashSet<string> Sent(Server server)
    {
        var files=JsonNode.Parse(server.Patches[^1])!["files"]!.AsObject();
        var index=ExchangeV3.ParseIndex(files[ExchangeV3.IndexName]!["content"]!.GetValue<string>())!;
        var keys=new HashSet<string>();
        foreach(var entry in index.Files)
            foreach(var fact in ExchangeV3.ReadKind(new MemoryStream(Convert.FromBase64String(files[entry.Name+ExchangeV3.GistSuffix]!["content"]!.GetValue<string>())),entry).Facts)
                keys.Add(fact.Key);
        return keys;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gist given up 1600: M1, the upload after the give-up fails: the give-up is kept and said by the next send, after a restart",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,a,b,_)=AsideGist1561Tests.Damaged(root);
            Meta(root);
            var server=new Server{Files=gist,RawStatus=HttpStatusCode.NotFound};
            var store=AsideGist1561Tests.Start(database);
            using(var exchange=Connected(root,server))
            {
                Check(Said(Run(exchange,store))=="ShareAsideRefused" && Said(Run(exchange,store))=="ShareAsideRefused" && Saved(root).AsideRefused==2,"two refusals counted");
                server.PatchStatus=HttpStatusCode.ServiceUnavailable;
                var failed=Run(exchange,store);
                Check(server.PatchTried>=1 && server.Patches.Count==0 && Said(failed)!="ShareAsideGivenUp","3rd: given up, rows settled, but the upload failed: not said ("+Said(failed)+")");
                Check(store.Aside is null && database.AsidePending().Count==0 && Saved(root).AsideGivenUp,"the rows settled, the give-up kept on disk");
            }
            server.PatchStatus=HttpStatusCode.OK;
            using(var restarted=Connected(root,server))
            {
                var said=Run(restarted,store);
                Check(Said(said)=="ShareAsideGivenUp" && server.Patches.Count==1,"the next send says it, after a restart: "+Said(said));
                var sent=Sent(server);
                Check(sent.Contains(a) && !sent.Contains(b),"A sent, B left out");
                Check(!Saved(root).AsideGivenUp,"said once: back to false");
                server.PatchStatus=HttpStatusCode.OK;
                store.SeeSource(DiscoveryKind.Venture,77,7,2,DateTimeOffset.UtcNow);
                Check(Said(Run(restarted,store))=="Sent","the send after: « Sent » only");
            }
        })),
        ("Gist given up 1600: M2, an exchange « not reached » between two refusals breaks the row: no give-up at the 3rd refusal",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,_,b,_)=AsideGist1561Tests.Damaged(root);
            Meta(root);
            var server=new Server{Files=gist,RawStatus=HttpStatusCode.NotFound};
            using var exchange=Connected(root,server);
            var store=AsideGist1561Tests.Start(database);
            Check(Said(Run(exchange,store))=="ShareAsideRefused" && Said(Run(exchange,store))=="ShareAsideRefused" && Saved(root).AsideRefused==2,"two refusals");
            server.RawStatus=HttpStatusCode.ServiceUnavailable;
            Check(Said(Run(exchange,store))=="ShareAsideWaiting" && Saved(root).AsideRefused==0,"not reached: the row broken, back to 0");
            server.RawStatus=HttpStatusCode.NotFound;
            var said=Run(exchange,store);
            Check(Said(said)=="ShareAsideRefused" && server.Patches.Count==0 && Saved(root).AsideRefused==1,"a refusal again: the 1st of a new row, nothing sent: "+Said(said));
            Check(database.AsidePending().SequenceEqual([b]) && store.Aside is not null,"B still waits for its copy");
        })),
        ("Gist given up 1600: M2, a count left by other rows or another gist does not count for these rows",() => AsideGist1561Tests.InTemp(root =>
        {
            var (database,gist,_,b,_)=AsideGist1561Tests.Damaged(root);
            Meta(root,refused:GistAside.RefusalLimit-1,refusedFor:"g1|0000000000000000");
            var server=new Server{Files=gist,RawStatus=HttpStatusCode.NotFound};
            using var exchange=Connected(root,server);
            var store=AsideGist1561Tests.Start(database);
            var said=Run(exchange,store);
            Check(Said(said)=="ShareAsideRefused" && server.Patches.Count==0 && Saved(root).AsideRefused==1,"other rows' count: these start at 1, nothing given up: "+Said(said));
            Check(Saved(root).AsideRefusedFor==GitHubExchange.AsideRows("g1",[b]),"the count now about g1 and B");
            Check(GitHubExchange.AsideRows("g1",["x","y"])==GitHubExchange.AsideRows("g1",["y","x"]) && GitHubExchange.AsideRows("g1",["x"])!=GitHubExchange.AsideRows("g2",["x"]),"the rows in any order; another gist differs");
        })),
    ];
}
