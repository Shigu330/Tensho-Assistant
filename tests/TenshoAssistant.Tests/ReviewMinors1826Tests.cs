using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1826 (X-824, review of 0.1823, minor findings). M1: « ShareOldGistKept » took the place of « Sent », so the give-up of copies set aside
// (« ShareAsideGivenUp ») was never said while another account's merged gist waited, and meta.AsideGivenUp stayed true: now the upload itself
// tells it. Behaviour test (R28.14) against a fake GitHub. M2: in tools/discoveries-trust.py, passing states (mergeable « UNKNOWN », a commit
// status still pending) wait instead of a public doubt; the tool's selftest holds its cases and is run by DiscoveryOrigin1824Tests.
internal static class ReviewMinors1826Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string Gists="https://api.github.com/gists";

    private sealed class Server : HttpMessageHandler
    {
        public readonly List<(string Method,string Address)> Asked=[];
        private int made=1;
        public int Count(string method,string address){lock(Asked)return Asked.Count(a=>a.Method==method && a.Address==address);}
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();var method=request.Method.Method;
            lock(Asked)Asked.Add((method,address));
            if(method=="POST" && address==Gists)
            {
                var id="g"+Interlocked.Increment(ref made);
                return Task.FromResult(Json(new JsonObject{["id"]=id,["history"]=new JsonArray(new JsonObject{["version"]="r"+id[1..]})}));
            }
            if(method=="PATCH" && address.StartsWith(Gists+"/",StringComparison.Ordinal))
                return Task.FromResult(Json(new JsonObject{["id"]=address[(Gists.Length+1)..],["history"]=new JsonArray(new JsonObject{["version"]="rp"})}));
            if(method=="DELETE")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            if(address.Contains("/issues",StringComparison.Ordinal))return Task.FromResult(Json(new JsonObject{["number"]=method=="POST" ? 8:7}));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
        private static HttpResponseMessage Json(JsonObject value)=>new(HttpStatusCode.OK){Content=new StringContent(value.ToJsonString())};
    }

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-minors-1826-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try{run(root);}finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    private static DiscoveryStore Store(params uint[] subjects)
    {
        var day=new DateTimeOffset(2026,11,1,0,0,0,TimeSpan.Zero);
        var store=new DiscoveryStore();
        store.Load(new DiscoveryFile{Facts=[..subjects.Select(subject=>
        {
            var x=10f+60f*subject;var z=-20f;var (cellX,cellZ)=DiscoveryStore.Cell(x,z);
            return new DiscoveryFact{Kind=DiscoveryKind.MonsterPlace,Subject=subject,Territory=148,Map=4,CellX=cellX,CellZ=cellZ,X=x,Z=z,LevelMin=5,LevelMax=6,Seen=1,First=day,Last=day};
        })]});
        return store;
    }
    private static ExchangeMessage Run(GitHubExchange exchange,DiscoveryStore store)
    {
        Check(exchange.Exchange(store,true),"the exchange starts");
        for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is { Key:"ExchangeDone" },$"the exchange ends ({exchange.Message?.Key} {string.Join(", ",exchange.Message?.Args ?? [])})");
        return exchange.Message!;
    }
    private static GitHubExchangeMeta Meta(string root)=>JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(root,"github.json")))!;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Review minors 1826: M1, copies given up and another account's merged gist waiting: the send says the give-up once, then the gist kept",() => InTemp(root =>
        {
            // Connected as « other », no gist yet; copies set aside were given up at an earlier exchange; g1, merged, still to delete by « tester ».
            File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="other",GistLogin="other",ClientId=GitHubSignIn.Current.ClientId,UploadHash="old",AsideGivenUp=true}));
            File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
            GistRotation.Write(Path.Combine(root,GistRotation.FileName),new GistRotationState{Issue=7,OldGist="g1",OldLogin="tester",Rotated=true});
            var server=new Server();
            using var exchange=new GitHubExchange(root,server);
            var said=Run(exchange,Store(1));
            Check(said.Args[3]=="ShareAsideGivenUp" && server.Count("POST",Gists)==1,$"the send that replaced the gist says the give-up ({said.Args[3]})");
            Check(!Meta(root).AsideGivenUp,"said once: no longer to say");
            Check(Run(exchange,Store(1,2)).Args[3]=="ShareOldGistKept" && server.Count("DELETE",Gists+"/g1")==0,"the next exchange says the gist kept, still not deleted");
            Check(Run(exchange,Store(1,2,3)).Args[3]=="ShareOldGistKept","and again while it waits; the give-up never said twice");
        })),
        ("Review minors 1826: M1 and M2 wired (the upload tells the give-up; the trusted merge waits on passing states)",() =>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs")).ReplaceLineEndings("\n");
            Check(code.Contains("if(replaced)lock(gate)if(meta.AsideGivenUp){meta.AsideGivenUp=false;sent=\"ShareAsideGivenUp\";}",StringComparison.Ordinal)
                && !code.Contains("if(sent==\"Sent\")lock(gate)if(meta.AsideGivenUp)",StringComparison.Ordinal),"M1: told by the upload");
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-trust.py")).ReplaceLineEndings("\n");
            Check(tool.Contains("if pr.get(\"mergeable\")==\"UNKNOWN\":return \"wait\"",StringComparison.Ordinal) && tool.Contains("checks=[check_row(c) for c in info.get(\"statusCheckRollup\") or []]",StringComparison.Ordinal)
                && !tool.Contains("(c.get(\"status\") or \"COMPLETED\")",StringComparison.Ordinal),"M2: passing states wait");
            foreach(var name in new[]{"X-824 M2: mergeable not computed yet: wait","X-824 M2: a commit status still pending is no failure","X-824 M2: a check run without its status is not finished: wait",
                "X-824 M2: a commit status in error is still a failure"})
                Check(tool.Contains(name,StringComparison.Ordinal),"selftest case: "+name);
        }),
    ];
}
