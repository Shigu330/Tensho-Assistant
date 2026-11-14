using System.IO.Compression;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenshoAssistant.Discoveries;

// 0.1137 (X-174, DEC-052, user choice of 12/11 « option 3 »): the exchange format v3 of the shared discoveries: index.json and one kind-NN.jsonl.gz per
// kind, read as a stream and bounded, checked against the index's hash; the plugin downloads the kind files that changed and notes each hash only once a
// save holds it (X-166); it uploads a v3 gist from Mine (X-167) and removes the old v2 file. X-357 (0.1349): the v2 common file is no longer read.
// Behaviour tests (R28): real gzip bytes, a fake GitHub answering the exchange, saves simulated with SnapshotToSave and MarkSaved.
internal static class ExchangeV31137Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,10,0,0,0,TimeSpan.Zero);
    private static readonly Vector3 Place=new(120,0,-40);

    private static DiscoveryFact Fish(uint subject,Vector3? at=null)
    {
        var place=at ?? Place;var cell=DiscoveryStore.Cell(place.X,place.Z);
        return new(){Kind=DiscoveryKind.FishCatch,Subject=subject,Detail=1,Territory=135,Map=16,CellX=cell.X,CellZ=cell.Z,X=place.X,Z=place.Z,
            LevelMin=1,LevelMax=1,Imported=1,First=At.AddDays(-3),Last=At.AddDays(-2)};
    }
    private static DiscoveryFact Venture(uint subject)=>new(){Kind=DiscoveryKind.Venture,Subject=subject,Detail=7,LevelMin=1,LevelMax=1,Imported=1,First=At.AddDays(-3),Last=At.AddDays(-1)};
    private static DiscoveryFile Common(params DiscoveryFact[] facts)=>new(){Producer="Tensho Assistant community",Written=At,Facts=[..facts]};

    private static string Text(byte[] gzip)
    {
        using var reader=new StreamReader(new GZipStream(new MemoryStream(gzip),CompressionMode.Decompress));
        return reader.ReadToEnd();
    }
    private static (byte[] Bytes,ExchangeV3Entry Entry) KindFile(DiscoveryKind kind,int count,params string[] lines)
    {
        using var memory=new MemoryStream();
        using(var gzip=new GZipStream(memory,CompressionLevel.Optimal,true))gzip.Write(Encoding.UTF8.GetBytes(string.Concat(lines.Select(line=>line+"\n"))));
        var bytes=memory.ToArray();
        return (bytes,new(){Name=ExchangeV3.FileName(kind),Kind=kind,Count=count,Bytes=bytes.Length,Sha256=ExchangeV3.Sha256(bytes)});
    }
    private static string Header(DiscoveryKind kind,int count,int version=3)=>
        $"{{\"Format\":\"tensho-discoveries\",\"Version\":{version},\"Kind\":{(int)kind},\"Count\":{count},\"Producer\":\"t\",\"Written\":\"2026-11-10T00:00:00+00:00\"}}";
    private static string[] FactLines(int count)=>
        Text(ExchangeV3.WriteKind(DiscoveryKind.Venture,Enumerable.Range(1,count).Select(subject=>Venture((uint)subject)).ToList(),"t")).Split('\n',StringSplitOptions.RemoveEmptyEntries)[1..];
    private static bool Refused(Action read)
    {
        try{read();return false;}
        catch(InvalidDataException){return true;}
    }

    /// <summary>A stream that never ends: how much was asked of it is counted.</summary>
    private sealed class Endless : Stream
    {
        public long Given;
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
        public override long Length=>throw new NotSupportedException();public override long Position{get=>Given;set=>throw new NotSupportedException();}
        public override void Flush(){}
        public override int Read(byte[] buffer,int offset,int count){Array.Fill(buffer,(byte)0x1f,offset,count);Given+=count;return count;}
        public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
        public override void SetLength(long value)=>throw new NotSupportedException();
        public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
    }

    /// <summary>A fake GitHub: raw files by address (404 otherwise); the API calls recorded and answered « {} ».</summary>
    private sealed class Server : HttpMessageHandler
    {
        public readonly Dictionary<string,byte[]> Files=[];
        public readonly List<string> Asked=[];
        public readonly List<(string Method,string Address,string Body)> Calls=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();
            lock(Asked)Asked.Add(address);
            if(request.Method!=HttpMethod.Get)
            {
                var body=request.Content is null ? "":await request.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                lock(Calls)Calls.Add((request.Method.Method,address,body));
                return new(HttpStatusCode.OK){Content=new StringContent("{}")};
            }
            return Files.TryGetValue(address,out var bytes) ? new(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)}:new(HttpStatusCode.NotFound);
        }
        public void Serve(ExchangeV3Set set)
        {
            Files[CommunityDiscoveries.AddressV3+ExchangeV3.IndexName]=Encoding.UTF8.GetBytes(set.Index);
            foreach(var (name,bytes) in set.Files)Files[CommunityDiscoveries.AddressV3+name]=bytes;
        }
    }

    private static ExchangeMessage Run(GitHubExchange exchange,DiscoveryStore store,bool upload)
    {
        Check(exchange.Exchange(store,upload),"the exchange starts");
        for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is { Key:"ExchangeDone" },$"the exchange ends ({exchange.Message?.Key} {string.Join(", ",exchange.Message?.Args ?? [])})");
        return exchange.Message!;
    }
    private static void Saved(DiscoveryStore store){store.SnapshotToSave(At,out var stamp);store.MarkSaved(stamp);}
    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-v3-1137-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Exchange v3 1137: a set is written as real gzip bytes and read back; the same facts give the same bytes; a line never holds Own, Key or imported counts",() =>
        {
            var store=new DiscoveryStore();
            store.Merge([Fish(4869)]);
            store.SeeSource(DiscoveryKind.FishCatch,4869,1,3,At,135,16,new Vector3(130,0,-20));
            store.SeeSource(DiscoveryKind.Venture,5,7,2,At);
            var mine=CommunityDiscoveries.Mine(store.Snapshot(At,false));
            var set=ExchangeV3.Build(mine);
            Check(set.Index==ExchangeV3.Build(CommunityDiscoveries.Mine(store.Snapshot(At.AddHours(3),false))).Index,"same facts, same index (each file's hash in it)");
            var index=ExchangeV3.ParseIndex(set.Index)!;
            Check(index.Files.Select(entry=>entry.Name).SequenceEqual(["kind-05.jsonl.gz","kind-10.jsonl.gz"]),"one file per kind, in kind order");
            var read=0;
            foreach(var entry in index.Files)
            {
                var bytes=set.Files[entry.Name];
                Check(bytes[0]==0x1f && bytes[1]==0x8b && bytes.Length==entry.Bytes,"gzip bytes, the size the index gives");
                var text=Text(bytes);
                Check(text.Split('\n')[0].Contains("\"Version\":3") && !text.Contains("\"Own\"") && !text.Contains("\"Key\"") && !text.Contains("ImportedHq") && !text.Contains("ImportedValues"),
                    "a header line, and no Own, Key or imported counts in the lines");
                var kind=ExchangeV3.ReadKind(new MemoryStream(bytes),entry);
                Check(kind.Refused==0 && kind.Facts.All(fact=>fact.Kind==entry.Kind && fact.Imported==0 && fact.Own is null && fact.ImportedValues is null),"read back, yours only");
                read+=kind.Facts.Count;
            }
            Check(read==mine.Facts.Count && read==2,"every fact read back");
        }),
        ("Exchange v3 1137: a file of another hash or larger than its index says is refused, without reading it whole; an index past the bounds is refused",() =>
        {
            var set=ExchangeV3.Build(Common(Venture(1),Venture(2),Fish(3)));
            var entry=ExchangeV3.ParseIndex(set.Index)!.Files[0];var bytes=set.Files[entry.Name];
            var other=new ExchangeV3Entry{Name=entry.Name,Kind=entry.Kind,Count=entry.Count,Bytes=entry.Bytes,Sha256=new string('0',64)};
            Check(Refused(()=>ExchangeV3.ReadKind(new MemoryStream(bytes),other)),"a hash that differs is refused");
            var endless=new Endless();
            Check(Refused(()=>ExchangeV3.ReadKind(endless,new(){Name=entry.Name,Kind=entry.Kind,Count=1,Bytes=4096,Sha256=entry.Sha256})),"larger than the index says: refused");
            Check(endless.Given<=4096+81920,$"stopped at the bound, not read whole ({endless.Given} bytes asked)");
            Check(Refused(()=>ExchangeV3.ReadKind(new MemoryStream(bytes),entry,maxDecompressed:64)),"past the decompressed bound: refused");
            JsonObject Index(Action<JsonObject> change)
            {
                var node=JsonNode.Parse(set.Index)!.AsObject();change(node);return node;
            }
            Check(ExchangeV3.ParseIndex(Index(node=>node["Files"]![0]!["Bytes"]=ExchangeV3.MaxCompressedBytes+1).ToJsonString()) is null,"an entry past 32 MB refuses the index");
            Check(ExchangeV3.ParseIndex(Index(node=>node["Files"]![0]!["Name"]="../kind-05.jsonl.gz").ToJsonString()) is null,"a name that is not its kind's refuses the index");
            Check(ExchangeV3.ParseIndex(Index(node=>node["Version"]=4).ToJsonString()) is null,"an unknown version refuses the index");
            Check(ExchangeV3.ParseIndex(Index(node=>node["Files"]![0]!["Sha256"]="xyz").ToJsonString()) is null,"a malformed hash refuses the index");
            var unknown=Index(node=>node["Files"]!.AsArray().Add(new JsonObject{["Name"]="kind-99.jsonl.gz",["Kind"]=99,["Count"]=1,["Bytes"]=10,["Sha256"]=new string('A',64)}));
            Check(ExchangeV3.ParseIndex(unknown.ToJsonString()) is { Files.Count:2 },"a kind this version does not know is left out, the others kept");
        }),
        ("Exchange v3 1137: a bad line is refused and counted, past 5 % the file is refused; a header of another kind or an unknown version is refused",() =>
        {
            var good=FactLines(40);
            var (bytes,entry)=KindFile(DiscoveryKind.Venture,41,[Header(DiscoveryKind.Venture,41),..good,"{not json"]);
            var read=ExchangeV3.ReadKind(new MemoryStream(bytes),entry);
            Check(read.Facts.Count==40 && read.Refused==1,$"a bad line counted, the file kept ({read.Facts.Count}, {read.Refused})");
            (bytes,entry)=KindFile(DiscoveryKind.Venture,41,[Header(DiscoveryKind.Venture,41),..good,new string('x',300)]);
            read=ExchangeV3.ReadKind(new MemoryStream(bytes),entry,maxLine:good.Max(line=>line.Length)+10);
            Check(read.Facts.Count==40 && read.Refused==1,"a line too long counted, not read whole");
            var withOwn=good[0].Replace("\"Kind\":5","\"Kind\":5,\"Own\":{\"LevelMin\":1}");var withKey=good[1].Replace("\"Kind\":5","\"Kind\":5,\"Key\":\"5:2\"");
            var unknown=good[2].Replace("\"Kind\":5","\"Kind\":5,\"Name\":\"someone\"");var otherKind=good[3].Replace("\"Kind\":5","\"Kind\":4");
            (bytes,entry)=KindFile(DiscoveryKind.Venture,80,[Header(DiscoveryKind.Venture,80),..FactLines(76),withOwn,withKey,unknown,otherKind]);
            read=ExchangeV3.ReadKind(new MemoryStream(bytes),entry);
            Check(read.Refused==3 && read.Facts.Count==77,$"Own, Key or another kind: each line refused; an unknown name ignored since 0.1170 (X-186) ({read.Refused})");
            (bytes,entry)=KindFile(DiscoveryKind.Venture,3,[Header(DiscoveryKind.Venture,3),good[0],"{bad","{bad"]);
            Check(Refused(()=>ExchangeV3.ReadKind(new MemoryStream(bytes),entry)),"over 5 % refused: the file refused");
            (bytes,entry)=KindFile(DiscoveryKind.Venture,1,[Header(DiscoveryKind.FishCatch,1),good[0]]);
            Check(Refused(()=>ExchangeV3.ReadKind(new MemoryStream(bytes),entry)),"a header of another kind than its index: refused");
            (bytes,entry)=KindFile(DiscoveryKind.Venture,1,[Header(DiscoveryKind.Venture,1,version:4),good[0]]);
            Check(Refused(()=>ExchangeV3.ReadKind(new MemoryStream(bytes),entry)),"an unknown version: refused");
            (bytes,entry)=KindFile(DiscoveryKind.Venture,2,[Header(DiscoveryKind.Venture,2),good[0]]);
            Check(Refused(()=>ExchangeV3.ReadKind(new MemoryStream(bytes),entry)),"fewer facts than the header says: refused");
        }),
        ("Exchange v3 1137: the plugin downloads the kind files that changed and notes each hash only after the save; without the v3 set nothing is taken and the base is untouched",() => InTemp(root =>
        {
            using var server=new Server();
            server.Serve(ExchangeV3.Build(Common(Fish(1),Fish(2),Venture(5))));
            var folder=Path.Combine(root,"a");var marker=Path.Combine(folder,"community-v3.json");
            using var exchange=new GitHubExchange(folder,server);
            var store=new DiscoveryStore();
            var message=Run(exchange,store,false);
            Check(message.Args[2]=="CommonTaken" && message.Args[0]=="3","the v3 set taken: 3 facts added");
            Check(store.Find(DiscoveryKind.FishCatch,2u,5).Single() is { Seen:0, Imported:1 } && store.Find(DiscoveryKind.Venture,5u,5).Count==1,"the facts are in the base, imported");
            Check(!File.Exists(marker),"no hash noted before a save holds the facts");
            server.Asked.Clear();
            message=Run(exchange,store,false);
            Check(message.Args[2]=="CommonSame" && server.Asked.Count(address=>address.EndsWith(".jsonl.gz",StringComparison.Ordinal))==0,"waiting for the save: not merged twice");
            Saved(store);
            var noted=JsonSerializer.Deserialize<Dictionary<int,string>>(File.ReadAllText(marker))!;
            Check(noted.Keys.Order().SequenceEqual([5,10]),"each kind's hash noted once the save holds it");
            // One kind changes: only its file is downloaded again.
            server.Serve(ExchangeV3.Build(Common(Fish(1),Fish(2),Fish(3),Venture(5))));
            server.Asked.Clear();
            message=Run(exchange,store,false);
            Check(message.Args[2]=="CommonTaken" && server.Asked.Where(address=>address.EndsWith(".jsonl.gz",StringComparison.Ordinal)).SequenceEqual([CommunityDiscoveries.AddressV3+"kind-10.jsonl.gz"]),
                "only the kind that changed is downloaded");
            Check(store.Find(DiscoveryKind.FishCatch,3u,5).Count==1,"its new fact taken");
            // A kind file whose bytes do not match the index is not merged and not noted.
            using var bad=new Server();
            var set=ExchangeV3.Build(Common(Fish(7)));bad.Serve(set);bad.Files[CommunityDiscoveries.AddressV3+"kind-10.jsonl.gz"]=set.Files["kind-10.jsonl.gz"].Select(value=>(byte)(value^0xff)).ToArray();
            using var refused=new GitHubExchange(Path.Combine(root,"b"),bad);var other=new DiscoveryStore();
            Check(Run(refused,other,false).Args[2]=="CommonUnreadable" && other.Find(DiscoveryKind.FishCatch,7u,5).Count==0,"a file that does not match its hash: refused, nothing merged");
            Saved(other);
            Check(!File.Exists(Path.Combine(root,"b","community-v3.json")),"and its hash not noted");
            // X-357 (0.1349, R28.14): no v3 index (404), or one this version does not read: nothing taken, « unreadable », the local base untouched;
            // the old v2 file, still served here, is never asked for.
            using var old=new Server();
            const string V2="https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/community-discoveries/discoveries.json";
            old.Files[V2]=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Common(Fish(9))));
            using var v2=new GitHubExchange(Path.Combine(root,"c"),old);var third=new DiscoveryStore();
            third.Merge([Venture(4),Fish(8)],common:true);Saved(third);
            var before=JsonSerializer.Serialize(third.Snapshot(At,false));
            Check(Run(v2,third,false).Args[2]=="CommonUnreadable" && third.Find(DiscoveryKind.FishCatch,9u,5).Count==0,"no v3 set: nothing taken, said unreadable");
            Check(old.Asked.SequenceEqual([CommunityDiscoveries.AddressV3+ExchangeV3.IndexName]),"only the v3 index asked, never the v2 file");
            Saved(third);
            Check(JsonSerializer.Serialize(third.Snapshot(At,false))==before && third.Find(DiscoveryKind.FishCatch,8u,5).Count==1 && third.Find(DiscoveryKind.Venture,4u,5).Count==1,
                "the local base untouched: the facts it held are all there, nothing added");
            Check(!File.Exists(Path.Combine(root,"c","community-v3.json")) && !File.Exists(Path.Combine(root,"c","community.sha256")),"no hash noted");
            old.Files[CommunityDiscoveries.AddressV3+ExchangeV3.IndexName]=Encoding.UTF8.GetBytes("{\"Format\":\"tensho-discoveries\",\"Version\":4,\"Files\":[]}");
            using var newer=new GitHubExchange(Path.Combine(root,"d"),old);var fourth=new DiscoveryStore();
            Check(Run(newer,fourth,false).Args[2]=="CommonUnreadable" && fourth.Find(DiscoveryKind.FishCatch,9u,5).Count==0 && !old.Asked.Contains(V2),
                "an index of an unknown version: nothing taken, the v2 file not read");
        })),
        ("Exchange v3 1137: the upload is a v3 gist built from Mine (never Own nor imported counts), the old v2 file removed; the same facts send nothing",() => InTemp(root =>
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Login="tester",GistId="g1",Issue=7,ClientId=GitHubSignIn.Current.ClientId}));
            // 0.1161: a token of the sign-in in use (the GitHub App since X-175); one of the former OAuth app is forgotten (GitHubApp1138Tests).
            File.WriteAllBytes(Path.Combine(root,"github.token"),UserSecret.Protect(Encoding.UTF8.GetBytes("token")));
            using var server=new Server();
            using var exchange=new GitHubExchange(root,server);
            Check(exchange.Connected,"connected");
            var store=new DiscoveryStore();
            var theirs=new Vector3(110,0,-30);var mine=new Vector3(130,0,-20);
            var imported=Fish(4869,theirs);imported.Seen=2;imported.Imported=0;imported.LevelMin=40;imported.LevelMax=90;imported.Values=new(){[40]=1,[90]=1};
            store.Merge([imported]);
            store.SeeSource(DiscoveryKind.FishCatch,4869,1,3,At,135,16,mine);
            store.SeeSource(DiscoveryKind.Venture,5,7,2,At);
            store.Merge([Venture(6)]);
            var message=Run(exchange,store,true);
            Check(message.Args[2]=="CommonUnreadable" && message.Args[3]=="Sent","sent (no common set served: unreadable, X-357)");
            var patch=server.Calls.Single();
            Check(patch.Method=="PATCH" && patch.Address=="https://api.github.com/gists/g1","the gist updated");
            var files=JsonNode.Parse(patch.Body)!["files"]!.AsObject();
            Check(files.ContainsKey(GitHubExchange.GistFile) && files[GitHubExchange.GistFile] is null,"the old v2 file removed (set to null)");
            Check(files.Select(pair=>pair.Key).Order(StringComparer.Ordinal).SequenceEqual(["index.json","kind-05.jsonl.gz.b64","kind-10.jsonl.gz.b64",GitHubExchange.GistFile]),"index.json and one file per kind");
            var index=ExchangeV3.ParseIndex(files["index.json"]!["content"]!.GetValue<string>())!;
            var sent=new List<DiscoveryFact>();
            foreach(var entry in index.Files)
            {
                var bytes=Convert.FromBase64String(files[entry.Name+ExchangeV3.GistSuffix]!["content"]!.GetValue<string>());
                var text=Text(bytes);
                Check(!text.Contains("\"Own\"") && !text.Contains("Imported\":1") && !text.Contains("ImportedHq") && !text.Contains("ImportedValues"),"no Own nor imported counts sent");
                sent.AddRange(ExchangeV3.ReadKind(new MemoryStream(bytes),entry).Facts);
            }
            Check(sent.Count==2 && sent.All(fact=>fact.Imported==0) && sent.All(fact=>fact.Subject!=6),"your sightings only, not the imported venture");
            var fish=sent.Single(fact=>fact.Kind==DiscoveryKind.FishCatch);
            Check(fish.X==mine.X && fish.LevelMin==3 && fish.LevelMax==3 && fish.First==CommunityDiscoveries.Day(At) && fish.Values is { Count:1 },"your place, value and day (X-167), not the import's");
            Check(Run(exchange,store,true).Args[3]=="SentSame" && server.Calls.Count==1,"the same facts: nothing sent");
            store.SeeSource(DiscoveryKind.Venture,8,7,2,At);
            Check(Run(exchange,store,true).Args[3]=="Sent" && server.Calls.Count==2,"a new sighting: sent");
            var second=JsonNode.Parse(server.Calls[1].Body)!["files"]!.AsObject();
            Check(!second.ContainsKey(GitHubExchange.GistFile),"the v2 file not asked for again once removed");
            Check(JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(root,"github.json")))!.GistFiles.Count==3,"the gist's files remembered");
        })),
        ("Exchange v3 1137: the v3 set the merge tool wrote in the repository reads in the plugin, each file whole; the v2 file is gone",() =>
        {
            var folder=Path.GetFullPath("community-discoveries/v3");
            var index=ExchangeV3.ParseIndex(File.ReadAllText(Path.Combine(folder,"index.json")));
            Check(index is { Files.Count:>0 },"the repository's index reads");
            var keys=new HashSet<string>();
            foreach(var entry in index!.Files)
            {
                using var stream=File.OpenRead(Path.Combine(folder,entry.Name));
                var kind=ExchangeV3.ReadKind(stream,entry);
                Check(kind.Refused==0 && kind.Facts.Count==entry.Count && kind.Facts.All(fact=>fact.Seen==0 && fact.Imported>0),entry.Name+" reads whole");
                foreach(var fact in kind.Facts)keys.Add(fact.Key);
            }
            // X-357 (0.1349): the v2 discoveries.json is gone (it held the same 3007 facts, checked by the relay before its removal).
            Check(keys.Count==index!.Files.Sum(entry=>entry.Count) && !File.Exists(Path.GetFullPath("community-discoveries/discoveries.json")),$"one fact per line, no double ({keys.Count}); the v2 file gone");
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("def v3_kind(") && tool.Contains("def from_gist_files(") && tool.Contains("return write_v3(common[\"Facts\"],V3_DIR,say)")
                && !tool.Contains("COMMON=") && !tool.Contains("write_json(COMMON"),"the merge tool reads v3 gists and writes the v3 set only");
            var exchange=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs"));
            Check(exchange.Contains("store.WhenImportSaved(key,()=>NoteCommonV3(kind,hash));") && exchange.Contains("await TakeCommonV3(store).ConfigureAwait(false) ?? (\"CommonUnreadable\",0,0);")
                && !exchange.Contains("TakeCommonV2") && !exchange.Contains("community.sha256"),"the wiring: each kind's hash noted after the save, v3 only");
        }),
    ];
}
