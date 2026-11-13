using System.Net;
using TenshoAssistant;
using TenshoAssistant.Items;
using TenshoAssistant.Market;

// X-278 (0.1287): in game on 13/11, hover price « Fetched 12-11-26 01:04:37 », lines of 9/11 and 11/11 marked « old », and Ctrl+Alt
// changed nothing: the shown quote was the one seen at the market board on 12/11 (the price cache set to « never » keeps it fresh), and a
// Universalis answer whose world upload is older than that look gave way to it again. Behaviour tests of the coordinator and the market.
internal static class HoverRefresh1287Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // Shape of a real Universalis aggregated answer (item 5057, world 97); its world upload, 3/11, is older than the look at the board.
    private const string Answer = """
        {"results":[{"itemId":5057,
          "nq":{"minListing":{"world":{"price":89},"dc":{"price":45,"worldId":83},"region":{"price":40,"worldId":33}},
                "recentPurchase":{"world":{"price":71,"timestamp":1793612456000}}},
          "hq":{},
          "worldUploadTimes":[{"worldId":97,"timestamp":1793612456000}]}]}
        """;

    private static ItemCatalog Catalog() => ItemCatalog.Build([new(5057, "Marketable", false, IsMarketable: true)], new("fixture", "French", "v", 1));

    private static (MarketService Market, MarketQuote Seen) Setup(HttpClient http)
    {
        var market = new MarketService(http);
        market.SetWorld(7, 97, "World", "DataCenter");
        var seenAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var seen = new MarketQuote(97, 5057, seenAt, seenAt, new(120m, null, null, null), new(null, null, null, null));
        // As SeenPrices.Prefer: the board's look wins when it is newer than Universalis' world upload.
        market.Seen = (world, item, universalis) => universalis is null || (universalis.UploadedAt ?? universalis.FetchedAt) < seenAt ? seen : null;
        return (market, seen);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-278: Ctrl+Alt on a price seen at the board sends the request, and every line then comes from the new answer", () =>
        {
            var calls = 0;
            using var http = new HttpClient(new MarketTestHandler((_, _) => { Interlocked.Increment(ref calls); return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Answer) }); }));
            var (market, seen) = Setup(http);
            using var owned = market;
            var done = new List<(uint, MarketFetchStatus)>();
            using var context = new ItemHoverService();
            using var coordinator = new ItemHoverCoordinator(context, market) { RefreshDone = (item, status) => { lock (done) done.Add((item, status)); } };
            var now = DateTimeOffset.UtcNow;
            var sentBefore = TenshoAssistant.Community.BoundedHttp.Sent.GetValueOrDefault("universalis.app");
            coordinator.Observe(true, true, 7, 97, 5057, Catalog(), now);
            Check(coordinator.Observe(true, true, 7, 97, 5057, Catalog(), now.AddMilliseconds(700)) is null && calls == 0, "the seen price is fresh: nothing asked by itself");
            Check(market.GetCached(97, 5057) == seen, "the board's look is shown first");
            var pressed = coordinator.Observe(true, true, 7, 97, 5057, Catalog(), now.AddMilliseconds(800), true);
            Check(pressed is not null, "Ctrl+Alt sends a request although the price is fresh");
            Check(pressed!.GetAwaiter().GetResult().Status == MarketFetchStatus.Success && calls == 1, "request answered");
            // X-292 (0.1291): the newest reading wins, asked again or not: the look (a minute ago) is newer than the answer's world upload (3/11).
            Check(market.GetCached(97, 5057) == seen, "the newer look stays (X-292)");
            Check(TenshoAssistant.Community.BoundedHttp.Sent.GetValueOrDefault("universalis.app") > sentBefore, "the dashboard counts it");
            for (var wait = 0; wait < 50; wait++) { lock (done) if (done.Count > 0) break; Thread.Sleep(20); }
            lock (done) Check(done.SequenceEqual([(5057u, MarketFetchStatus.Success)]), "the log line is given the request's end");
        }),
        ("X-278: an ordinary fetch keeps preferring a newer look at the board (X-52 unchanged)", () =>
        {
            using var http = new HttpClient(new MarketTestHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Answer) })));
            var (market, seen) = Setup(http);
            using var owned = market;
            Check(market.RequestHoverAsync(5057, 7, 97).GetAwaiter().GetResult().Status == MarketFetchStatus.Success, "fetched");
            Check(market.GetCached(97, 5057) == seen, "not asked by the player: the newer look stays");
        }),
        ("X-278: Ctrl+Alt on an item without market price says so", () =>
        {
            using var http = new HttpClient(new MarketTestHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Answer) })));
            using var market = new MarketService(http);
            market.SetWorld(7, 97, "World");
            ulong? ignored = null;
            using var context = new ItemHoverService();
            using var coordinator = new ItemHoverCoordinator(context, market) { RefreshIgnored = raw => ignored = raw };
            var catalog = ItemCatalog.Build([new(43, "Bound", true)], new("fixture", "French", "v", 1));
            Check(coordinator.Observe(true, true, 7, 97, 43, catalog, DateTimeOffset.UtcNow, true) is null && ignored == 43, "no price asked, the reason logged");
        }),
    ];
}
