using System.Net;
using TenshoAssistant.Market;

// 0.1326 (X-331, review of 0.1318 I2): X-292's rule « the newest reading wins » compared the board's look with Universalis' upload, else its
// fetch time. An answer without an upload for the world (nobody sent that item there to Universalis) has no reading of it, but its fetch time
// is « now »: it hid a real look at the game's board. Behaviour tests on the real store and service, wired as the plugin wires them.
internal static class EmptyUniversalis1326Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // Shape of a real Universalis aggregated answer for an item nobody uploaded on world 97: other worlds only, no upload time for 97.
    private const string Empty = """
        {"results":[{"itemId":5057,
          "nq":{"minListing":{"dc":{"price":45,"worldId":83},"region":{"price":40,"worldId":33}}},
          "hq":{},
          "worldUploadTimes":[{"worldId":83,"timestamp":1763100000000}]}]}
        """;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Empty Universalis 1326: behaviour: the board seen yesterday, Universalis without an upload for the world: after Ctrl+Alt the board's line stays, other worlds kept", () =>
        {
            using var http = new HttpClient(new MarketTestHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Empty) })));
            using var market = new MarketService(http);
            market.SetWorld(7, 97, "World", "DataCenter");
            var store = new SeenPriceStore();
            var seen = DateTimeOffset.UtcNow.AddDays(-1);
            store.SeeListings(97, 5057, 1, [new SeenListing(120m, false, 3)], seen);
            market.Seen = (world, item, universalis) => store.Prefer(world, item, universalis, DateTimeOffset.UtcNow, true) ?? universalis;
            Check(market.RequestHoverAsync(5057, 7, 97, true).GetAwaiter().GetResult().Status == MarketFetchStatus.Success, "answered");
            var shown = market.GetCached(97, 5057) ?? throw new Exception("nothing shown");
            Check(shown.SeenAtBoard && shown.Nq.Minimum == 120, "the board's line kept: " + shown.SeenAtBoard + " " + shown.Nq.Minimum);
            Check(shown.UploadedAt?.ToUnixTimeMilliseconds() == seen.ToUnixTimeMilliseconds(), "dated by the look");
            Check(shown.NqRegion?.Price == 40, "Universalis' region kept beside it");
        }),
        ("Empty Universalis 1326: the rule: no upload keeps the look; an upload newer than the look wins; an older one loses", () =>
        {
            var now = DateTimeOffset.UtcNow;
            var store = new SeenPriceStore();
            store.SeeListings(97, 5057, 1, [new SeenListing(120m, false, 3)], now.AddHours(-2));
            MarketQuote Universalis(DateTimeOffset? upload) => new(97, 5057, now, upload, new(89, null, null), new(null, null, null));
            Check(store.Prefer(97, 5057, Universalis(null), now) is { SeenAtBoard: true }, "no upload for the world: the look stays");
            Check(store.Prefer(97, 5057, Universalis(now.AddHours(-1)), now) is null, "an upload after the look: Universalis");
            Check(store.Prefer(97, 5057, Universalis(now.AddHours(-3)), now) is { SeenAtBoard: true }, "an upload before the look: the look");
        }),
    ];
}
