using System.Net;
using TenshoAssistant.Market;

// X-292 (0.1291): essay T-0.1287-1 of 13/11 (not ok): after Ctrl+Alt the world's line still said a reading of 09/11, « old data ». User rule:
// for each line, the newest reading wins, the look at the game's board or Universalis' upload, with its date and source. Behaviour tests on
// the real store and service, wired as the plugin wires them (Plugin.cs: SeenPriceStore.Prefer, else Universalis).
internal static class NewestPrice1291Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // Shape of a real Universalis aggregated answer (item 5057, world 97), its world upload at the given time.
    private static string Answer(DateTimeOffset upload) => """
        {"results":[{"itemId":5057,
          "nq":{"minListing":{"world":{"price":89},"dc":{"price":45,"worldId":83},"region":{"price":40,"worldId":33}},
                "recentPurchase":{"world":{"price":71,"timestamp":UPLOAD}}},
          "hq":{},
          "worldUploadTimes":[{"worldId":97,"timestamp":UPLOAD}]}]}
        """.Replace("UPLOAD", upload.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture));

    // The board seen at boardAt (one NQ offer at 120), Universalis answering with its world upload at uploadAt; then a fetch, forced or not.
    private static MarketQuote Shown(DateTimeOffset boardAt, DateTimeOffset uploadAt, bool force, out DateTimeOffset asked)
    {
        using var http = new HttpClient(new MarketTestHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Answer(uploadAt)) })));
        using var market = new MarketService(http);
        market.SetWorld(7, 97, "World", "DataCenter");
        var store = new SeenPriceStore();
        store.SeeListings(97, 5057, 1, [new SeenListing(120m, false, 3)], boardAt);
        market.Seen = (world, item, universalis) => store.Prefer(world, item, universalis, DateTimeOffset.UtcNow, true) ?? universalis;
        asked = DateTimeOffset.UtcNow;
        Check(market.RequestHoverAsync(5057, 7, 97, force).GetAwaiter().GetResult().Status == MarketFetchStatus.Success, "answered");
        return market.GetCached(97, 5057) ?? throw new Exception("nothing shown");
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-292: seen at the board on 09/11, Universalis uploaded on 13/11: after Ctrl+Alt the world's line is Universalis', dated 13/11", () =>
        {
            var now = DateTimeOffset.UtcNow;
            var shown = Shown(now.AddDays(-4), now.AddMinutes(-10), true, out var asked);
            Check(!shown.SeenAtBoard && shown.Nq.Minimum == 89, "Universalis' lines");
            Check(shown.UploadedAt?.ToUnixTimeMilliseconds() == now.AddMinutes(-10).ToUnixTimeMilliseconds(), "dated by its upload");
            Check(shown.FetchedAt >= asked.AddSeconds(-1) && shown.HasRecentUpload(DateTimeOffset.UtcNow), "fetched now, not old");
        }),
        ("X-292: the reverse, seen at the board on 13/11 and Universalis' upload of 09/11: after Ctrl+Alt the board's line, dated and marked, Fetched moves", () =>
        {
            var now = DateTimeOffset.UtcNow;
            var boardAt = now.AddMinutes(-10);
            var shown = Shown(boardAt, now.AddDays(-4), true, out var asked);
            Check(shown.SeenAtBoard && shown.Nq.Minimum == 120, "the board's lines");
            Check(shown.UploadedAt == boardAt, "dated by the look");
            Check(shown.FetchedAt >= asked.AddSeconds(-1), "« Fetched » is the request's time, not the look's");
            Check(shown.NqCheapest?.Price == 45 && shown.NqRegion?.Price == 40, "data centre and region lines from the answer");
        }),
        ("X-292: one rule: an ordinary fetch gives the same as Ctrl+Alt, both ways", () =>
        {
            var now = DateTimeOffset.UtcNow;
            Check(!Shown(now.AddDays(-4), now.AddMinutes(-10), false, out _).SeenAtBoard, "Universalis newer");
            Check(Shown(now.AddMinutes(-10), now.AddDays(-4), false, out _).SeenAtBoard, "board newer");
        }),
        ("X-292: the hover says the line's source; no forced bypass left in the service", () =>
        {
            string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
            Check(Read("MainWindow.MarketHover.cs").Contains("prices.SeenAtBoard ? \"HoverUi.SeenAtBoard\":\"HoverUi.Uploaded\""), "source on the hover");
            Check(!Read("Market/MarketService.cs").Contains("refreshed"), "no forced bypass");
        }),
    ];
}
