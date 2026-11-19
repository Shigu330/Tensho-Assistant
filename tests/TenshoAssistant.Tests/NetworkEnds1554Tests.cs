using System.Net;
using TenshoAssistant.Caching;
using TenshoAssistant.Community;
using TenshoAssistant.Market;

// 0.1554 (X-589 first part, external audit of 18/11, family « network sources »): each source always ends in a terminal state (data, an
// expired copy, or an error tried again). Behaviour tests with a simulated network (R28.14): A08 a request past its delay, A09 an error then
// its deadline and the expired copy, A12 an upload time out of range or not a number, A26 an index without a fish.
internal static class NetworkEnds1554Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Until(Func<bool> done) { for (var wait = 0; !done() && wait < 500; wait++) Thread.Sleep(10); }

    /// <summary>A simulated network: each request answered by <c>answer</c> (its count passed), or the exception it throws.</summary>
    private sealed class Network(Func<int, HttpRequestMessage, string> answer) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var text = answer(Interlocked.Increment(ref Calls), request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text) });
        }
    }

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1554-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } }
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Network ends 1554: A08, LuminaSupplemental: a request past its delay without a copy ends in an error, never « loading » for the session", () =>
        {
            var network = new Network((_, _) => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 30 seconds elapsing."));
            using var service = new LuminaSupplementalService(() => TimeSpan.FromDays(1), null, network);
            service.Request();
            Until(() => !service.State.Loading);
            Check(!service.State.Loading && service.State.Error is { Length: > 0 } && service.State.Data is null, "an error, tried again after the pause: " + service.State);
        }),
        ("Network ends 1554: A09, Teamcraft's bite times: an error is asked again after its deadline, not before; the expired copy is taken first", () => InTemp(root =>
        {
            const string Answer = "{\"data\":{\"biteTimes\":[{\"itemId\":4869,\"flooredBiteTime\":12,\"occurences\":5}]}}";
            var network = new Network((call, _) => call == 1 ? throw new HttpRequestException("Teamcraft unreachable") : Answer);
            using var service = new FishBiteTimeService(() => TimeSpan.FromDays(1), null, new HttpClient(network)) { RetryMs = 100_000 };
            service.Get(31);
            Until(() => !service.Get(31).Loading);
            Check(service.Get(31) is { Loading: false, Error: { Length: > 0 } } && network.Calls == 1, "the error said");
            Thread.Sleep(50);
            Check(service.Get(31).Error is not null && network.Calls == 1, "not asked again before the deadline (no request at each frame)");
            service.RetryMs = 0;
            Until(() => service.Get(31) is { Loading: false, Times: not null });
            Check(service.Get(31).Times is { } times && times.ContainsKey(4869) && network.Calls == 2, "asked again after it: the times there");
            // An expired copy and Teamcraft unreachable: the copy, not an error.
            using var cache = new DataCache(Path.Combine(root, "DataCache"));
            Check(cache.WriteAsync("teamcraft-bites", "32", "v1", new FishBiteCache([[4869, 20, 7]]), DateTimeOffset.UtcNow.AddDays(-10)).Result, "an old copy written");
            var down = new Network((_, _) => throw new HttpRequestException("down"));
            using var offline = new FishBiteTimeService(() => TimeSpan.FromDays(1), cache, new HttpClient(down));
            offline.Get(32);
            Until(() => !offline.Get(32).Loading);
            Check(offline.Get(32) is { Error: null, Times: { } old } && old.ContainsKey(4869) && down.Calls == 1, "the expired copy taken: " + offline.Get(32));
        })),
        ("Network ends 1554: A12, Universalis: an upload time past the calendar or not a number is unknown; the comparison never stays « loading »", () =>
        {
            foreach (var time in new[] { "99999999999999999", "\"soon\"", "-5" })
            {
                var network = new Network((_, _) => "{\"itemID\":5057,\"listings\":[],\"lastUploadTime\":" + time + "}");
                using var service = new RetainerCompareService(new HttpClient(network));
                Check(service.Request("Moogle", [5057], DateTimeOffset.UtcNow), "asked");
                Until(() => !service.State.Loading);
                Check(!service.State.Loading && service.State.Error is null && service.State.Items is { } items && items[5057].DataAt is null,
                    $"lastUploadTime {time}: read, its time unknown: {service.State.Error}");
            }
            Check(RetainerSales.MaxUnixMilliseconds == DateTimeOffset.MaxValue.ToUnixTimeMilliseconds(), "the calendar's last millisecond");
        }),
        ("Network ends 1554: A26, GatherBuddy: an index without a data file is a failure, never an empty copy kept fresh", () => InTemp(root =>
        {
            using var cache = new DataCache(Path.Combine(root, "DataCache"));
            var network = new Network((_, _) => "[]");
            using var service = new GatherBuddyFishService(() => TimeSpan.FromDays(1), cache, new HttpClient(network));
            service.Request();
            Until(() => !service.State.Loading);
            Check(service.State is { Loading: false, Data: null, Error: { Length: > 0 } }, "an error tried again later: " + service.State);
            Check(cache.ReadAsync<GatherBuddyFishCache>(GatherBuddyFishService.Family, "all", GatherBuddyFishService.Version).Result is null, "nothing kept in the cache");
        })),
    ];
}
