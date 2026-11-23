using System.Diagnostics;
using System.Net;
using System.Net.Http;
using TenshoAssistant.Market;

// X-846 (review 0.1848, I1): a list refused by a 400 is asked again in halves (X-815), now under a ceiling of answers 400 and at the pace of
// any request: the path's own pause (RequestPause for a short list, SplitPause/ListPause for a long one), a pause asked by Universalis, and
// one sale-speed request for all the packets asked again. Behaviour tests on the real service with a fake Universalis.
internal static class UniversalisCeiling1849Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(send(request));
    }

    private sealed class FakeUniversalis(Func<uint[], HttpStatusCode> answer)
    {
        public readonly List<(uint[] Ids, long At)> Asked = [];
        public int Speeds;
        public HttpClient Client() => new(new Handler(request =>
        {
            if (request.RequestUri?.Query.Contains("listings=0", StringComparison.Ordinal) == true)
            {
                Interlocked.Increment(ref Speeds);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            }
            var path = request.RequestUri?.AbsolutePath ?? "";
            const string prefix = "/api/v2/aggregated/";
            if (!path.StartsWith(prefix, StringComparison.Ordinal)) return new HttpResponseMessage(HttpStatusCode.NotFound);
            var ids = path[prefix.Length..].Split('/')[1].Split(',').Select(uint.Parse).ToArray();
            lock (Asked) Asked.Add((ids, Stopwatch.GetTimestamp()));
            var status = answer(ids);
            if (status != HttpStatusCode.OK) return new HttpResponseMessage(status) { Content = new StringContent("{\"title\":\"refused\"}") };
            var results = string.Join(",", ids.Select(id => $"{{\"itemId\":{id},\"nq\":{{\"minListing\":{{\"world\":{{\"price\":{100 + id}}}}}}},\"hq\":{{}}}}"));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{{\"results\":[{results}],\"failedItems\":[]}}") };
        }));
        public int Count { get { lock (Asked) return Asked.Count; } }
        public double Gap(int index) { lock (Asked) return Stopwatch.GetElapsedTime(Asked[index - 1].At, Asked[index].At).TotalMilliseconds; }
    }

    private static MarketService Service(HttpClient http, TimeSpan request, TimeSpan split)
    {
        var market = new MarketService(http) { RequestPause = request, SplitPause = split };
        market.SetWorld(7, 97, "World", "DataCenter");
        return market;
    }

    private static uint[] Items(int count) => Enumerable.Range(2, count).Select(id => (uint)id).ToArray();

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Universalis ceiling 1849: behaviour: a server that answers 400 to everything costs MaxBadRequests requests, not 2n-1, and the list says it failed", () =>
        {
            foreach (var count in new[] { 32, 100 })
            {
                var fake = new FakeUniversalis(_ => HttpStatusCode.BadRequest);
                using var http = fake.Client();
                using var market = Service(http, TimeSpan.Zero, TimeSpan.Zero);
                var refused = new HashSet<uint>();
                var message = market.FillCacheAsync(Items(count), 7, 97, CancellationToken.None, true, refused).GetAwaiter().GetResult();
                Check(message == MarketService.PricesUnread, count + " items: the list stops with the plugin's message: " + message);
                Check(fake.Count == MarketService.MaxBadRequests, count + " items: " + fake.Count + " requests, at most " + MarketService.MaxBadRequests);
                Check(fake.Speeds == 0, "no sale speed asked for nothing read");
            }
            var shortList = new FakeUniversalis(_ => HttpStatusCode.BadRequest);
            using var shortHttp = shortList.Client();
            using var shortMarket = Service(shortHttp, TimeSpan.Zero, TimeSpan.Zero);
            Check(shortMarket.FillCacheAsync(Items(32), 7, 97, CancellationToken.None, false, new HashSet<uint>()).GetAwaiter().GetResult() == MarketService.PricesUnread
                && shortList.Count == MarketService.MaxBadRequests, "the short path has the same ceiling: " + shortList.Count);
        }),
        ("Universalis ceiling 1849: behaviour: one item refused in a list of 100 still goes through under the ceiling; the packets asked again share one sale-speed request", () =>
        {
            var fake = new FakeUniversalis(ids => ids.Contains(77u) ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
            using var http = fake.Client();
            using var market = Service(http, TimeSpan.Zero, TimeSpan.Zero);
            var refused = new HashSet<uint>();
            Check(market.FillCacheAsync(Items(100), 7, 97, CancellationToken.None, true, refused).GetAwaiter().GetResult() is null, "no failure");
            Check(refused.SetEquals([77u]), "only that item refused: " + string.Join(",", refused));
            var bad = fake.Asked.Count(asked => asked.Ids.Contains(77u));
            Check(bad < MarketService.MaxBadRequests, "answers 400 under the ceiling: " + bad);
            Check(Items(100).Where(id => id != 77).All(id => market.GetCached(97, id)?.Nq.Minimum == 100 + id), "every other price read");
            Check(fake.Speeds == 1, "one sale-speed request for the packets asked again, not one each: " + fake.Speeds);
            var whole = new FakeUniversalis(_ => HttpStatusCode.OK);
            using var wholeHttp = whole.Client();
            using var wholeMarket = Service(wholeHttp, TimeSpan.Zero, TimeSpan.Zero);
            Check(wholeMarket.FillCacheAsync(Items(10), 7, 97, CancellationToken.None, true).GetAwaiter().GetResult() is null && whole.Count == 1 && whole.Speeds == 1,
                "a list read at once: one request and its sale speed, as before");
        }),
        ("Universalis ceiling 1849: behaviour: a packet asked again waits the pause of its path: RequestPause for a short list, SplitPause for a long one", () =>
        {
            foreach (var bulk in new[] { false, true })
            {
                var calls = 0;
                var fake = new FakeUniversalis(_ => Interlocked.Increment(ref calls) == 1 ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
                using var http = fake.Client();
                var pause = TimeSpan.FromMilliseconds(300);
                using var market = Service(http, bulk ? TimeSpan.Zero : pause, bulk ? pause : TimeSpan.Zero);
                Check(market.FillCacheAsync([2, 3, 4, 5], 7, 97, CancellationToken.None, bulk, new HashSet<uint>()).GetAwaiter().GetResult() is null, "read");
                Check(fake.Count == 3, "the list, then its two halves: " + fake.Count);
                for (var index = 1; index < fake.Count; index++)
                    Check(fake.Gap(index) >= 280, (bulk ? "long" : "short") + " list: request " + index + " came " + fake.Gap(index) + " ms after the one before");
            }
        }),
        ("Universalis ceiling 1849: behaviour: a pause asked by Universalis meanwhile stops the halves still to ask", () =>
        {
            var fake = new FakeUniversalis(ids => ids.Contains(900u) ? (HttpStatusCode)429 : ids.Length == 4 ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
            using var http = fake.Client();
            using var market = Service(http, TimeSpan.Zero, TimeSpan.FromMilliseconds(600));
            var list = Task.Run(() => market.FillCacheAsync([2, 3, 4, 5], 7, 97, CancellationToken.None, true, new HashSet<uint>()));
            var waited = Stopwatch.StartNew();
            while (fake.Count < 1 && waited.ElapsedMilliseconds < 3000) Thread.Sleep(5);
            var other = market.FillCacheAsync([900, 901], 7, 97, CancellationToken.None, false, new HashSet<uint>()).GetAwaiter().GetResult();
            Check(other?.Contains("429", StringComparison.Ordinal) == true, "the other list got the 429: " + other);
            var message = list.GetAwaiter().GetResult();
            Check(message == "Universalis asked for a pause.", "the list stops on the server's pause: " + message);
            Check(fake.Count == 2, "no half asked during the pause: " + string.Join(" | ", fake.Asked.Select(asked => string.Join(",", asked.Ids))));
        }),
        ("Universalis ceiling 1849: wiring: the ceiling, the shared turn and the one speed request are in the service", () =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Market/MarketService.cs"));
            Check(code.Contains("if(++badRequests>=MaxBadRequests)return PricesUnread;", StringComparison.Ordinal), "ceiling");
            Check(code.Contains("await NextTurnAsync(world,expectedOwner,bulk,token)", StringComparison.Ordinal), "the packets asked again wait their turn");
            Check(!code.Contains("await Task.Delay(SplitPause,token)", StringComparison.Ordinal), "no bare SplitPause wait left");
        }),
    ];
}
