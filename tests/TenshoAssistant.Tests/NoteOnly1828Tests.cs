using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1828 (X-825, question of the laptop worker of 22/11 around 20 h 10): a player file merged before X-804 (a header without Gist nor Revision)
// whose gist brings nothing new gave « nothing new »: no pull request noted the revision, so the plugin never deleted that gist (its exact places
// and days stayed public), against X-804 for every gist merged before it (ticket #43 among them). The master's answer: a « note only » pull
// request (as sync writes it), changing the player file's header only (Gist, Revision, Counted), the facts the same, v3/ unchanged; the trusted
// merge (tools/discoveries-trust.py) takes it as a safe case: the player file the only file changed, its fact lines byte for byte Devloppement's,
// its header changed by the note only; anything more waits for the maintainer's word. Here: the wiring (the selftests of both tools hold the
// cases: sync writes the note alone; a note only merged; a fact or a common file changed beside it: a doubt) and the end to end of X-804 on a
// file of before: the merge tool writes the note-only file from an old one, a made-up GitHub serves the old then the new file, and the plugin
// deletes the gist only once the revision is noted.
internal static class NoteOnly1828Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private const string IssueAddress="https://api.github.com/repos/"+GitHubExchange.Repository+"/issues/7";
    private const string Gist="0123456789abcdef0123",Revision="abababababababababababababababababababab";

    private sealed class Server : HttpMessageHandler
    {
        public byte[]? PlayerFile;
        public readonly List<(string Method,string Address)> Asked=[];
        public int Count(string method,string address){lock(Asked)return Asked.Count(a=>a.Method==method && a.Address==address);}
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();var method=request.Method.Method;
            lock(Asked)Asked.Add((method,address));
            if(method=="GET" && address==GistRotation.NotesAddress+"7.jsonl.gz")
                return Task.FromResult(PlayerFile is null ? new HttpResponseMessage(HttpStatusCode.NotFound) : new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(PlayerFile)});
            if(method=="POST" && address=="https://api.github.com/gists")return Task.FromResult(Json(new JsonObject{["id"]="g2",["history"]=new JsonArray(new JsonObject{["version"]="r2"})}));
            if(method=="PATCH" && address.StartsWith("https://api.github.com/gists/",StringComparison.Ordinal))
                return Task.FromResult(Json(new JsonObject{["id"]=Gist,["history"]=new JsonArray(new JsonObject{["version"]=Revision})}));   // the gist's revision the tool reads and notes
            if(method=="DELETE")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            if(method=="PATCH" && address==IssueAddress)return Task.FromResult(Json(new JsonObject{["number"]=7}));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
        private static HttpResponseMessage Json(JsonObject value)=>new(HttpStatusCode.OK){Content=new StringContent(value.ToJsonString())};
    }

    private static DiscoveryFact Fact(uint subject)
    {
        var x=10f+60f*subject;var z=-20f;var (cellX,cellZ)=DiscoveryStore.Cell(x,z);
        var day=new DateTimeOffset(2026,11,1,0,0,0,TimeSpan.Zero);
        return new DiscoveryFact{Kind=DiscoveryKind.MonsterPlace,Subject=subject,Territory=148,Map=4,CellX=cellX,CellZ=cellZ,X=x,Z=z,LevelMin=5,LevelMax=6,Seen=1,First=day,Last=day};
    }

    // The merge tool's own replay of sync (tools/discoveries-trust.py replay): a share merged before X-804 (no note), then the same share's gist
    // read again with nothing new (the note written). Prints whether the trusted merge reads it as a « note only », and writes both player files.
    private const string Maker="""
import importlib.util,os,sys
spec=importlib.util.spec_from_file_location("discoveries_trust",sys.argv[1]);t=importlib.util.module_from_spec(spec);spec.loader.exec_module(t)
m=t.tool();out=sys.argv[2];gist,revision=sys.argv[3],sys.argv[4]
raw={"Kind":1,"Subject":2,"Detail":0,"Territory":128,"Map":1,"CellX":1,"CellZ":2,"X":60.0,"Z":140.0,"LevelMin":1,"LevelMax":1,"Seen":2,"Imported":0,
     "First":"2026-11-10T00:00:00+00:00","Last":"2026-11-12T00:00:00+00:00"}
facts=m.clean({"Version":2,"Facts":[raw]},player=True)[0]
old=t.replay({},7,"tester",facts,{})
new=t.replay(old,7,"tester",facts,{"Gist":gist,"Revision":revision})
mine=f"{t.FOLDERS[0]}/7{m.PLAYER_SUFFIX}"
changed=sorted(p for p in new if new[p]!=old.get(p))
open(os.path.join(out,"old.jsonl.gz"),"wb").write(old[mine]);open(os.path.join(out,"new.jsonl.gz"),"wb").write(new[mine])
print("note only:",t.note_only(old[mine],new[mine]),"changed:",",".join(changed))
""";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Note only 1828: wiring: the trusted merge takes a note-only pull request as safe, and both selftests hold its cases",() =>
        {
            var trust=Read("tools/discoveries-trust.py");
            Check(trust.Contains("NOTE_FIELDS=(\"Gist\",\"Revision\",\"Counted\")",StringComparison.Ordinal) && trust.Contains("def note_only(before,after):",StringComparison.Ordinal),"note_only");
            Check(trust.Contains("if held.get(\"same\") and not (share.get(\"note_only\") is True and paths==[f\"community-discoveries/players/{number}.jsonl.gz\"]):return \"doubt\",\"facts\"",StringComparison.Ordinal),
                "nothing new: a merge only as a note only, the player file alone");
            Check(trust.Contains("share[\"note_only\"]=mine in base and mine in head and note_only(base[mine],head[mine])",StringComparison.Ordinal),"computed on the bytes read from GitHub");
            Check(trust.Contains("no « Merged » comment on the ticket (as sync does)",StringComparison.Ordinal),"no « merged » notice for a note");
            foreach(var name in new[]{"X-825: a note only (the player file alone, its note alone changed): merged","X-825: a note only, the player file alone: merged, nothing commented",
                "X-825: {name}: a doubt, never merged","\"a note and a fact changed\"","\"a note and the common set changed\"","\"facts (nothing new, more than the note)\"","X-825: the header's note alone: a note only"})
                Check(trust.Contains(name,StringComparison.Ordinal),"trust selftest case: "+name);
            Check(Read("tools/discoveries-merge.py").Contains("X-825: sync, a share merged before X-804 with nothing new: its player file's note alone written",StringComparison.Ordinal),"merge selftest case: sync writes the note alone");
        }),
        ("Note only 1828: end to end of X-804 on a file of before: the old file deletes nothing, the note-only file the merge tool writes makes the plugin delete the gist",() =>
        {
            string? python=null;
            foreach(var name in new[]{"python","py"})
            {
                try
                {
                    using var probe=Process.Start(new ProcessStartInfo(name,"--version"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false});
                    if(probe is null)continue;
                    probe.WaitForExit(20000);
                    if(probe.ExitCode==0){python=name;break;}
                }
                catch(Exception){}
            }
            if(python is null)return; // no Python on this machine: the wiring case above still holds
            var root=Path.Combine(Path.GetTempPath(),"tensho-noteonly-"+Guid.NewGuid().ToString("N"));
            var tool=Path.Combine(root,"tool");var profile=Path.Combine(root,"profile");
            Directory.CreateDirectory(tool);Directory.CreateDirectory(profile);
            try
            {
                var script=Path.Combine(tool,"make.py");
                File.WriteAllText(script,Maker);
                var start=new ProcessStartInfo(python){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
                foreach(var argument in new[]{"-I","-X","utf8",script,Path.GetFullPath("tools/discoveries-trust.py"),tool,Gist,Revision})start.ArgumentList.Add(argument);
                using var run=Process.Start(start)!;
                var output=run.StandardOutput.ReadToEndAsync();var error=run.StandardError.ReadToEndAsync();
                Check(run.WaitForExit(120000),"the tool ends");
                Check(run.ExitCode==0 && output.Result.Contains("note only: True changed: community-discoveries/players/7.jsonl.gz",StringComparison.Ordinal),
                    "the merge tool's sync of a share with nothing new changes the player file's note only ("+run.ExitCode+"): "+output.Result+error.Result);
                var old=File.ReadAllBytes(Path.Combine(tool,"old.jsonl.gz"));var noted=File.ReadAllBytes(Path.Combine(tool,"new.jsonl.gz"));
                Check(GistRotation.Note(old) is null && GistRotation.Note(noted)==(Gist,Revision),"the plugin reads no note in the old file, the gist and revision in the new one");

                var store=new DiscoveryStore();store.Load(new DiscoveryFile{Facts=[Fact(1)]});
                File.WriteAllText(Path.Combine(profile,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="tester",GistId=Gist,Issue=7,ClientId=GitHubSignIn.Current.ClientId,UploadHash="old"}));
                File.WriteAllBytes(Path.Combine(profile,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
                var hash=GistRotation.Hash(CommunityDiscoveries.Mine(store.Snapshot(DateTimeOffset.UtcNow,false)).Facts.Single());
                GistRotation.Write(Path.Combine(profile,GistRotation.FileName),new GistRotationState{Issue=7,SentGist=Gist,SentRevision=Revision,Sent=new(){[Fact(1).Key]=hash}});
                ExchangeMessage Exchange(GitHubExchange exchange)
                {
                    Check(exchange.Exchange(store,true),"the exchange starts");
                    for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
                    Check(!exchange.Busy && exchange.Message is { Key:"ExchangeDone" },$"the exchange ends ({exchange.Message?.Key} {string.Join(", ",exchange.Message?.Args ?? [])})");
                    return exchange.Message!;
                }
                var server=new Server{PlayerFile=old};
                using(var exchange=new GitHubExchange(profile,server))Exchange(exchange);
                Check(server.Count("DELETE","https://api.github.com/gists/"+Gist)==0,"the old file (merged before X-804, no note): the gist kept");
                server.PlayerFile=noted;
                using(var exchange=new GitHubExchange(profile,server)){var said=Exchange(exchange);Check(said.Args[3]=="ShareMerged","« everything is in the common base »: "+said.Args[3]+" ("+string.Join("; ",server.Asked.Select(a=>a.Method+" "+a.Address[Math.Max(0,a.Address.Length-40)..]))+")");}
                Check(server.Count("DELETE","https://api.github.com/gists/"+Gist)==1 && server.Count("POST","https://api.github.com/gists")==0,
                    "the note-only file on main: the gist deleted (its exact places and days no longer public), no new gist");
                var meta=JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(profile,"github.json")))!;
                Check(meta.GistId=="" && meta.Issue==7,"no gist left, the issue kept for the next one");
            }
            finally { try { Directory.Delete(root,true); } catch(IOException){} catch(UnauthorizedAccessException){} }
        }),
    ];
}
