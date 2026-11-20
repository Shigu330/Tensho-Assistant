using System.Net;
using Microsoft.Data.Sqlite;
using TenshoAssistant.Community;
using TenshoAssistant.Discoveries;
using TenshoAssistant.Market;

// 0.1560 (X-619, review of 0.1557, minor findings): behaviour tests (R28.14). M1 a damaged row of your discoveries set aside, so the
// sharing is blocked for one session only; M2 MarketSeen.json held at the start, read once free; M3 an item page that failed asked again.
internal static class AfterError1560Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Until(Func<bool> done) { for (var wait = 0; !done() && wait < 500; wait++) Thread.Sleep(10); }

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1560-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } }
    }

    private static void Sql(string root, string sql)
    {
        using var open = new SqliteConnection("Data Source=" + Path.Combine(root, DiscoveryDatabase.FileName) + ";Pooling=False");
        open.Open();
        using var command = open.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private sealed class Network(Func<int, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(answer(Interlocked.Increment(ref Calls)));
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("After error 1560: M1, a damaged row of yours is set aside after the read: this session incomplete, the next one whole, the text kept", () => InTemp(root =>
        {
            var database = new DiscoveryDatabase(Path.Combine(root, DiscoveryDatabase.FileName));
            Sql(root, "INSERT INTO facts(key,kind,shared,fact) VALUES('a',1,0,'{ damaged');");
            var first = new DiscoveryStore();
            var broken = new List<string>();
            database.ReadMine(DiscoveryStart.Incomplete(first, (key, _) => broken.Add(key)));
            Check(first.MineIncomplete && broken.SequenceEqual(["a"]), "this session: incomplete, the row said");
            Check(database.SetAside(broken, DateTimeOffset.UtcNow) == 1 && database.CountAside() == 1, "the row moved into facts_aside");
            var next = new DiscoveryStore();
            database.ReadMine(DiscoveryStart.Incomplete(next, (_, _) => { }));
            Check(!next.MineIncomplete && database.Count().Mine == 0, "the next start: your facts read whole, the sharing no longer blocked");
            Check(database.SetAside(["a"], DateTimeOffset.UtcNow) == 0 && database.CountAside() == 1, "asked again: nothing moved twice");
        })),
        ("After error 1560: M1, a row written again readable by the other client between the read and the set-aside stays", () => InTemp(root =>
        {
            var database = new DiscoveryDatabase(Path.Combine(root, DiscoveryDatabase.FileName));
            Sql(root, "INSERT INTO facts(key,kind,shared,fact) VALUES('b',1,0,'{ damaged');");
            var broken = new List<string>();
            database.ReadMine((key, _) => broken.Add(key));
            Sql(root, "UPDATE facts SET fact='{}' WHERE key='b';");
            Check(database.SetAside(broken, DateTimeOffset.UtcNow) == 0 && database.Count().Mine == 1 && database.CountAside() == 0, "readable now: left in facts");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs"));
            Check(plugin.Contains("opened.SetAside(aside,DateTimeOffset.UtcNow)") && plugin.Contains("lock(unreadable)unreadable.Add(key);"), "the start sets them aside after the read");
        })),
        ("After error 1560: M2, MarketSeen.json held at the start is read once another program lets it go", () => InTemp(root =>
        {
            var path = Path.Combine(root, "MarketSeen.json");
            File.WriteAllText(path, "[]");
            List<SeenMarketEntry>? read;
            using (var hold = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var left = false;
                Check(SeenPriceStore.ReadAtStart(path, DateTimeOffset.UtcNow, (_, _) => { }, _ => left = true, tries: 1, waitMs: 0) is null && left, "held: left where it is");
                var later = SeenPriceStore.ReadWhenFree(path, TimeSpan.FromMilliseconds(50), 40, CancellationToken.None);
                Thread.Sleep(200);
                Check(!later.IsCompleted, "still held: still waiting");
                hold.Dispose();
                read = later.Result;
            }
            Check(read is { Count: 0 } && File.Exists(path), "read once free, the file in place");
            Check(SeenPriceStore.ReadWhenFree(Path.Combine(root, "gone.json"), TimeSpan.Zero, 3, CancellationToken.None).Result is null, "a file gone: nothing, no wait for ever");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs"));
            Check(plugin.Contains("if(held)_=ReadSeenPricesLater(seenPath,seenPrices);") && plugin.Contains("seenPrices.Load(later);"), "the start reads it again and joins it");
        })),
        ("After error 1560: M3, an item page that failed is kept a while, then asked again and read", () =>
        {
            var network = new Network(call => call == 1 ? new HttpResponseMessage(HttpStatusCode.InternalServerError) : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
            var now = DateTimeOffset.UtcNow;
            using var service = new WikiItemAcquisitionService(id => "Item " + id, () => TimeSpan.FromDays(1), null, network) { Gap = TimeSpan.Zero, Clock = () => now };
            service.Get(7);
            Until(() => service.Peek(7) is { Loading: false });
            Check(service.Peek(7) is { Error: not null, FailedAt: not null } && network.Calls == 1, "first read failed: " + service.Peek(7));
            Check(service.Get(7).Error is not null && network.Calls == 1, "within the delay: the error kept, nothing asked");
            now += service.RetryAfter;
            Check(service.Get(7).Loading, "after the delay: asked again");
            Until(() => service.Peek(7) is { Loading: false });
            Check(service.Peek(7) is { Error: null, FailedAt: null } && network.Calls == 2, "read the second time: " + service.Peek(7));
        }),
    ];
}
