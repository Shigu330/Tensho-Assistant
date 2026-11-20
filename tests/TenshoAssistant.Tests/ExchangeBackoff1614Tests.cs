using System.Net.Http;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.1614 (X-666 M4, review of 0.1612, R04): « at most once every 20 hours » held only for an exchange that ended; one that failed was tried
// again every 15 minutes, so GitHub was asked every 15 minutes for as long as it failed (expired token, refusal, network). Now the wait doubles
// after each failure in a row (15 min, 30 min, 1 h … 20 h at most), kept in github.json across restarts, and cleared by an exchange that ends.
internal static class ExchangeBackoff1614Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,15,0,0,TimeSpan.Zero);

    private sealed class Broken : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            throw new InvalidOperationException("no network in this test");
        }
    }

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-backoff-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}finally{try{Directory.Delete(root,true);}catch(IOException){}}
    }

    private static GitHubExchangeMeta Meta(string root)=>JsonSerializer.Deserialize<GitHubExchangeMeta>(File.ReadAllText(Path.Combine(root,"github.json")))!;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Exchange backoff 1614: the wait doubles after each failure in a row, 15 minutes to 20 hours at most",()=>
        {
            var waits=Enumerable.Range(0,10).Select(GitHubExchange.Wait).ToArray();
            Check(waits[0]==TimeSpan.FromMinutes(15) && waits[1]==TimeSpan.FromMinutes(15) && waits[2]==TimeSpan.FromMinutes(30) && waits[3]==TimeSpan.FromHours(1)
                && waits[5]==TimeSpan.FromHours(4) && waits[7]==TimeSpan.FromHours(16) && waits[8]==TimeSpan.FromHours(20) && waits[9]==TimeSpan.FromHours(20),string.Join(", ",waits));
            Check(GitHubExchange.Wait(1000)==GitHubExchange.Every && GitHubExchange.Wait(-3)==GitHubExchange.Retry,"bounded");
            // Over a day of failures, the automatic exchange asks GitHub 8 times, no longer 96.
            var tries=0;for(var spent=TimeSpan.Zero;spent<TimeSpan.FromDays(1);tries++)spent+=GitHubExchange.Wait(tries+1);
            Check(tries<=8,"tries in a day: "+tries);
        }),
        ("Exchange backoff 1614: failed exchanges are counted and kept on disk; a restart waits as long, the clock set back does not hold it",()=>InTemp(root=>
        {
            var now=At;var broken=new Broken();
            using(var exchange=new GitHubExchange(root,broken,GitHubSignIn.For("Iv1.test"),()=>now))
            {
                for(var failure=1;failure<=3;failure++)
                {
                    Check(exchange.Due(now),"due before failure "+failure);
                    Check(exchange.Exchange(new DiscoveryStore(),false),"started");
                    for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
                    Check(!exchange.Busy && exchange.Message?.Key=="ExchangeFailed","failed: "+exchange.Message?.Key);
                    Check(Meta(root) is { Failures: var count, Tried: { } at } && count==failure && at==now,"kept: "+Meta(root).Failures);
                    var wait1=GitHubExchange.Wait(failure);
                    Check(!exchange.Due(now+wait1-TimeSpan.FromMinutes(1)) && exchange.Due(now+wait1),"waits "+wait1+" after failure "+failure);
                    now+=wait1;
                }
            }
            Check(broken.Calls>=3,"GitHub asked once per attempt");
            var tried=now-GitHubExchange.Wait(3);
            using var restarted=new GitHubExchange(root,broken,GitHubSignIn.For("Iv1.test"),()=>now);
            Check(!restarted.Due(tried+TimeSpan.FromMinutes(59)) && restarted.Due(tried+TimeSpan.FromHours(1)),"the restart keeps the wait (third failure: 1 h)");
            Check(restarted.Due(tried-TimeSpan.FromDays(1)),"a last attempt dated after now does not hold the exchange");
        })),
        ("Exchange backoff 1614: an exchange that ends clears the count",()=>InTemp(root=>
        {
            File.WriteAllText(Path.Combine(root,"github.json"),JsonSerializer.Serialize(new GitHubExchangeMeta{Failures=6,Tried=At}));
            using var exchange=new GitHubExchange(root,new Broken(),GitHubSignIn.For("Iv1.test"),()=>At);
            Check(!exchange.Due(At+TimeSpan.FromHours(7)) && exchange.Due(At+TimeSpan.FromHours(8)),"six failures: 8 h");
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs"));
            Check(code.Contains("meta.Failures=result.Key==\"ExchangeDone\" ? 0:Math.Min(meta.Failures+1,30);") && code.Contains("Start(()=>ExchangeAsync(store,upload),exchange:true)"),"cleared by « ExchangeDone », exchanges only");
        })),
        ("Exchange backoff 1614: the setting's tip and SECURITY say the wait after a failure, in both languages",()=>
        {
            var en=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            var fr=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            var security=File.ReadAllText(Path.GetFullPath("SECURITY.md"));
            Check(en.Contains("After a failed exchange, it tries again 15 minutes later, then waits twice as long after each new failure (20 hours at most).")
                && fr.Contains("Après un échange qui a échoué, il réessaie 15 minutes plus tard, puis attend deux fois plus longtemps après chaque nouvel échec (20 heures au plus)."),"tip");
            Check(security.Contains("after a failed exchange, tried again 15 minutes later, then twice as long after each new failure") && security.Contains("après un échange qui a échoué, réessayé 15 minutes plus tard"),"SECURITY");
        }),
    ];
}
