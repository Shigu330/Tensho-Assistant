using System.Net;
using System.Text;
using TenshoAssistant.Discoveries;

// 0.1805 (X-468 part 1, the user's choice D-216 a of 22/11, after the study of the same day; D-172 of 15/11: « a fact verified several times
// by several people counts more »): the bound of shared facts per kind goes from 200 000 to 2 000 000 (the merge tool's MAX_FACTS, the bound
// of an index entry); when a list brings more than there is room for, the plugin puts in the facts seen by the most players first, then the
// most recent, instead of the first ones in key order. The exchange times its read of the common set in the log. The merge tool's side (the
// same order, the format's byte bounds) is in its selftest (tools/discoveries-merge.py --selftest, run before each version).
internal static class DiscoveryWeight1805Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,0,0,0,TimeSpan.Zero);

    private static DiscoveryFact Fish(uint subject,int imported,int daysAgo,int seen=0)
    {
        var cell=DiscoveryStore.Cell(120,-40);
        return new(){Kind=DiscoveryKind.FishCatch,Subject=subject,Detail=1,Territory=135,Map=16,CellX=cell.X,CellZ=cell.Z,X=120,Z=-40,
            LevelMin=1,LevelMax=1,Seen=seen,Imported=imported,First=At.AddDays(-daysAgo),Last=At.AddDays(-daysAgo)};
    }

    private sealed class Server : HttpMessageHandler
    {
        public readonly Dictionary<string,byte[]> Files=[];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>
            Task.FromResult(Files.TryGetValue(request.RequestUri!.ToString(),out var bytes) ? new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)}:new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery weight 1805: the bound is 2 000 000 shared facts per kind, the merge tool's and the index's",() =>
        {
            Check(DiscoveryStore.MaxFacts==2_000_000,"the plugin's bound");
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("MAX_FACTS=2_000_000;",StringComparison.Ordinal),"the merge tool's bound");
        }),
        ("Discovery weight 1805: behaviour: over the room left, the most players first, then the most recent; under it, the same list",() =>
        {
            var list=new List<DiscoveryFact>{Fish(1,1,9),Fish(2,3,9),Fish(3,1,1),Fish(4,3,2),Fish(5,2,5),Fish(6,1,0,seen:2)};
            Check(ReferenceEquals(DiscoveryStore.Weighed(list,new Dictionary<DiscoveryKind,int>()),list),"room for all: the same list, untouched");
            var full=new Dictionary<DiscoveryKind,int>{[DiscoveryKind.FishCatch]=DiscoveryStore.MaxFacts-3};
            var order=DiscoveryStore.Weighed(list,full).Select(fact=>fact.Subject).ToArray();
            Check(order.SequenceEqual(new uint[]{4,2,5,6,3,1}),"seen by 3 (newest first), by 2, then once (newest first): "+string.Join(",",order));
            var other=new Dictionary<DiscoveryKind,int>{[DiscoveryKind.MonsterPlace]=DiscoveryStore.MaxFacts};
            Check(ReferenceEquals(DiscoveryStore.Weighed(list,other),list),"another kind full: this list untouched");
        }),
        ("Discovery weight 1805: behaviour: the exchange says in the log how long the common set took to download and to merge",() =>
        {
            var root=Path.Combine(Path.GetTempPath(),"tensho-1805-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var set=ExchangeV3.Build(new DiscoveryFile{Producer="Tensho Assistant community",Written=At,Facts=[Fish(7,2,3),Fish(8,1,4)]});
                var server=new Server();server.Files[CommunityDiscoveries.AddressV3+ExchangeV3.IndexName]=Encoding.UTF8.GetBytes(set.Index);
                foreach(var (name,bytes) in set.Files)server.Files[CommunityDiscoveries.AddressV3+name]=bytes;
                var lines=new List<string>();
                using var exchange=new GitHubExchange(Path.Combine(root,"x"),server){Log=line=>{lock(lines)lines.Add(line);}};
                var store=new DiscoveryStore();
                Check(exchange.Exchange(store,false),"the exchange starts");
                for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
                Check(!exchange.Busy && exchange.Message is {Key:"ExchangeDone"},"the exchange ends done");
                string[] said;lock(lines)said=[..lines];
                Check(said.Length==1 && said[0].StartsWith("Discoveries: common set: 1 of 1 kind file(s) taken (2 facts, ",StringComparison.Ordinal)
                    && said[0].Contains(" ms, merge ",StringComparison.Ordinal) && said[0].Contains("; 2 added, 0 joined",StringComparison.Ordinal),"one line: "+string.Join(" | ",said));
            }
            finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }),
        ("Discovery weight 1805: the log's line in English, invariant numbers, failures said",() =>
        {
            Check(GitHubExchange.CommonTimes(32,2,1234,5678,15,7,1000,234,0,0)=="Discoveries: common set: 2 of 32 kind file(s) taken (1234 facts, 5678 bytes): download 15 ms, merge 7 ms; 1000 added, 234 joined.","the line");
            Check(GitHubExchange.CommonTimes(3,1,1,1,1,1,1,0,1,1).EndsWith("; 1 unreadable, 1 not reached.",StringComparison.Ordinal),"failures said");
        }),
        ("Discovery weight 1805: wiring: the store weighs a merge and a file read, the plugin gives the exchange its log",() =>
        {
            var model=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/DiscoveriesModel.cs")).ReplaceLineEndings("\n");
            Check(model.Contains("lock(gate)shared=SharedHeld();\n        ready=Weighed(ready,shared);",StringComparison.Ordinal),"a merge weighed with the shared facts held");
            Check(model.Contains("return new(Weighed(ready,EmptyShared),refused,moved,gone,movedTo);",StringComparison.Ordinal),"a file read (base, shared kind) weighed");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs")).Contains("new Discoveries.GitHubExchange(discoveryFolder){Log=message=>log.Information(message)}",StringComparison.Ordinal),"the log given");
        }),
    ];
}
