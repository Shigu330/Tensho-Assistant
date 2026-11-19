using System.Net;
using Microsoft.Data.Sqlite;
using TenshoAssistant.Beastmaster;
using TenshoAssistant.Caching;
using TenshoAssistant.Community;
using TenshoAssistant.Discoveries;
using TenshoAssistant.Market;

// 0.1557 (X-618, review of block 0.1551 to 0.1554, minor findings): behaviour tests (R28.14). M1 a damaged plans' file that cannot be moved,
// M2 MarketSeen.json held at the start, M3 Teamcraft's expired copy asked again, M4 a fresh GatherBuddy copy without a fish, M5 a row of
// your discoveries that cannot be read.
internal static class AfterError1557Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Until(Func<bool> done) { for (var wait = 0; !done() && wait < 500; wait++) Thread.Sleep(10); }
    private static readonly DateTimeOffset At = DateTimeOffset.UtcNow.AddHours(-1);

    private sealed class Network(Func<int, HttpRequestMessage, string> answer) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(answer(Interlocked.Increment(ref Calls), request)) });
    }

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1557-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } }
    }

    private const string Fish = "data.Apply(43803, Patch.Dawntrail) // Turali Loach\n            .Spear(data, SpearfishSize.Small, SpearfishSpeed.ExtremelySlow);";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("After error 1557: M1, a damaged plans' file that cannot be moved is not said set aside; said once, when it is", () => InTemp(root =>
        {
            var file = Path.Combine(root, "BeastTeams.json");
            File.WriteAllText(file, "{ damaged");
            var reader = new BeastPlansFile();
            var said = new List<string>();
            // Readable but not movable: another program holds it, sharing reading only.
            using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Check(reader.ReadIfDue(file, At, (aside, _) => said.Add(aside)) is null && reader.Blocked && !reader.MayWrite, "not moved: blocked");
                Check(reader.ReadIfDue(file, At + BeastPlansFile.RetryEvery, (aside, _) => said.Add(aside)) is null && reader.Blocked, "still not moved");
            }
            Check(said.Count == 0 && File.Exists(file), "nothing said set aside while it is not: " + string.Join(",", said));
            reader.ReadIfDue(file, At + BeastPlansFile.RetryEvery * 2, (aside, _) => said.Add(aside));
            Check(said.Count == 1 && said[0] == reader.SetAside && File.Exists(said[0]) && !File.Exists(file) && reader.MayWrite, "let go: set aside, said once");
        })),
        ("After error 1557: M2, MarketSeen.json held at the start is read again, then left where it is; damaged, set aside and said once moved", () => InTemp(root =>
        {
            var path = Path.Combine(root, "MarketSeen.json");
            var entry = new SeenMarketEntry { World = 40, Item = 5057, Listings = [new SeenListing(100, false, 1)], ListingsAt = DateTimeOffset.UtcNow };
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new List<SeenMarketEntry> { entry }));
            var asides = new List<string>(); var left = 0;
            List<SeenMarketEntry>? read;
            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                read = SeenPriceStore.ReadAtStart(path, At, (aside, _) => asides.Add(aside), _ => left++, tries: 2, waitMs: 10);
            Check(read is null && left == 1 && asides.Count == 0 && File.Exists(path) && Directory.GetFiles(root).Length == 1, "held: left where it is, nothing set aside");
            // Released while it is read again: read.
            var holder = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            var release = Task.Run(() => { Thread.Sleep(50); holder.Dispose(); });
            read = SeenPriceStore.ReadAtStart(path, At, (aside, _) => asides.Add(aside), _ => left++, tries: 50, waitMs: 20);
            release.Wait();
            Check(read is [{ Item: 5057 }] && left == 1, "let go while read again: the prices seen read");
            File.WriteAllText(path, "[ damaged");
            Check(SeenPriceStore.ReadAtStart(path, At, (aside, _) => asides.Add(aside), _ => left++) is null && asides.Count == 1 && File.ReadAllText(asides[0]) == "[ damaged" && !File.Exists(path),
                "damaged: set aside, said once moved");
        })),
        ("After error 1557: M3, Teamcraft unreachable with an expired copy: the copy shown, then asked again after the deadline", () => InTemp(root =>
        {
            using var cache = new DataCache(Path.Combine(root, "DataCache"));
            Check(cache.WriteAsync("teamcraft-bites", "32", "v1", new FishBiteCache([[4869, 20, 7]]), DateTimeOffset.UtcNow.AddDays(-10)).Result, "an old copy written");
            const string Answer = "{\"data\":{\"biteTimes\":[{\"itemId\":4870,\"flooredBiteTime\":12,\"occurences\":5}]}}";
            var network = new Network((call, _) => call == 1 ? throw new HttpRequestException("down") : Answer);
            using var service = new FishBiteTimeService(() => TimeSpan.FromDays(1), cache, new HttpClient(network)) { RetryMs = 100_000 };
            service.Get(32);
            Until(() => !service.Get(32).Loading);
            Check(service.Get(32) is { Error: null, Times: { } old } && old.ContainsKey(4869) && network.Calls == 1, "the expired copy shown");
            Thread.Sleep(30);
            Check(service.Get(32).Times!.ContainsKey(4869) && network.Calls == 1, "not asked again before the deadline");
            service.RetryMs = 0;
            Check(service.Get(32) is { Loading: true, Times: { } shown } && shown.ContainsKey(4869), "asked again, the copy still shown meanwhile");
            Until(() => service.Get(32) is { Loading: false, Times: { } now } && now.ContainsKey(4870));
            Check(service.Get(32).Times is { } times && times.ContainsKey(4870) && network.Calls == 2, "asked again after the deadline: the new times");
        })),
        ("After error 1557: M4, GatherBuddy: a fresh copy without a fish (kept before 0.1554) is downloaded again, never taken as empty", () => InTemp(root =>
        {
            using var cache = new DataCache(Path.Combine(root, "DataCache"));
            Check(cache.WriteAsync(GatherBuddyFishService.Family, "all", GatherBuddyFishService.Version, new GatherBuddyFishCache("// no fish"), DateTimeOffset.UtcNow).Result, "an empty fresh copy");
            var network = new Network((_, request) => request.RequestUri!.AbsoluteUri.Contains("api.github.com", StringComparison.Ordinal) ? "[{\"name\":\"Data7.0.cs\"}]" : Fish);
            using var service = new GatherBuddyFishService(() => TimeSpan.FromDays(1), cache, new HttpClient(network));
            service.Request();
            Until(() => !service.State.Loading);
            Check(service.State is { Error: null, Data: { Count: > 0 } } && network.Calls == 2, "downloaded again: " + service.State + " " + network.Calls);
            Check(cache.ReadAsync<GatherBuddyFishCache>(GatherBuddyFishService.Family, "all", GatherBuddyFishService.Version).Result?.Value.Text.Contains("43803") == true, "the copy replaced");
        })),
        ("After error 1557: M5, a row of your discoveries that cannot be read marks them incomplete: nothing sent to your gist", () => InTemp(root =>
        {
            var database = new DiscoveryDatabase(Path.Combine(root, DiscoveryDatabase.FileName));
            using (var open = new SqliteConnection("Data Source=" + Path.Combine(root, DiscoveryDatabase.FileName)))
            {
                open.Open();
                using var insert = open.CreateCommand();
                insert.CommandText = "INSERT INTO facts(key,kind,shared,fact) VALUES('a',1,0,'{ damaged');";
                insert.ExecuteNonQuery();
            }
            var store = new DiscoveryStore();
            var said = new List<string>();
            var file = database.ReadMine(DiscoveryStart.Incomplete(store, (key, _) => said.Add(key)));
            Check(file.Facts.Count == 0 && said.SequenceEqual(["a"]) && store.MineIncomplete, "the damaged row said, your facts incomplete");
            var clean = new DiscoveryStore();
            database.ReadMine(DiscoveryStart.Incomplete(clean, (_, _) => { }));
            using (var open = new SqliteConnection("Data Source=" + Path.Combine(root, DiscoveryDatabase.FileName)))
            {
                open.Open();
                using var delete = open.CreateCommand();
                delete.CommandText = "DELETE FROM facts;";
                delete.ExecuteNonQuery();
            }
            var whole = new DiscoveryStore();
            database.ReadMine(DiscoveryStart.Incomplete(whole, (_, _) => { }));
            Check(!whole.MineIncomplete, "a base read whole: not incomplete");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs"));
            Check(plugin.Contains("opened.ReadMine(Discoveries.DiscoveryStart.Incomplete(discoveries,", StringComparison.Ordinal), "the start marks your facts incomplete");
        })),
    ];
}
