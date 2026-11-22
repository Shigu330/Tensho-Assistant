using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1822 (X-820 M1, review of 0.1819): the merged gist still to delete is deleted only by the account it belongs to. Another account connected
// on the same profile would be answered 404 (taken for « already gone ») though the gist stays: it is kept noted and said. Behaviour tests
// (R28.14) against a fake GitHub.
internal static class GistOwner1822Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string Gists="https://api.github.com/gists";

    private sealed class Server : HttpMessageHandler
    {
        public (string Gist,string Revision)? Note=null;
        public HttpStatusCode Delete=HttpStatusCode.NoContent;
        public readonly List<(string Method,string Address)> Asked=[];
        private int made=1;
        public int Count(string method,string address){lock(Asked)return Asked.Count(a=>a.Method==method && a.Address==address);}
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();var method=request.Method.Method;
            lock(Asked)Asked.Add((method,address));
            if(method=="GET" && address.StartsWith(GistRotation.NotesAddress,StringComparison.Ordinal) && Note is { } note)
            {
                // X-830: the list of gists merged (merged.txt), no longer a player file
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(Encoding.UTF8.GetBytes(GistRotation.NotesText([note])))});
            }
            if(method=="POST" && address==Gists)
            {
                var id="g"+Interlocked.Increment(ref made);
                return Task.FromResult(Json(new JsonObject{["id"]=id,["history"]=new JsonArray(new JsonObject{["version"]="r"+id[1..]})}));
            }
            if(method=="PATCH" && address.StartsWith(Gists+"/",StringComparison.Ordinal))
                return Task.FromResult(Json(new JsonObject{["id"]=address[(Gists.Length+1)..],["history"]=new JsonArray(new JsonObject{["version"]="rp"})}));
            if(method=="DELETE")return Task.FromResult(new HttpResponseMessage(Delete));
            if(address.Contains("/issues",StringComparison.Ordinal))return Task.FromResult(Json(new JsonObject{["number"]=method=="POST" ? 8:7}));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
        private static HttpResponseMessage Json(JsonObject value)=>new(HttpStatusCode.OK){Content=new StringContent(value.ToJsonString())};
    }

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-owner-1822-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
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
    /// <summary>Connected as <paramref name="login"/> with gist g2 and issue <paramref name="issue"/>; the rotation's state: g1 merged, still to delete, by <paramref name="owner"/>.</summary>
    private static void Profile(string root,string login,int issue,string owner)
    {
        File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login=login,GistLogin=login,GistId=issue>0 ? "g2":"",Issue=issue,ClientId=GitHubSignIn.Current.ClientId,UploadHash="old"}));
        File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
        GistRotation.Write(Path.Combine(root,GistRotation.FileName),new GistRotationState{Issue=7,OldGist="g1",OldLogin=owner,Rotated=true});
    }
    private static ExchangeMessage Run(GitHubExchange exchange,DiscoveryStore store)
    {
        Check(exchange.Exchange(store,true),"the exchange starts");
        for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is { Key:"ExchangeDone" },$"the exchange ends ({exchange.Message?.Key} {string.Join(", ",exchange.Message?.Args ?? [])})");
        return exchange.Message!;
    }
    private static GistRotationState State(string root)=>GistRotation.Load(Path.Combine(root,GistRotation.FileName))!;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gist owner 1822: another account connected on the profile: the merged gist of the account before is not « deleted » with its token, kept noted and said",() => InTemp(root =>
        {
            Profile(root,"other",0,"tester");var server=new Server();
            using var exchange=new GitHubExchange(root,server);
            Check(Run(exchange,Store(1)).Args[3]=="ShareOldGistKept","said: only the account before can delete it");
            Check(server.Count("DELETE",Gists+"/g1")==0 && State(root) is { OldGist:"g1", OldLogin:"tester" },"no DELETE sent, g1 still to delete by tester");
            Check(Run(exchange,Store(1)).Args[3]=="ShareOldGistKept" && server.Count("DELETE",Gists+"/g1")==0,"said again at the next exchange, still not sent");
        })),
        ("Gist owner 1822: the account it belongs to (any letter case) deletes it, and the state forgets it",() => InTemp(root =>
        {
            Profile(root,"Tester",7,"tester");var server=new Server();
            using var exchange=new GitHubExchange(root,server);
            Check(Run(exchange,Store(1)).Args[3]!="ShareOldGistKept","nothing to say");
            Check(server.Count("DELETE",Gists+"/g1")==1 && State(root) is { OldGist:"", OldLogin:"" },"g1 deleted, forgotten");
        })),
        ("Gist owner 1822: a merge confirmed notes the account of the gist to delete; a deletion refused keeps it",() => InTemp(root =>
        {
            File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="tester",GistLogin="tester",GistId="g1",Issue=7,ClientId=GitHubSignIn.Current.ClientId,UploadHash="old"}));
            File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
            GistRotation.Write(Path.Combine(root,GistRotation.FileName),new GistRotationState{Issue=7,SentGist="g1",SentRevision="r1",Sent=new(){["k"]="h"}});
            var server=new Server{Note=("g1","r1"),Delete=HttpStatusCode.InternalServerError};
            using var exchange=new GitHubExchange(root,server);
            Run(exchange,Store(1));
            Check(State(root) is { OldGist:"g1", OldLogin:"tester" },"g1 to delete, by tester");
            Check(!GistRotation.Deletes(new GistRotationState{OldGist="g1",OldLogin="tester"},"other") && GistRotation.Deletes(new GistRotationState{OldGist="g1"},"other"),
                "another account may not; a state of before that names no account: tried");
        })),
    ];
}
