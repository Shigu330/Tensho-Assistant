using System.Net;
using TenshoAssistant.Market;
using TenshoAssistant.Monsters;

// 0.1144 (X-170, GPT audit of 12/11, X-143 items). Behaviour tests (R28): a wiki request that runs out of time waits the retry delay instead of being
// asked again at once; Universalis turned off between a click and the departure of the retainer comparison or of the regions table: nothing leaves.
internal static class Timeouts1144Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private sealed class Counting(TimeSpan delay,string body) : HttpMessageHandler
    {
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            if(delay>TimeSpan.Zero)await Task.Delay(delay,cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body)};
        }
    }
    private static bool Wait(Func<bool> done,int milliseconds=10_000)
    {
        var until=Environment.TickCount64+milliseconds;
        while(Environment.TickCount64<until){if(done())return true;Thread.Sleep(20);}
        return done();
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Timeouts 1144: the blue magic spellbook request that runs out of time is said and waits a minute (it was asked again at every frame)",() =>
        {
            var handler=new Counting(TimeSpan.FromSeconds(5),"{}");
            using var index=new BlueSpellWikiIndex(()=>TimeSpan.FromDays(1),null,()=>true,new HttpClient(handler){Timeout=TimeSpan.FromMilliseconds(100)});
            index.Ensure();
            Check(Wait(()=>handler.Calls==1 && !index.Running),"the request left and ended");
            Check(index.Error is { Length:>0 },"the time-out is said: "+index.Error);
            for(var frame=0;frame<20;frame++){index.Ensure();Thread.Sleep(5);}
            Check(handler.Calls==1 && !index.Running,"no new request before the retry delay: "+handler.Calls);
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Monsters/BlueSpellMonsterIndex.cs"));
            Check(source.Contains("catch(OperationCanceledException) when(!token.IsCancellationRequested && attempt<5){await Task.Delay(1000,token)",StringComparison.Ordinal),"spell pages: a time-out waits the retry delay");
        }),
        ("Timeouts 1144: Universalis turned off between the click and the departure: the retainer comparison sends nothing and stops loading",() =>
        {
            var handler=new Counting(TimeSpan.Zero,"{}");
            var asked=0;
            using var service=new RetainerCompareService(new HttpClient(handler)){Enabled=()=>Interlocked.Increment(ref asked)==1};
            Check(service.Request("Ragnarok",[5057u],DateTimeOffset.UtcNow),"accepted while on");
            Check(Wait(()=>!service.State.Loading),"stops loading");
            Check(handler.Calls==0 && asked>=2,"nothing left once turned off: "+handler.Calls);
        }),
        ("Timeouts 1144: Universalis turned off before the regions table leaves: nothing sent, asked again once on",() =>
        {
            var handler=new Counting(TimeSpan.Zero,"[{\"name\":\"Chaos\",\"region\":\"Europe\"}]");
            var on=0;var asked=0;
            using var board=new MarketBoardService(new HttpClient(handler)){Enabled=()=>Interlocked.Increment(ref asked)==1 || Volatile.Read(ref on)==1};
            Check(board.RegionOf("Chaos") is null,"not known yet");
            Thread.Sleep(300);
            Check(handler.Calls==0,"turned off before departure: nothing sent ("+handler.Calls+")");
            Volatile.Write(ref on,1);
            board.RegionOf("Chaos");
            Check(Wait(()=>board.RegionOf("Chaos")=="Europe"),"asked again once on, and read");
            Check(handler.Calls==1,"one request: "+handler.Calls);
        }),
    ];
}
