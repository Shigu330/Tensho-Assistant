using System.Net;
using System.Net.Http;
using TenshoAssistant.Market;

// X-849 (review 0.1852, M1): a list given up between two halves (the Universalis setting turned off, the world or the character changed,
// the service released) ends without a failure, as before 0.1847: no English text shown to the player, the prices read stay; the log line
// about the answers 400 says why it stopped.
internal static class MarketAbandon1855Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(send(request));
    }

    private static HttpClient Client(Action<uint[]> asked) => new(new Handler(request =>
    {
        if (request.RequestUri?.Query.Contains("listings=0", StringComparison.Ordinal) == true)
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        var path = request.RequestUri?.AbsolutePath ?? "";
        const string prefix = "/api/v2/aggregated/";
        if (!path.StartsWith(prefix, StringComparison.Ordinal)) return new HttpResponseMessage(HttpStatusCode.NotFound);
        var ids = path[prefix.Length..].Split('/')[1].Split(',').Select(uint.Parse).ToArray();
        asked(ids);
        // The whole list is refused (400): the service goes on with its halves, and the test gives the list up before them.
        if (ids.Length > 2) return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"title\":\"refused\"}") };
        var results = string.Join(",", ids.Select(id => $"{{\"itemId\":{id},\"nq\":{{\"minListing\":{{\"world\":{{\"price\":{100 + id}}}}}}},\"hq\":{{}}}}"));
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{{\"results\":[{results}],\"failedItems\":[]}}") };
    }));

    private static (string? Result, int Asked, List<string> Log) Run(Action<MarketService> giveUp)
    {
        var asked = 0;
        var log = new List<string>();
        MarketService? service = null;
        using var http = Client(_ => { if (Interlocked.Increment(ref asked) == 1) giveUp(service!); });
        using var market = new MarketService(http) { RequestPause = TimeSpan.Zero, SplitPause = TimeSpan.Zero };
        service = market;
        market.Trace = line => { lock (log) log.Add(line); };
        market.SetWorld(7, 97, "World", "DataCenter");
        var result = market.FillCacheAsync([2, 3, 4, 5], 7, 97, CancellationToken.None, true, new HashSet<uint>()).GetAwaiter().GetResult();
        return (result, asked, log);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Market abandon 1855: behaviour: a list given up between two halves (world changed, character changed, setting off) returns no failure and asks no half", () =>
        {
            foreach (var (name, giveUp) in new (string, Action<MarketService>)[]
            {
                ("world changed", market => market.SetWorld(7, 98, "Other", "DataCenter")),
                ("character changed", market => market.SetWorld(8, 97, "World", "DataCenter")),
            })
            {
                var (result, asked, log) = Run(giveUp);
                Check(result is null, name + ": no failure shown to the player: " + result);
                Check(asked == 1, name + ": no half asked after: " + asked);
                Check(log.Count == 1 && log[0].Contains("stopped: the list was no longer wanted", StringComparison.Ordinal), name + ": the log says why: " + string.Join(" | ", log));
            }
            var enabled = true;
            var count = 0;
            var lines = new List<string>();
            using var http = Client(_ => { if (Interlocked.Increment(ref count) == 1) enabled = false; });
            using var market = new MarketService(http) { RequestPause = TimeSpan.Zero, SplitPause = TimeSpan.Zero, Enabled = () => enabled, Trace = line => lines.Add(line) };
            market.SetWorld(7, 97, "World", "DataCenter");
            var off = market.FillCacheAsync([2, 3, 4, 5], 7, 97, CancellationToken.None, true, new HashSet<uint>()).GetAwaiter().GetResult();
            Check(off is null && count == 1, "setting off: no failure, no half asked: " + off + ", " + count);
            Check(lines.Count == 1 && lines[0].Contains("no longer wanted", StringComparison.Ordinal), "setting off: the log says why");
        }),
        ("Market abandon 1855: wiring: no English failure text for a list no longer wanted", () =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Market/MarketService.cs"));
            Check(!code.Contains("?? \"not wanted any more\"", StringComparison.Ordinal), "the old failure text is gone");
            Check(code.Contains("return stopped.Failure;", StringComparison.Ordinal), "only a real failure (a pause asked by Universalis) is returned");
        }),
    ];
}
