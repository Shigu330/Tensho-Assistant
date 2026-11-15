using System.Net;
using System.Numerics;
using System.Text;
using TenshoAssistant.Discoveries;

// X-367 M1 (0.1370, relecture 0.1349, decision D-159 choice a): since X-357 a player whose game is running but who cannot reach GitHub (no answer,
// a refusal, a request past its delay) or whose common set is not published yet read « Common file unreadable. », which says the file is damaged.
// Behaviour test (R28.14) with a fake GitHub: 404, 403, 503, no connection and a delay give « CommonUnreachable » (« try again later »); a damaged
// index or kind file still gives « CommonUnreadable »; in every case the local base stays as it was.
internal static class CommonUnreachable1370Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,13,0,0,0,TimeSpan.Zero);
    private static readonly Vector3 Place=new(120,0,-40);

    private static DiscoveryFact Fish(uint subject)
    {
        var cell=DiscoveryStore.Cell(Place.X,Place.Z);
        return new(){Kind=DiscoveryKind.FishCatch,Subject=subject,Detail=1,Territory=135,Map=16,CellX=cell.X,CellZ=cell.Z,X=Place.X,Z=Place.Z,
            LevelMin=1,LevelMax=1,Imported=1,First=At.AddDays(-3),Last=At.AddDays(-2)};
    }

    /// <summary>A fake GitHub: Answer decides each request (a status, a thrown error, or the files served).</summary>
    private sealed class Server : HttpMessageHandler
    {
        public readonly Dictionary<string,byte[]> Files=[];
        public Func<string,HttpResponseMessage?>? Answer;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var address=request.RequestUri!.ToString();
            if(Answer?.Invoke(address) is { } answer)return Task.FromResult(answer);
            return Task.FromResult(Files.TryGetValue(address,out var bytes) ? new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)}:new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private static string Common(Server server,string root,string name)
    {
        using var exchange=new GitHubExchange(Path.Combine(root,name),server);
        var store=new DiscoveryStore();store.Merge([Fish(900)],common:true);
        Check(exchange.Exchange(store,false),name+": the exchange starts");
        for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
        Check(!exchange.Busy && exchange.Message is {Key:"ExchangeDone"},$"{name}: the exchange ends done ({exchange.Message?.Key} {string.Join(", ",exchange.Message?.Args ?? [])})");
        Check(store.Find(DiscoveryKind.FishCatch,900u,5).Count==1,name+": the local base kept");
        return exchange.Message!.Args[2];
    }

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-1370-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Common unreachable 1370: behaviour: 404, 403, 503, no connection and a delay say « unreachable or not published yet », never « unreadable »",() => InTemp(root =>
        {
            var index=CommunityDiscoveries.AddressV3+ExchangeV3.IndexName;
            var cases=new (string Name,Func<string,HttpResponseMessage?> Answer)[]
            {
                ("404",_=>new HttpResponseMessage(HttpStatusCode.NotFound)),
                ("403",_=>new HttpResponseMessage(HttpStatusCode.Forbidden)),
                ("503",_=>new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
                ("no connection",_=>throw new HttpRequestException("No such host is known (test).")),
                ("delay",_=>throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout (test).",new TimeoutException())),
            };
            foreach(var (name,answer) in cases)
            {
                var server=new Server{Answer=answer};
                var said=Common(server,root,name.Replace(' ','-'));
                Check(said=="CommonUnreachable",$"{name}: {said}");
            }
            // A kind file that cannot be fetched while the index can: the same message (nothing taken, nothing damaged).
            var set=ExchangeV3.Build(new DiscoveryFile{Producer="Tensho Assistant community",Written=At,Facts=[Fish(7)]});
            var kind=new Server();kind.Files[index]=Encoding.UTF8.GetBytes(set.Index);
            kind.Answer=address=>address==index ? null:new HttpResponseMessage(HttpStatusCode.BadGateway);
            Check(Common(kind,root,"kind-502")=="CommonUnreachable","a kind file refused (502): unreachable");
        })),
        ("Common unreachable 1370: behaviour: a damaged index or kind file still says « unreadable »",() => InTemp(root =>
        {
            var index=CommunityDiscoveries.AddressV3+ExchangeV3.IndexName;
            var garbled=new Server();garbled.Files[index]=Encoding.UTF8.GetBytes("{ not json");
            Check(Common(garbled,root,"json")=="CommonUnreadable","an index that is not JSON: unreadable");
            var set=ExchangeV3.Build(new DiscoveryFile{Producer="Tensho Assistant community",Written=At,Facts=[Fish(7)]});
            var damaged=new Server();damaged.Files[index]=Encoding.UTF8.GetBytes(set.Index);
            foreach(var (name,bytes) in set.Files)damaged.Files[CommunityDiscoveries.AddressV3+name]=bytes.Select(value=>(byte)(value^0xff)).ToArray();
            Check(Common(damaged,root,"hash")=="CommonUnreadable","a kind file that does not match its hash: unreadable");
        })),
        ("Common unreachable 1370: the message exists in both languages and says to try again later",() =>
        {
            var en=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            var fr=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(en.Contains("\"CommonUnreachable\": \"GitHub could not be reached or the common file is not published yet; try again later.\""),"en");
            Check(fr.Contains("\"CommonUnreachable\": \"GitHub injoignable ou fichier commun pas encore publié, réessayez plus tard.\""),"fr");
        }),
    ];
}
