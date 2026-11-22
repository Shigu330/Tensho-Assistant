using TenshoAssistant.Craft;
using TenshoAssistant.Gear;

// X-780 (the review of 0.1789, M2): the meal eaten and the stats are each read once a second, each at its own pace; just after the meal seen
// changed, stats read before they follow it still gave a value (one nearly always exists) and the « With meal » column showed a wrong value
// for a second. The column now waits for stats read at least a second after the change was seen (GearMealWatch).
internal static class GearSettle1790Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset Start = new(2026, 11, 22, 9, 0, 0, TimeSpan.Zero);

    // Critical hit +10 % (max 90), as in GearEaten1787Tests.
    private static readonly CraftConsumable Eaten = new(44091, "Tarte", false, [new CraftBonus(27, true, 10, 90, 12, 110)], 3001);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gear settle 1790: behaviour: stats read before they hold the meal still give a value: why the column must wait", () =>
        {
            // Critical hit 2000 without the meal; the status seen, the stats read not holding it yet: 2000 read. 1910 + 90 (capped) = 2000:
            // a value is found back, 90 too low.
            var stale = new Dictionary<string, int> { ["CriticalHit"] = 2000 };
            Check(GearMeals.WithoutActive(stale, stale, Eaten, false)?.Values["CriticalHit"] == 1910, "a value found back from stats without the meal: 1910, not 2000");
        }),
        ("Gear settle 1790: behaviour: after the meal seen changes, the stats read count only once read at least a second after", () =>
        {
            var watch = new GearMealWatch();
            watch.Seen(3001, Start);
            Check(watch.Settled(Start.AddSeconds(-0.5)), "the first meal seen is not a change: the stats read count");
            watch.Seen(3001, Start.AddSeconds(1));
            Check(watch.Settled(Start.AddSeconds(0.5)), "the same meal again: still nothing to wait for");
            watch.Seen(13002, Start.AddSeconds(2));
            Check(!watch.Settled(Start.AddSeconds(1.5)) && !watch.Settled(Start.AddSeconds(2.4)), "another meal: stats read before, or less than a second after, do not count");
            Check(watch.Settled(Start.AddSeconds(3)), "stats read a second after: they follow it");
            watch.Seen(0, Start.AddSeconds(10));
            Check(!watch.Settled(Start.AddSeconds(10.3)), "the meal ended: stats still holding its bonus would add two meals: wait");
            Check(watch.Settled(Start.AddSeconds(11.2)), "then they count again");
            watch.Seen(-1, Start.AddSeconds(20));
            Check(!watch.Settled(Start.AddSeconds(20.5)) && !watch.Settled(null), "not read any more, or no stats: wait");
        }),
        ("Gear settle 1790: the planner tells the watch each meal read and shows « — » while the stats read do not follow it", () =>
        {
            var model = Read("src/TenshoAssistant/MainWindow.GearMeal.cs");
            Check(model.Contains("gearMealWatch.Seen(gearMealEaten,now);", StringComparison.Ordinal), "each meal read, once a second");
            Check(model.Contains("var waiting=choice is { ItemId:not 0 } && fromGame && !gearMealWatch.Settled(characterStats.State.ObservedAt);", StringComparison.Ordinal), "the stats read must follow it");
            Check(model.Contains("cached.Key.Waiting==key.Item6", StringComparison.Ordinal) && model.Contains("var unknown=key.Item6;", StringComparison.Ordinal), "in the cache's key; unknown while waiting");
        }),
    ];
}
