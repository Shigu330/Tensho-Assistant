using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1828 (X-825, question of the laptop worker of 22/11 around 20 h 10): a player file merged before X-804 (a header without Gist nor Revision)
// whose gist brings nothing new gave « nothing new »: no pull request noted the revision, so the plugin never deleted that gist (its exact places
// and days stayed public), against X-804 for every gist merged before it (ticket #43 among them). The master's answer then: a « note only » pull
// request changing the player file's header only.
// X-830 (D-226 a, D-227 B+C, user's choices of 22/11): the player files are anonymous, so the note left them: the gists merged are listed in
// community-discoveries/merged.txt (hashes of « gist:revision »). A gist with nothing new now changes that list only, which the trusted merge
// replays like any batch. Here: the wiring (both selftests hold the cases) and the end to end of X-804: the merge tool's replay of a batch
// whose gist brings nothing new changes merged.txt only; a made-up GitHub serves the list before then after, and the plugin deletes the gist
// only once its revision is listed.
internal static class NoteOnly1828Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private const string IssueAddress="https://api.github.com/repos/"+GitHubExchange.Repository+"/issues/7";
    private const string Gist="0123456789abcdef0123",Revision="abababababababababababababababababababab";

    private sealed class Server : HttpMessageHandler
    {
        public byte[]? Merged;
        public readonly List<(string Method,string Address)> Asked=[];
        public int Count(string method,string address){lock(Asked)return Asked.Count(a=>a.Method==method && a.Address==address);}
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();var method=request.Method.Method;
            lock(Asked)Asked.Add((method,address));
            if(method=="GET" && address==GistRotation.NotesAddress)
                return Task.FromResult(Merged is null ? new HttpResponseMessage(HttpStatusCode.NotFound) : new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(Merged)});
            if(method=="POST" && address=="https://api.github.com/gists")return Task.FromResult(Json(new JsonObject{["id"]="g2",["history"]=new JsonArray(new JsonObject{["version"]="r2"})}));
            if(method=="PATCH" && address.StartsWith("https://api.github.com/gists/",StringComparison.Ordinal))
                return Task.FromResult(Json(new JsonObject{["id"]=Gist,["history"]=new JsonArray(new JsonObject{["version"]=Revision})}));   // the gist's revision the tool reads and lists
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

    // The trusted merge's own replay of a sync (tools/discoveries-trust.py replay): a share merged at an earlier revision, then the same
    // share's gist read again at a new revision with nothing new. Prints what changed, and writes the list of merged gists before and after.
    private const string Maker="""
import importlib.util,os,sys
spec=importlib.util.spec_from_file_location("discoveries_trust",sys.argv[1]);t=importlib.util.module_from_spec(spec);spec.loader.exec_module(t)
m=t.tool();out=sys.argv[2];gist,revision=sys.argv[3],sys.argv[4]
raw={"Kind":1,"Subject":2,"Detail":0,"Territory":128,"Map":1,"CellX":1,"CellZ":2,"X":60.0,"Z":140.0,"LevelMin":1,"LevelMax":1,"Seen":2,"Imported":0,
     "First":"2026-11-10T00:00:00+00:00","Last":"2026-11-12T00:00:00+00:00"}
body=f"Shared from Tensho Assistant 0.1832.0\n\nGist: https://gist.github.com/{gist}\n\n- [x] I share these discoveries under CC0-1.0 (public domain).\n"
issue={"number":7,"title":"Discoveries: tester","body":body,"author":{"login":"tester"},"labels":[]}
links={"7":{"File":"0123456789abcdef","Login":"tester"}}
reader=lambda files:{"Version":2,"Facts":[dict(raw)]}
old,_=t.replay({},[issue],links,lambda g:("tester",{"id":g},"r0"),reader)
new,read=t.replay(old,[issue],links,lambda g:("tester",{"id":g},revision),reader)
changed=sorted(p for p in new if new[p]!=old.get(p))
open(os.path.join(out,"old.txt"),"wb").write(old[t.MERGED_FILE]);open(os.path.join(out,"new.txt"),"wb").write(new[t.MERGED_FILE])
print("new:",read[0]["new"],"changed:",",".join(changed))
""";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Note only 1828: wiring: a gist with nothing new is listed as merged, and both selftests hold its cases",() =>
        {
            var trust=Read("tools/discoveries-trust.py");
            Check(trust.Contains("A batch whose gists bring nothing new changes merged.txt only",StringComparison.Ordinal),"said in the trusted merge's help");
            Check(trust.Contains("no « Merged » comment on the ticket (as sync does)",StringComparison.Ordinal),"no « merged » notice for a gist with nothing new");
            foreach(var name in new[]{"X-825: the replay of a gist with nothing new changes the merged list only","X-825: nothing new, the merged list alone: merged, nothing commented",
                "X-825: the merged list and a fact changed: a doubt"})
                Check(trust.Contains(name,StringComparison.Ordinal),"trust selftest case: "+name);
            var tool=Read("tools/discoveries-merge.py");
            Check(tool.Contains("merged.add(note_hash(gist,revision))",StringComparison.Ordinal) && tool.Contains("if (added,gone)==(0,0):say(f\"#{issue['number']}: its gist noted as merged\");continue",StringComparison.Ordinal),
                "sync lists the revision read, nothing more for a gist with nothing new");
            Check(tool.Contains("Ann's gist with nothing new listed as merged",StringComparison.Ordinal),"merge selftest case: a gist with nothing new listed as merged");
        }),
        ("Note only 1828: end to end of X-804: the list before deletes nothing, the list the merge tool writes for a gist with nothing new makes the plugin delete the gist",() =>
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
                Check(run.ExitCode==0 && output.Result.Contains("new: False changed: community-discoveries/merged.txt",StringComparison.Ordinal),
                    "the replay of a gist with nothing new changes the list of merged gists only ("+run.ExitCode+"): "+output.Result+error.Result);
                var old=File.ReadAllBytes(Path.Combine(tool,"old.txt"));var noted=File.ReadAllBytes(Path.Combine(tool,"new.txt"));
                Check(GistRotation.Note(old,Gist,Revision) is null && GistRotation.Note(noted,Gist,Revision)==(Gist,Revision),"the plugin finds its revision in the new list only");

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
                var server=new Server{Merged=old};
                using(var exchange=new GitHubExchange(profile,server))Exchange(exchange);
                Check(server.Count("DELETE","https://api.github.com/gists/"+Gist)==0,"the list before (another revision listed): the gist kept");
                server.Merged=noted;
                using(var exchange=new GitHubExchange(profile,server)){var said=Exchange(exchange);Check(said.Args[3]=="ShareMerged","« everything is in the common base »: "+said.Args[3]+" ("+string.Join("; ",server.Asked.Select(a=>a.Method+" "+a.Address[Math.Max(0,a.Address.Length-40)..]))+")");}
                Check(server.Count("DELETE","https://api.github.com/gists/"+Gist)==1 && server.Count("POST","https://api.github.com/gists")==0,
                    "its revision listed on main: the gist deleted (its exact places and days no longer public), no new gist");
                var meta=JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(profile,"github.json")))!;
                Check(meta.GistId=="" && meta.Issue==7,"no gist left, the issue kept for the next one");
            }
            finally { try { Directory.Delete(root,true); } catch(IOException){} catch(UnauthorizedAccessException){} }
        }),
    ];
}
