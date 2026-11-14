using System.Net;
using TenshoAssistant.Market;

// 0.1331 (X-343, user report of 14/11): an item just seen at the game's board, then hovered with Ctrl+Alt, showed Universalis' line for the
// world instead of the board's. Dalamud uploads what the player sees at the board to Universalis itself (docs/evidence/x343-dalamud-upload.md):
// the upload falls seconds after the look and won X-292's « newest reading » rule although it is the same reading. An upload counts as newer
// only past SeenPrices.SameReading (5 min) after the look. Behaviour tests on the real store and service, wired as the plugin wires them.
internal static class SameReading1331Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // A Universalis aggregated answer for item 5057 with an upload for world 97 at « uploaded » (null: no upload for 97, other worlds only).
    private static string Answer(DateTimeOffset? uploaded) =>
        """{"results":[{"itemId":5057,"nq":{"minListing":{"world":{"price":150},"dc":{"price":45,"worldId":83},"region":{"price":40,"worldId":33}}},"hq":{},"worldUploadTimes":[{"worldId":83,"timestamp":1763100000000}"""
        + (uploaded is { } at ? """,{"worldId":97,"timestamp":""" + at.ToUnixTimeMilliseconds() + "}" : "") + "]}]}";

    // The board seen « look » ago, then the item hovered with Ctrl+Alt (a forced Universalis request): what the line of world 97 shows.
    private static MarketQuote Shown(DateTimeOffset look, DateTimeOffset? uploaded)
    {
        using var http = new HttpClient(new MarketTestHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Answer(uploaded)) })));
        using var market = new MarketService(http);
        market.SetWorld(7, 97, "World", "DataCenter");
        var store = new SeenPriceStore();
        store.SeeListings(97, 5057, 1, [new SeenListing(120m, false, 3)], look);
        store.SeeSales(97, 5057, [new SeenSale(118m, false, 1, look.AddHours(-1))], look);
        market.Seen = (world, item, universalis) => store.Prefer(world, item, universalis, DateTimeOffset.UtcNow, true) ?? universalis;
        Check(market.RequestHoverAsync(5057, 7, 97, true).GetAwaiter().GetResult().Status == MarketFetchStatus.Success, "answered");
        return market.GetCached(97, 5057) ?? throw new Exception("nothing shown");
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Same reading 1331: behaviour: the board just seen, Dalamud's upload of it seconds later: after Ctrl+Alt the board's line stays", () =>
        {
            var look = DateTimeOffset.UtcNow.AddSeconds(-30);
            var shown = Shown(look, look.AddSeconds(4));
            Check(shown.SeenAtBoard && shown.Nq.Minimum == 120, "the board's line kept: " + shown.SeenAtBoard + " " + shown.Nq.Minimum);
            Check(shown.UploadedAt?.ToUnixTimeMilliseconds() == look.ToUnixTimeMilliseconds(), "dated by the look");
            Check(shown.NqRegion?.Price == 40 && shown.NqCheapest?.Price == 45, "Universalis' other worlds and region kept beside it");
        }),
        ("Same reading 1331: behaviour: Universalis without an upload for the world: the board's line stays", () =>
        {
            var shown = Shown(DateTimeOffset.UtcNow.AddMinutes(-1), null);
            Check(shown.SeenAtBoard && shown.Nq.Minimum == 120, "the board's line kept");
        }),
        ("Same reading 1331: behaviour: a truly newer upload (another player, 20 min after the look) replaces the board's line", () =>
        {
            var look = DateTimeOffset.UtcNow.AddMinutes(-30);
            var shown = Shown(look, look.AddMinutes(20));
            Check(!shown.SeenAtBoard && shown.Nq.Minimum == 150, "Universalis' line: " + shown.SeenAtBoard + " " + shown.Nq.Minimum);
        }),
        ("Same reading 1331: the same bound for the data center's cheapest, the market page and the retainer comparison", () =>
        {
            var now = DateTimeOffset.UtcNow;
            var look = now.AddMinutes(-30);
            Check(SeenPrices.UniversalisNewer(look.AddSeconds(4), look) == false && SeenPrices.UniversalisNewer(look.Add(SeenPrices.SameReading), look) == false, "within 5 min: the same reading");
            Check(SeenPrices.UniversalisNewer(look.AddMinutes(6), look) && !SeenPrices.UniversalisNewer(null, look) && !SeenPrices.UniversalisNewer(look.AddHours(-1), look), "after: newer; none or before: not");
            // Data center: Universalis' cheapest is on world 97 itself, uploaded by Dalamud seconds after the look: the look's price stands.
            var same = SeenPrices.Cheapest(new MarketListing(150m, 97, look.AddSeconds(4)), [(97u, 120m, look)], now);
            Check(same?.Price == 120m && same.WorldId == 97, "data center: the look");
            var newer = SeenPrices.Cheapest(new MarketListing(150m, 97, look.AddMinutes(20)), [(97u, 120m, look)], now);
            Check(newer?.Price == 150m, "data center: a newer upload");
            // The market page: Universalis' offers of world « World » reviewed seconds after the look give way to the look's.
            var entry = new SeenMarketEntry { World = 97, Item = 5057, Listings = [new SeenListing(120m, false, 3)], ListingsAt = look };
            MarketBoard Board(DateTimeOffset reviewed) => new("World", 5057, now, [new MarketOffer(150, 2, false, "World", "", 15, reviewed)], [], 1, 1, 0);
            Check(SeenPrices.WithSeen(Board(look.AddSeconds(4)), "World", 5057, [("World", entry)], now)?.Offers is [{ UnitPrice: 120, Seen: true }], "page: the look");
            Check(SeenPrices.WithSeen(Board(look.AddMinutes(20)), "World", 5057, [("World", entry)], now)?.Offers is [{ UnitPrice: 150 }], "page: a newer review");
            // The retainer comparison: Universalis' data of the home world uploaded seconds after the look: the look's offers.
            var mine = new SeenMarketEntry { World = 97, Item = 5057, Listings = [new SeenListing(120m, false, 3)], ListingsAt = look, OwnKnown = true };
            Check(SeenPrices.RetainerOffers(mine, "World", new RetainerItemOffers([], look.AddSeconds(4)), now) is { Offers: [{ UnitPrice: 120 }] }, "retainers: the look");
            Check(SeenPrices.RetainerOffers(mine, "World", new RetainerItemOffers([], look.AddMinutes(20)), now) is null, "retainers: a newer upload");
        }),
    ];
}
