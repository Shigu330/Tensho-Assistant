using System.Net;
using TenshoAssistant.Beastmaster;
using TenshoAssistant.Market;

// X-848 (review of the block 0.1845 to 0.1848, minors): M1 the « N missing » count of a price list that failed, and the prices read before
// the failure; M2 one log line for a list refused by answers 400; M3 a failed « Best team » calculation logged once per key and worked out
// again less and less often. M4 is a measure (docs/evidence/0.1846-memory-measure.md). Behaviour tests with a fake Universalis.
internal static class ReviewMinors1851Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(send(request));
    }

    private static HttpClient Universalis(Func<uint[], HttpStatusCode> answer) => new(new Handler(request =>
    {
        if (request.RequestUri?.Query.Contains("listings=0", StringComparison.Ordinal) == true) return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        var ids = request.RequestUri!.AbsolutePath["/api/v2/aggregated/".Length..].Split('/')[1].Split(',').Select(uint.Parse).ToArray();
        var status = answer(ids);
        if (status != HttpStatusCode.OK) return new HttpResponseMessage(status) { Content = new StringContent("{}") };
        var results = string.Join(",", ids.Select(id => $"{{\"itemId\":{id},\"nq\":{{\"minListing\":{{\"world\":{{\"price\":{100 + id}}}}}}},\"hq\":{{}}}}"));
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{{\"results\":[{results}],\"failedItems\":[]}}") };
    }));

    private static MarketService Service(HttpClient http)
    {
        var market = new MarketService(http) { RequestPause = TimeSpan.Zero, SplitPause = TimeSpan.Zero };
        market.SetWorld(7, 97, "World", "DataCenter");
        return market;
    }

    private static uint[] Items(int count) => Enumerable.Range(2, count).Select(id => (uint)id).ToArray();

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1851: behaviour: M1, a list that fails at its first request says every item missing, the unsold ones counted once", () =>
        {
            using var http = Universalis(_ => HttpStatusCode.InternalServerError);
            using var market = Service(http);
            market.Marketable = id => id is not (2 or 3 or 4);
            using var session = new PreparationPriceSession();
            session.StartListAsync(market, 7, 97, Items(32), bulk: true).GetAwaiter().GetResult();
            var state = session.State;
            Check(state.Error is not null && state.Refused == 3, "failed, three unsold refused: " + state.Error + " " + state.Refused);
            Check(state.Missing == 32, "32 missing out of 32, not 35: " + state.Missing);
        }),
        ("Review minors 1851: behaviour: M1, the prices read before a failure count as done and are not missing", () =>
        {
            var calls = 0;
            // The whole list answers 400, its first half is read, its second half fails.
            using var http = Universalis(ids => Interlocked.Increment(ref calls) switch { 1 => HttpStatusCode.BadRequest, 2 => HttpStatusCode.OK, _ => HttpStatusCode.InternalServerError });
            using var market = Service(http);
            using var session = new PreparationPriceSession();
            session.StartListAsync(market, 7, 97, Items(10), bulk: true).GetAwaiter().GetResult();
            var state = session.State;
            Check(state.Error is not null && state.Refused == 0, "failed after the first half: " + state.Error);
            Check(state.Completed == 5 && state.Missing == 5, "five read, five missing: " + state.Completed + " " + state.Missing);
            Check(Items(10).Take(5).All(id => market.GetCached(97, id) is not null), "the first half's prices are in the cache");
            Check(PreparationPriceState.Done([1, 2, 3], new HashSet<uint> { 3 }, id => id == 1) == 2, "done: priced or refused");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.RoutePlanner.cs"));
            Check(page.Contains("localization.Format(\"RoutePlanner.PricesFailed\",prices.Missing)", StringComparison.Ordinal)
                && !page.Contains("prices.Items.Take(prices.Completed)", StringComparison.Ordinal), "the route page counts Missing and keeps every price read");
        }),
        ("Review minors 1851: behaviour: M2, a list refused by answers 400 leaves one log line with the refused items; a list read at once leaves none", () =>
        {
            foreach (var (answer, expect) in new (Func<uint[], HttpStatusCode>, string?)[]
            {
                (ids => ids.Contains(9u) ? HttpStatusCode.BadRequest : HttpStatusCode.OK, "got 4 answer(s) 400; refused alone: 9; the others read."),
                (_ => HttpStatusCode.BadRequest, "got 10 answer(s) 400; refused alone: "),
                (_ => HttpStatusCode.OK, null),
            })
            {
                using var http = Universalis(answer);
                using var market = Service(http);
                var lines = new List<string>();
                market.Trace = lines.Add;
                market.FillCacheAsync(Items(10), 7, 97, CancellationToken.None, true, new HashSet<uint>()).GetAwaiter().GetResult();
                if (expect is null) Check(lines.Count == 0, "no line: " + string.Join(" | ", lines));
                else Check(lines.Count == 1 && lines[0].StartsWith("Universalis prices: a list of 10 item(s) on world 97 ", StringComparison.Ordinal) && lines[0].Contains(expect, StringComparison.Ordinal), "one line: " + string.Join(" | ", lines));
            }
            using var stopHttp = Universalis(_ => HttpStatusCode.BadRequest);
            using var stopMarket = Service(stopHttp);
            var stopLines = new List<string>();
            stopMarket.Trace = stopLines.Add;
            stopMarket.FillCacheAsync(Items(10), 7, 97, CancellationToken.None, true, new HashSet<uint>()).GetAwaiter().GetResult();
            Check(stopLines.Single().EndsWith("stopped: the ceiling of answers 400.", StringComparison.Ordinal), "the ceiling said: " + stopLines.Single());
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("market.Trace=line=>log.Information(line);", StringComparison.Ordinal), "the plugin writes it to the log");
        }),
        ("Review minors 1851: behaviour: M3, a failed best team is logged once per key, then worked out again less and less often", () =>
        {
            var retry = new BestTeamRetry();
            Check(retry.Failed(1, "a", 0) == BestTeamRetry.Step.Log, "first failure logged");
            Check(retry.Failed(1, "a", 9_999) == BestTeamRetry.Step.Wait, "waits 10 s");
            Check(retry.Failed(1, "a", 10_000) == BestTeamRetry.Step.Retry, "worked out again after 10 s");
            Check(retry.Failed(1, "a", 11_000) == BestTeamRetry.Step.Wait, "failed again: not logged");
            Check(retry.Failed(1, "a", 30_999) == BestTeamRetry.Step.Wait && retry.Failed(1, "a", 31_000) == BestTeamRetry.Step.Retry, "then 20 s");
            Check(BestTeamRetry.Delay(1) == 10_000 && BestTeamRetry.Delay(3) == 40_000 && BestTeamRetry.Delay(50) == BestTeamRetry.LongestDelay, "doubling, at most 5 min");
            var logs = 0;
            for (long now = 0; now < 600_000; now += 1_000)
                if (retry.Failed(2, "always", now) == BestTeamRetry.Step.Log) logs++;
            Check(logs == 1, "a calculation that always fails is logged once in 10 min: " + logs);
            Check(retry.Failed(1, "b", 40_000) == BestTeamRetry.Step.Log && retry.Failed(1, "b", 40_001) == BestTeamRetry.Step.Wait, "a new key: logged and waits, not retried at once by an old entry");
            retry.Succeeded(1);
            Check(retry.Failed(1, "b", 50_000) == BestTeamRetry.Step.Log, "after a success, a new failure is logged again");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("switch (beastBestRetry.Failed(board.Id, key, Environment.TickCount64))", StringComparison.Ordinal)
                && page.Contains("beastBestRetry.Succeeded(board.Id);", StringComparison.Ordinal), "the page goes through BestTeamRetry");
        }),
    ];
}
