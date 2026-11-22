using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1820 (X-819, review of 0.1819, I1): a gist made, then the issue step failing, is kept by the rotation's state: the next exchange writes it
// again and links it, never a second gist. Behaviour tests (R28.14) against a fake GitHub.
internal static class GistKept1820Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string Gists="https://api.github.com/gists";
    private const string Issues="https://api.github.com/repos/"+GitHubExchange.Repository+"/issues";
    private const string IssueAddress=Issues+"/7";

    private sealed class Server : HttpMessageHandler
    {
        public (string Gist,string Revision)? Note=("g1","r1");
        public HttpStatusCode IssueEdit=HttpStatusCode.OK,IssueOpen=HttpStatusCode.OK,OldGist=HttpStatusCode.OK;
        public readonly List<(string Method,string Address,string Body)> Asked=[];
        private int made=1;
        public int Count(string method,string address){lock(Asked)return Asked.Count(a=>a.Method==method && a.Address==address);}
        public string Body(string method,string address){lock(Asked)return Asked.Last(a=>a.Method==method && a.Address==address).Body;}
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();var method=request.Method.Method;
            var body=request.Content is null ? "":await request.Content.ReadAsStringAsync(token);
            lock(Asked)Asked.Add((method,address,body));
            if(method=="GET" && address==GistRotation.NotesAddress)
            {
                if(Note is not { } note)return new HttpResponseMessage(HttpStatusCode.NotFound);
                // X-830: the list of gists merged (merged.txt), no longer a player file
                return new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(Encoding.UTF8.GetBytes(GistRotation.NotesText([note])))};
            }
            if(method=="POST" && address==Gists)
            {
                var id="g"+Interlocked.Increment(ref made);
                return Json(new JsonObject{["id"]=id,["history"]=new JsonArray(new JsonObject{["version"]="r"+id[1..]})});
            }
            if(method=="PATCH" && address==Gists+"/g1" && OldGist!=HttpStatusCode.OK)return new HttpResponseMessage(OldGist);
            if(method=="PATCH" && address.StartsWith(Gists+"/",StringComparison.Ordinal))
                return Json(new JsonObject{["id"]=address[(Gists.Length+1)..],["history"]=new JsonArray(new JsonObject{["version"]="rp"})});
            if(method=="DELETE")return new HttpResponseMessage(HttpStatusCode.NoContent);
            if(method=="PATCH" && address==IssueAddress)return IssueEdit==HttpStatusCode.OK ? Json(new JsonObject{["number"]=7}):new HttpResponseMessage(IssueEdit);
            if(method=="POST" && address==Issues)return IssueOpen==HttpStatusCode.OK ? Json(new JsonObject{["number"]=8}):new HttpResponseMessage(IssueOpen);
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
        private static HttpResponseMessage Json(JsonObject value)=>new(HttpStatusCode.OK){Content=new StringContent(value.ToJsonString())};
    }

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-kept-1820-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try{run(root);}finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    private static DiscoveryFact Fact(uint subject)
    {
        var x=10f+60f*subject;var z=-20f;var (cellX,cellZ)=DiscoveryStore.Cell(x,z);
        var day=new DateTimeOffset(2026,11,1,0,0,0,TimeSpan.Zero);
        return new DiscoveryFact{Kind=DiscoveryKind.MonsterPlace,Subject=subject,Territory=148,Map=4,CellX=cellX,CellZ=cellZ,X=x,Z=z,LevelMin=5,LevelMax=6,Seen=1,First=day,Last=day};
    }
    private static DiscoveryStore Store(params uint[] subjects)
    {
        var store=new DiscoveryStore();
        store.Load(new DiscoveryFile{Facts=[..subjects.Select(Fact)]});
        return store;
    }
    private static string MinedHash(DiscoveryStore store,uint subject)
        =>GistRotation.Hash(CommunityDiscoveries.Mine(store.Snapshot(DateTimeOffset.UtcNow,false)).Facts.Single(fact=>fact.Subject==subject));

    /// <summary>Connected as « tester », gist g1, issue 7; the last upload (g1, r1) held A. <paramref name="known"/>: the rotation's state names g1 (a rotation done before).</summary>
    private static void Profile(string root,DiscoveryStore store,bool known)
    {
        File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="tester",GistId="g1",Issue=7,ClientId=GitHubSignIn.Current.ClientId,UploadHash="old"}));
        File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
        GistRotation.Write(Path.Combine(root,GistRotation.FileName),new GistRotationState{Issue=7,SentGist="g1",SentRevision="r1",Sent=new(){[Fact(1).Key]=MinedHash(store,1)},
            Known=known,Login=known ? "tester":"",Gist=known ? "g1":"",GistFiles=[],UploadHash=known ? "old":""});
    }
    private static ExchangeMessage Run(GitHubExchange exchange,DiscoveryStore store)
    {
        Check(exchange.Exchange(store,true),"the exchange starts");
        for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is not null,"the exchange ends");
        return exchange.Message!;
    }
    private static GitHubExchangeMeta Meta(string root)=>JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(root,"github.json")))!;
    private static GistRotationState? State(string root)=>GistRotation.Load(Path.Combine(root,GistRotation.FileName));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gist kept 1820: merge confirmed, new gist made, the issue edit refused (5xx): the next exchange writes that gist again, links it, deletes the merged one; no second gist",() => InTemp(root =>
        {
            var store=Store(1,2);Profile(root,store,known:false);var server=new Server{IssueEdit=HttpStatusCode.InternalServerError};
            using var exchange=new GitHubExchange(root,server);
            Check(Run(exchange,store) is not { Key:"ExchangeDone" },"the first exchange fails at the issue");
            Check(server.Count("POST",Gists)==1 && State(root) is { Known:true, Gist:"g2", Rotated:true, Issue:7 },"g2 made and noted in the rotation's state, the rotation still to end");
            server.IssueEdit=HttpStatusCode.OK;
            var said=Run(exchange,store);
            Check(said is { Key:"ExchangeDone" } && said.Args[3]=="Sent",$"the second exchange sends ({said.Key} {string.Join(", ",said.Args)})");
            Check(server.Count("POST",Gists)==1 && server.Count("PATCH",Gists+"/g2")==1,"g2 written again, no other gist");
            Check(server.Body("PATCH",IssueAddress).Contains("https://gist.github.com/g2",StringComparison.Ordinal) && server.Count("POST",Issues)==0,"issue 7 edited to link g2, no new issue");
            Check(server.Count("DELETE",Gists+"/g1")==1 && server.Count("DELETE",Gists+"/g2")==0,"g1 deleted, g2 kept");
            Check(State(root) is { Gist:"g2", SentGist:"g2", Rotated:false, OldGist:"" } st && st.Merged.ContainsKey(Fact(1).Key) && Meta(root).GistId=="g2","g2 in use, A still known merged");
        })),
        ("Gist kept 1820: the rotation's state names a gist gone (404), a new one made, the new issue refused: the next exchange keeps that gist and opens the issue for it",() => InTemp(root =>
        {
            var store=Store(1,2);Profile(root,store,known:true);
            var server=new Server{Note=("g1","r0"),OldGist=HttpStatusCode.NotFound,IssueOpen=HttpStatusCode.BadGateway};
            using var exchange=new GitHubExchange(root,server);
            Check(Run(exchange,store) is not { Key:"ExchangeDone" },"the first exchange fails at the issue");
            Check(server.Count("POST",Gists)==1 && State(root) is { Gist:"g2", Issue:0 } && Meta(root).GistId=="g2","g2 made and noted, its issue still to open");
            server.IssueOpen=HttpStatusCode.OK;
            Check(Run(exchange,store) is { Key:"ExchangeDone" } said && said.Args[3]=="Sent","the second exchange sends");
            Check(server.Count("POST",Gists)==1 && server.Count("PATCH",Gists+"/g2")==1 && server.Count("PATCH",Gists+"/g1")==1,"g2 written again, g1 not asked again, no third gist");
            Check(server.Count("POST",Issues)==2 && server.Body("POST",Issues).Contains("https://gist.github.com/g2",StringComparison.Ordinal),"the issue opened links g2");
            Check(State(root) is { Gist:"g2", Issue:8, SentGist:"g2" } && Meta(root) is { GistId:"g2", Issue:8 },"g2 and issue 8 in use");
        })),
        ("Gist kept 1820: an upload that fails before any gist is made changes nothing of the rotation's gist",() => InTemp(root =>
        {
            var store=Store(1,2);Profile(root,store,known:true);
            var server=new Server{Note=("g1","r0"),OldGist=HttpStatusCode.InternalServerError};
            using var exchange=new GitHubExchange(root,server);
            Check(Run(exchange,store) is not { Key:"ExchangeDone" },"the exchange fails at the gist");
            Check(server.Count("POST",Gists)==0 && State(root) is { Gist:"g1", Issue:7, UploadHash:"old" } && Meta(root) is { GistId:"g1", Issue:7 },"g1 and issue 7 kept, nothing made");
        })),
        ("Gist kept 1820: the upload's failure written to the rotation's state (KeepGist), the rotated issue edited at every try",() =>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs")).ReplaceLineEndings("\n");
            Check(code.Contains("catch{KeepGist(state);throw;}",StringComparison.Ordinal) && code.Contains("if(issue>0 && rotated)",StringComparison.Ordinal),"wired");
        }),
    ];
}
