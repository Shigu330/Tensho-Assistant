using TenshoAssistant.Diagnostics;
using TenshoAssistant.Discoveries;

// 0.1450 (X-433): the item discoveries' watcher read the bags ten times a second into two new tables and compared them with LINQ (8.6 MB
// of dictionary entries on the game's thread in the two minutes after a load). It fills again the tables of the read before last and
// compares them with plain loops: the same gains and losses, and next to nothing allocated once the tables exist.
internal static class BagCounts1450Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Bag counts 1450: the loops give the gains and losses of before, item by item", () =>
        {
            var random = new Random(1450);
            for (var round = 0; round < 200; round++)
            {
                var before = new Dictionary<uint, long>();
                var after = new Dictionary<uint, long>();
                for (var item = 1u; item <= 30; item++)
                {
                    if (random.Next(3) > 0) before[item] = random.Next(1, 999);
                    if (random.Next(3) > 0) after[item] = random.Next(1, 999);
                }
                IReadOnlyDictionary<uint, long> oldBefore = before, oldAfter = after;   // the LINQ forms of before
                var gains = ItemGains.Gains(before, after);
                var losses = ItemGains.Losses(before, after);
                var oldGains = ItemGains.Gains(oldBefore, oldAfter);
                var oldLosses = ItemGains.Losses(oldBefore, oldAfter);
                Check(gains.Count == oldGains.Count && gains.All(pair => oldGains.TryGetValue(pair.Key, out var value) && value == pair.Value), "gains, round " + round);
                Check(losses.Count == oldLosses.Count && losses.All(pair => oldLosses.TryGetValue(pair.Key, out var value) && value == pair.Value), "losses, round " + round);
            }
            var bags = new Dictionary<uint, long> { [5] = 3, [7] = 1 };
            Check(ItemGains.Gains(bags, new Dictionary<uint, long> { [5] = 3, [7] = 1 }).Count == 0, "nothing changed: nothing");
            Check(ItemGains.SingleSpent(ItemGains.Losses(bags, new Dictionary<uint, long> { [5] = 3 })) == 7, "one unit spent");
        }),
        ("Bag counts 1450: comparing the same bags again allocates only the two empty answers", () =>
        {
            var before = new Dictionary<uint, long>();
            var after = new Dictionary<uint, long>();
            for (var item = 1u; item <= 140; item++) { before[item] = item; after[item] = item; }
            ItemGains.Gains(before, after); ItemGains.Losses(before, after);
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (var round = 0; round < 100; round++) { ItemGains.Gains(before, after); ItemGains.Losses(before, after); }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            Check(allocated < 200 * 120, "200 comparisons of 140 items: " + allocated + " bytes (an empty table each)");
        }),
        ("Bag counts 1450: the watcher fills the tables of the read before last; the other threads list sixteen types", () =>
        {
            var watcher = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs")).Replace("\r\n", "\n");
            Check(watcher.Contains("var counts=spareAll ?? new Dictionary<uint,long>();var hq=spareHq ?? new Dictionary<uint,long>();\n        spareAll=null;spareHq=null;\n        counts.Clear();hq.Clear();", StringComparison.Ordinal), "Counts reuses the spare tables");
            Check(watcher.Contains("spareAll=before;before=counts;", StringComparison.Ordinal) && watcher.Contains("spareHq=beforeHq;beforeHq=hqCounts;", StringComparison.Ordinal), "the tables replaced become the spare ones, after the comparison");
            var types = new AllocationTypes(kept: 1, keptOther: 3);
            foreach (var (name, thread) in new[] { ("A", 5L), ("B", 5L), ("C", 5L), ("D", 5L), ("E", 7L), ("F", 7L) }) types.See(name, 1_048_576, thread, 7, 9);
            var summary = types.Summary(TimeSpan.FromMinutes(2));
            Check(summary.Contains("the game's thread 2.0 MB (E 1.0 MB)", StringComparison.Ordinal) && summary.Contains("other threads 4.0 MB (A 1.0 MB, B 1.0 MB, C 1.0 MB)", StringComparison.Ordinal), summary);
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/RuntimeSuspensions.cs")).Contains("private readonly AllocationTypes allocations=new(keptOther:16);", StringComparison.Ordinal), "sixteen in the plugin");
        }),
    ];
}
