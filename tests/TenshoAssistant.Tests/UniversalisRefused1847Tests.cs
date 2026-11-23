using System.Net;
using System.Net.Http;
using TenshoAssistant.Market;

// X-815 (0.1847, the user's capture of 22/11: the submarine routes' « Market prices » answered « 400 (Bad Request) », no price of 32 read,
// the message in English). Universalis answers 400 to a list of items its market does not know, and now and then to any list: a list refused
// by a 400 is asked again in halves down to the refused item, the other prices are read, the items the market does not sell are not asked,
// and the window's line is the plugin's own text. Behaviour tests on the real service and price session, with a fake Universalis.
internal static class UniversalisRefused1847Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class FakeUniversalis(Func<uint[], HttpStatusCode> answer)
    {
        public readonly List<uint[]> Asked = [];
        public HttpClient Client() => new(new MarketTestHandler((request, _) =>
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            const string prefix = "/api/v2/aggregated/";
            if (!path.StartsWith(prefix, StringComparison.Ordinal)) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            var ids = path[prefix.Length..].Split('/')[1].Split(',').Select(uint.Parse).ToArray();
            lock (Asked) Asked.Add(ids);
            var status = answer(ids);
            if (status != HttpStatusCode.OK) return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent("{\"title\":\"Bad Request\",\"status\":400}") });
            var results = string.Join(",", ids.Select(id => $"{{\"itemId\":{id},\"nq\":{{\"minListing\":{{\"world\":{{\"price\":{100 + id}}}}}}},\"hq\":{{}}}}"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{{\"results\":[{results}],\"failedItems\":[]}}") });
        }));
    }

    private static MarketService Service(HttpClient http)
    {
        var market = new MarketService(http) { RequestPause = TimeSpan.Zero, SplitPause = TimeSpan.Zero };
        market.SetWorld(7, 97, "World", "DataCenter");
        return market;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Universalis refused 1847: behaviour: a list refused by a 400 for one item is asked again in halves: the other prices are read, only that item is refused", () =>
        {
            var fake = new FakeUniversalis(ids => ids.Contains(99u) ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
            using var http = fake.Client();
            using var market = Service(http);
            var refused = new HashSet<uint>();
            var message = market.FillCacheAsync([2, 3, 99, 4, 5], 7, 97, CancellationToken.None, true, refused).GetAwaiter().GetResult();
            Check(message is null, "no failure: " + message);
            Check(refused.SetEquals([99u]), "refused: " + string.Join(",", refused));
            foreach (var id in new uint[] { 2, 3, 4, 5 })
                Check(market.GetCached(97, id)?.Nq.Minimum == 100 + id, "price of " + id + " read");
            Check(market.GetCached(97, 99) is null, "the refused item has no price");
            var asked = string.Join(" | ", fake.Asked.Select(ids => string.Join(",", ids)));
            Check(asked == "2,3,99,4,5 | 2,3 | 99,4,5 | 99 | 4,5", "asked in halves down to the item: " + asked);
        }),
        ("Universalis refused 1847: behaviour: an item the market does not sell is not asked; a list of them alone sends nothing", () =>
        {
            var fake = new FakeUniversalis(_ => HttpStatusCode.OK);
            using var http = fake.Client();
            using var market = Service(http);
            market.Marketable = id => id != 1 && id != 10155;
            var refused = new HashSet<uint>();
            Check(market.FillCacheAsync([1, 2, 3], 7, 97, CancellationToken.None, true, refused).GetAwaiter().GetResult() is null, "read");
            Check(fake.Asked.Count == 1 && fake.Asked[0].SequenceEqual([2u, 3u]), "only the sold items asked: " + string.Join(" | ", fake.Asked.Select(ids => string.Join(",", ids))));
            Check(refused.SetEquals([1u]), "the unsold item refused without a request");
            refused.Clear();
            Check(market.FillCacheAsync([1, 10155], 7, 97, CancellationToken.None, true, refused).GetAwaiter().GetResult() is null, "nothing to ask");
            Check(fake.Asked.Count == 1 && refused.SetEquals([1u, 10155u]), "no request for unsold items alone");
        }),
        ("Universalis refused 1847: behaviour: a transient 400 on the whole list: the halves are read, nothing is refused", () =>
        {
            var calls = 0;
            var fake = new FakeUniversalis(_ => Interlocked.Increment(ref calls) == 1 ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
            using var http = fake.Client();
            using var market = Service(http);
            var refused = new HashSet<uint>();
            Check(market.FillCacheAsync([2, 3, 4, 5], 7, 97, CancellationToken.None, true, refused).GetAwaiter().GetResult() is null, "read");
            Check(refused.Count == 0, "nothing refused");
            Check(new uint[] { 2, 3, 4, 5 }.All(id => market.GetCached(97, id) is not null), "every price read");
        }),
        ("Universalis refused 1847: behaviour: the route's price list goes on past a refused item and says how many are missing; another failure is the plugin's message", () =>
        {
            var fake = new FakeUniversalis(ids => ids.Contains(99u) ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
            using var http = fake.Client();
            using var market = Service(http);
            using var session = new PreparationPriceSession();
            session.StartListAsync(market, 7, 97, [2, 99, 3]).GetAwaiter().GetResult();
            var state = session.State;
            Check(state.Error is null && !state.Loading, "the list ended without failure: " + state.Error);
            Check(state.Refused == 1 && state.Completed == 3, "one refused, three done: " + state.Refused + " " + state.Completed);
            Check(market.GetCached(97, 2) is not null && market.GetCached(97, 3) is not null, "the others read");
            var down = new FakeUniversalis(_ => HttpStatusCode.InternalServerError);
            using var downHttp = down.Client();
            using var downMarket = Service(downHttp);
            var failed = downMarket.FillCacheAsync([2, 3], 7, 97, CancellationToken.None, true).GetAwaiter().GetResult();
            Check(failed == MarketService.PricesUnread, "one fixed message: " + failed);
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/UI/InternalMessages.cs")).Contains("[\"Prices could not be read.\"]=\"InternalMessages.PricesCouldNotBeRead\"", StringComparison.Ordinal), "translated by the plugin's texts");
        }),
        ("Universalis refused 1847: languages and wiring: the route's line is the plugin's text in English and French; the plugin sets the market's items", () =>
        {
            var en = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            var fr = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            foreach (var key in new[] { "\"PricesFailed\":", "\"PricesFailedTip\":", "\"PricesRefused\":", "\"PricesCouldNotBeRead\":" })
                Check(en.Contains(key, StringComparison.Ordinal) && fr.Contains(key, StringComparison.Ordinal), key);
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.RoutePlanner.cs"));
            Check(page.Contains("localization.Format(\"RoutePlanner.PricesFailed\",", StringComparison.Ordinal) && !page.Contains("ImGui.TextColored(UI.FfxivTheme.Gold,failed)", StringComparison.Ordinal), "no raw message");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("market.Marketable=id=>", StringComparison.Ordinal), "the market's items set");
        }),
    ];
}
