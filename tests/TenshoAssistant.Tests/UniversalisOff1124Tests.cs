using System.Net;
using TenshoAssistant.Market;

// 0.1124 (X-143 part 2, audit A07): a Universalis request waiting for its slot (the 3 s pause) does not leave once the player turned
// Universalis off: the setting is read again right before each send (market board, list prices and their sale speed). Behaviour test
// (R28): a counting fake server, a second request waiting for its slot, the setting turned off during the wait.
internal static class UniversalisOff1124Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private sealed class Counting : HttpMessageHandler
    {
        public int Sent;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Interlocked.Increment(ref Sent);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{}")});
        }
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Universalis 1124: turned off while a request waits for its slot: nothing leaves, the board says the source is off",() =>
        {
            var server=new Counting();
            var on=true;
            using var board=new MarketBoardService(new HttpClient(server)){Enabled=()=>Volatile.Read(ref on)};
            board.Request("Chaos",5057);
            Check(SpinWait.SpinUntil(()=>Volatile.Read(ref server.Sent)==1 && !board.State.Loading,5000),"the first request leaves: "+server.Sent);
            board.Request("Chaos",5058);
            Check(SpinWait.SpinUntil(()=>board.State is { ItemId: 5058, Cooldown: true },2000),"the second waits for its slot");
            Volatile.Write(ref on,false);
            Check(SpinWait.SpinUntil(()=>board.State is { ItemId: 5058, Loading: false },6000),"the wait ends: "+board.State);
            Check(Volatile.Read(ref server.Sent)==1,"turned off during the wait: nothing sent (the old code sent it), sent "+server.Sent);
            Check(board.State.Error is not null,"the board says why (source off): "+board.State.Error);
        }),
        ("Universalis 1124: wiring (list prices read the setting at each turn and before the sale speed)",() =>
        {
            var market=Read("src/TenshoAssistant/Market/MarketService.cs");
            Check(market.Contains("        while(true)\n        {\n            // X-143 part 2 (0.1124, audit A07)") && market.Contains("// X-143 part 2 (0.1124, audit A07): the second request (sale speed) asks the setting again too.\n        if(Enabled?.Invoke()==false)return null;"),"list prices and speed ask again");
            var board=Read("src/TenshoAssistant/Market/MarketBoardService.cs");
            var after=board.IndexOf("if(wait>TimeSpan.Zero)await Task.Delay(wait,token).ConfigureAwait(false);",StringComparison.Ordinal);
            Check(after>0 && board.IndexOf("if(Enabled?.Invoke()==false){Publish(scope,itemId,known,SourceOff,known is not null);return;}",after,StringComparison.Ordinal)>after,"the board asks again after its pause");
        }),
    ];
}
