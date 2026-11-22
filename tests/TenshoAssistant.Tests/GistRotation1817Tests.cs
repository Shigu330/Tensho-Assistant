using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1817 (X-804, user decision of 22/11 around 14 h 35): the gist is deleted once its facts are merged, a new one holds only what is new.
// Behaviour tests (R28.14) against a fake GitHub: merge confirmed (new gist with the news, issue edited, old gist deleted, in that order);
// nothing new (gist deleted, issue kept for the next gist); a note of another revision (nothing deleted, nothing lost); a deletion refused then
// retried; two clients of the profile at once (one new gist, no new issue). The merge tool's side is in its selftest.
internal static class GistRotation1817Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string IssueAddress="https://api.github.com/repos/"+GitHubExchange.Repository+"/issues/7";

    private sealed class Server : HttpMessageHandler
    {
        public (string Gist,string Revision)? Note=("g1","r1");
        public HttpStatusCode DeleteStatus=HttpStatusCode.NoContent;
        public readonly List<(string Method,string Address,string Body)> Asked=[];
        private int made=1;
        public int Count(string method,string address){lock(Asked)return Asked.Count(a=>a.Method==method && a.Address==address);}
        public int Index(string method,string address){lock(Asked)return Asked.FindIndex(a=>a.Method==method && a.Address==address);}
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
            if(method=="POST" && address=="https://api.github.com/gists")
            {
                var id="g"+Interlocked.Increment(ref made);
                return Json(new JsonObject{["id"]=id,["history"]=new JsonArray(new JsonObject{["version"]="r"+id[1..]})});
            }
            if(method=="PATCH" && address.StartsWith("https://api.github.com/gists/",StringComparison.Ordinal))
                return Json(new JsonObject{["id"]=address[^2..],["history"]=new JsonArray(new JsonObject{["version"]="rp"})});
            if(method=="DELETE")return new HttpResponseMessage(DeleteStatus);
            if(method=="PATCH" && address==IssueAddress)return Json(new JsonObject{["number"]=7});
            if(method=="POST" && address.EndsWith("/issues",StringComparison.Ordinal))return Json(new JsonObject{["number"]=8});
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
        private static HttpResponseMessage Json(JsonObject value)=>new(HttpStatusCode.OK){Content=new StringContent(value.ToJsonString())};
    }

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-rotation-1817-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
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
    private static string KeyOf(uint subject)=>Fact(subject).Key;

    /// <summary>The profile: connected as « tester », gist g1, issue 7; the last upload (g1, revision r1) held A (subject 1).</summary>
    private static void Profile(string root,DiscoveryStore store)
    {
        File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="tester",GistId="g1",Issue=7,ClientId=GitHubSignIn.Current.ClientId,UploadHash="old"}));
        File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
        GistRotation.Write(Path.Combine(root,GistRotation.FileName),new GistRotationState{Issue=7,SentGist="g1",SentRevision="r1",Sent=new(){[KeyOf(1)]=MinedHash(store,1)}});
    }
    private static ExchangeMessage Run(GitHubExchange exchange,DiscoveryStore store)
    {
        Check(exchange.Exchange(store,true),"the exchange starts");
        for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is { Key:"ExchangeDone" },$"the exchange ends ({exchange.Message?.Key} {string.Join(", ",exchange.Message?.Args ?? [])})");
        return exchange.Message!;
    }
    private static GistRotationState State(string root)=>GistRotation.Read(Path.Combine(root,GistRotation.FileName),7);
    private static GitHubExchangeMeta Meta(string root)=>JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(root,"github.json")))!;
    /// <summary>The subjects a gist write carries (its index and kind files, as the merge tool reads them).</summary>
    private static uint[] Subjects(string body)
    {
        var files=JsonNode.Parse(body)!["files"]!.AsObject();
        var index=ExchangeV3.ParseIndex(files[ExchangeV3.IndexName]!["content"]!.GetValue<string>())!;
        return [..index.Files.SelectMany(entry=>ExchangeV3.ReadKind(new MemoryStream(Convert.FromBase64String(files[entry.Name+ExchangeV3.GistSuffix]!["content"]!.GetValue<string>())),entry).Facts).Select(fact=>fact.Subject).Order()];
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gist rotation 1817: merge confirmed: a new gist with the news only, the issue edited to link it, then the old gist deleted, in that order",() => InTemp(root =>
        {
            var store=Store(1,2);Profile(root,store);var server=new Server();
            using(var exchange=new GitHubExchange(root,server))Check(Run(exchange,store).Args[3]=="Sent","sent");
            var made=server.Index("POST","https://api.github.com/gists");var edited=server.Index("PATCH",IssueAddress);var deleted=server.Index("DELETE","https://api.github.com/gists/g1");
            Check(made>=0 && edited>made && deleted>edited,$"order: new gist {made}, issue {edited}, delete {deleted}");
            Check(Subjects(server.Body("POST","https://api.github.com/gists")).SequenceEqual([2u]),"the new gist holds B only (A is in the common base)");
            Check(server.Body("PATCH",IssueAddress).Contains("https://gist.github.com/g2",StringComparison.Ordinal) && server.Count("POST","https://api.github.com/repos/"+GitHubExchange.Repository+"/issues")==0,"the issue links g2; no new issue");
            Check(server.Count("PATCH","https://api.github.com/gists/g1")==0,"the merged gist is not written again");
            var state=State(root);var meta=Meta(root);
            Check(meta.GistId=="g2" && meta.Issue==7 && state.SentGist=="g2" && state.SentRevision=="r2" && state.Sent.Keys.SequenceEqual([KeyOf(2)])
                && state.Merged.ContainsKey(KeyOf(1)) && state.OldGist=="" && !state.Rotated,"g2 in use, its revision kept, A known merged, nothing left to delete");
        })),
        ("Gist rotation 1817: nothing new: the gist deleted, the issue kept; the next discovery makes a new gist linked from the same issue",() => InTemp(root =>
        {
            var store=Store(1);Profile(root,store);var server=new Server();
            using(var exchange=new GitHubExchange(root,server))
            {
                Check(Run(exchange,store).Args[3]=="ShareMerged","« everything is in the common base »");
                Check(server.Count("DELETE","https://api.github.com/gists/g1")==1 && server.Count("POST","https://api.github.com/gists")==0 && server.Count("PATCH",IssueAddress)==0,"g1 deleted, no gist made");
                Check(Meta(root).GistId=="" && Meta(root).Issue==7 && State(root) is { Rotated:true, OldGist:"" },"no gist, the issue kept, the next gist to link from it");
                var more=Store(1,3);
                Check(Run(exchange,more).Args[3]=="Sent","C sent");
                Check(Subjects(server.Body("POST","https://api.github.com/gists")).SequenceEqual([3u]) && server.Body("PATCH",IssueAddress).Contains("https://gist.github.com/g2",StringComparison.Ordinal)
                    && server.Count("POST","https://api.github.com/repos/"+GitHubExchange.Repository+"/issues")==0,"a new gist with C only, issue 7 edited to link it, no new issue");
                Check(State(root) is { Rotated:false, SentGist:"g2" } st && st.Merged.ContainsKey(KeyOf(1)),"A still known merged");
            }
        })),
        ("Gist rotation 1817: a note of another revision: nothing deleted, the gist written whole, nothing lost",() => InTemp(root =>
        {
            var store=Store(1,2);Profile(root,store);var server=new Server{Note=("g1","r0")};
            using(var exchange=new GitHubExchange(root,server))Check(Run(exchange,store).Args[3]=="Sent","sent");
            Check(server.Count("DELETE","https://api.github.com/gists/g1")==0 && server.Count("POST","https://api.github.com/gists")==0,"nothing deleted, no new gist");
            Check(Subjects(server.Body("PATCH","https://api.github.com/gists/g1")).SequenceEqual([1u,2u]),"g1 holds A and B: A not merged yet stays");
            Check(State(root).Merged.Count==0,"nothing known merged");
            var none=new Server{Note=null};var again=Store(1,2,4);
            using(var exchange=new GitHubExchange(root,none))Run(exchange,again);
            Check(none.Count("DELETE","https://api.github.com/gists/g1")==0 && Subjects(none.Body("PATCH","https://api.github.com/gists/g1")).SequenceEqual([1u,2u,4u]),"no player file yet: nothing deleted, everything sent");
        })),
        ("Gist rotation 1817: a deletion refused stays to do, and is done at the next exchange",() => InTemp(root =>
        {
            var store=Store(1,2);Profile(root,store);var server=new Server{DeleteStatus=HttpStatusCode.InternalServerError};
            using(var exchange=new GitHubExchange(root,server))
            {
                Run(exchange,store);
                Check(Meta(root).GistId=="g2" && State(root).OldGist=="g1","g2 in use, g1 still to delete");
                server.DeleteStatus=HttpStatusCode.NoContent;
                Check(Run(exchange,store).Args[3]=="SentSame","nothing new sent");
                Check(server.Count("DELETE","https://api.github.com/gists/g1")==2 && State(root).OldGist=="" && server.Count("POST","https://api.github.com/gists")==1,"deleted at the second try, no other gist made");
            }
        })),
        ("Gist rotation 1817: two clients of the profile at once: one new gist, one deletion, no new issue",() => InTemp(root =>
        {
            var store=Store(1,2);Profile(root,store);var server=new Server();
            using var first=new GitHubExchange(root,server);
            using var second=new GitHubExchange(root,server);
            Check(first.Exchange(store,true) && second.Exchange(store,true),"both start");
            for(var wait=0;(first.Busy || second.Busy) && wait<3000;wait++)Thread.Sleep(10);
            Check(!first.Busy && !second.Busy,"both end");
            var said=new[]{first.Message!.Args[3],second.Message!.Args[3]}.Order().ToArray();
            Check(said.SequenceEqual(["Sent","SentSame"]),"one sends, the other finds it sent: "+string.Join(", ",said)+" ("+string.Join("; ",server.Asked.Select(a=>a.Method+" "+a.Address[Math.Max(0,a.Address.Length-30)..]))+")");
            Check(server.Count("POST","https://api.github.com/gists")==1 && server.Count("DELETE","https://api.github.com/gists/g1")==1
                && server.Count("POST","https://api.github.com/repos/"+GitHubExchange.Repository+"/issues")==0 && server.Count("PATCH","https://api.github.com/gists/g2")==0,
                "one new gist, one deletion, no new issue, g2 not written twice");
        })),
        ("Gist rotation 1817: the player file's note read from its header only; a file of another format or without both fields gives none",() =>
        {
            // X-830: the note is the list of gists merged (merged.txt: hashes of « gist:revision »), no longer a player file's header
            byte[] List(params (string,string)[] notes)=>Encoding.UTF8.GetBytes(GistRotation.NotesText(notes));
            Check(GistRotation.Note(List(("g0","r0"),("g1","r1")),"g1","r1")==("g1","r1") && GistRotation.NoteHash("g1","r1")==Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("g1:r1"))).ToLowerInvariant(),"the note");
            Check(GistRotation.Note(List(("g1","r0")),"g1","r1") is null && GistRotation.Note(List(("g1","r1")),"g1","") is null && GistRotation.Note(List(),"g1","r1") is null,"another revision, no revision, or an empty list: none");
            Check(GistRotation.Note(Encoding.UTF8.GetBytes("# other\n"+GistRotation.NoteHash("g1","r1")+"\n"),"g1","r1") is null && GistRotation.Note([0xff,0xfe,1],"g1","r1") is null,"another header, or not UTF-8: none");
            Check(GistRotation.Note(new byte[GistRotation.MaxNoteFileBytes+1],"g1","r1") is null && !GistRotation.NotesText([("g1","r1")]).Contains("g1",StringComparison.Ordinal),"a list past its bound: none; the list names no gist");
            var state=new GistRotationState{Issue=7,SentGist="g1",SentRevision="r1",Sent=new(){["a"]="1"},Merged=new(){["b"]="2"}};
            Check(GistRotation.Confirmed(("g1","r1"),state,"g1") && !GistRotation.Confirmed(("g1","r1"),state,"g2") && !GistRotation.Confirmed(null,state,"g1"),"confirmed only for the gist in use and its revision");
            var after=GistRotation.AfterMerge(state);
            Check(after.Merged.Count==2 && after.Sent.Count==0 && after.OldGist=="g1" && after.Rotated && after.Issue==7,"after a merge: both known, g1 to delete");
        }),
    ];
}
