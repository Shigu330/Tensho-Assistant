using TenshoAssistant.Reminders;

// X-774 (the review of 0.1779, I1): the chocobo companion out of sight for a while (mounted, a city where it cannot be out, just after a zone
// change) is not a companion sent away: a reminder already said is not said again when it comes back, and its time running out during the
// absence is still told once (« has left ») when nothing was said for this end.
internal static class PrepAway1780Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset Start = new(2026, 11, 22, 6, 0, 0, TimeSpan.Zero);

    // Readings every 5 s from `from` seconds left down to `to`, the time going on; the notices said.
    private static List<PrepNotice> Run(PrepTracker tracker, ref DateTimeOffset now, double from, double to, int threshold)
    {
        var said = new List<PrepNotice>();
        for (var left = from; left >= to; left -= 5)
        {
            if (tracker.Read(now, left, threshold, false) is { } notice) said.Add(notice);
            now = now.AddSeconds(5);
        }
        return said;
    }

    // Readings every 5 s without the companion for `seconds`, held or not; the notices said.
    private static List<PrepNotice> Away(PrepTracker tracker, ref DateTimeOffset now, int seconds, int threshold, bool held)
    {
        var said = new List<PrepNotice>();
        for (var spent = 0; spent < seconds; spent += 5)
        {
            if (tracker.Read(now, null, threshold, false, held) is { } notice) said.Add(notice);
            now = now.AddSeconds(5);
        }
        return said;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Prep away 1780: behaviour: mounting and dismounting under the threshold after the reminder does not say « ends in n min » again", () =>
        {
            var chocobo = new PrepTracker(PrepKind.Chocobo);
            var now = Start;
            var said = Run(chocobo, ref now, 6 * 60, 4 * 60, 5);
            Check(said.Count == 1 && said[0] == new PrepNotice(PrepKind.Chocobo, false, 5), "the reminder once at 5 min: " + string.Join(", ", said));
            var left = 4 * 60.0 - 5;
            for (var ride = 0; ride < 3; ride++)
            {
                // A ride of 30 s (the companion out of sight, held), then 5 s on foot before it is back, its timer gone on.
                said = Away(chocobo, ref now, 30, 5, held: true);
                said.AddRange(Away(chocobo, ref now, 5, 5, held: false));
                left -= 35;
                said.AddRange(Run(chocobo, ref now, left, left - 20, 5));
                left -= 25;
                Check(said.Count == 0, $"ride {ride + 1}: nothing said again: " + string.Join(", ", said));
            }
            // A pause the game makes in its timer while out of sight is the same end too: nothing again.
            Away(chocobo, ref now, 60, 5, held: true);
            Check(Run(chocobo, ref now, left, left - 10, 5).Count == 0, "back with the time it had: the same end");
            // Out of sight with nothing to explain it (a case not known), decided « sent away », then back with less time: still the same end.
            var unknown = new PrepTracker(PrepKind.Chocobo);
            now = Start;
            Run(unknown, ref now, 5 * 60, 4 * 60, 5);
            Check(Away(unknown, ref now, 60, 5, held: false).Count == 0, "gone with 4 min left, nothing holding it: nothing said");
            Check(Run(unknown, ref now, 3 * 60, 2 * 60, 5).Count == 0, "back with less time than before: not said again");
            // More gysahl greens: more time than the last reading left, a new end armed again.
            Check(Run(unknown, ref now, 34 * 60, 4 * 60 + 30, 5) is [{ Ended: false, Minutes: 5 }], "more greens: a new end, said once at the threshold");
        }),
        ("Prep away 1780: behaviour: the time running out while out of sight is told once after it (threshold 0, or mounted before the threshold)", () =>
        {
            var noThreshold = new PrepTracker(PrepKind.Chocobo);
            var now = Start;
            Check(Run(noThreshold, ref now, 3 * 60, 2 * 60, 0).Count == 0, "threshold 0: nothing while it lasts");
            Check(Away(noThreshold, ref now, 5 * 60, 0, held: true).Count == 0, "a ride of 5 min, the time running out meanwhile: nothing decided while mounted");
            var after = Away(noThreshold, ref now, 30, 0, held: false);
            Check(after.Count == 1 && after[0] == new PrepNotice(PrepKind.Chocobo, true, 0), "dismounted, not back: « has left », once: " + string.Join(", ", after));
            Check(Away(noThreshold, ref now, 60, 0, held: false).Count == 0, "not again");
            var early = new PrepTracker(PrepKind.Chocobo);
            now = Start;
            Check(Run(early, ref now, 8 * 60, 6 * 60, 5).Count == 0, "mounted with 6 min left, above the threshold of 5");
            Away(early, ref now, 10 * 60, 5, held: true);
            Check(Away(early, ref now, 30, 5, held: false) is [{ Ended: true }], "a ride of 10 min: « has left » after it, once");
            // In a city (held while there) the time runs out: told once out of it, not while there.
            var city = new PrepTracker(PrepKind.Chocobo);
            now = Start;
            Run(city, ref now, 7 * 60, 6 * 60, 5);
            Check(Away(city, ref now, 20 * 60, 5, held: true).Count == 0, "in the city: nothing");
            Check(Away(city, ref now, 30, 5, held: false) is [{ Ended: true }], "out of the city: « has left »");
            // Back after the ride with time still left (the timer stopped meanwhile): no false « has left », the reminder at the threshold.
            var paused = new PrepTracker(PrepKind.Chocobo);
            now = Start;
            Run(paused, ref now, 8 * 60, 6 * 60, 5);
            Away(paused, ref now, 10 * 60, 5, held: true);
            var back = Away(paused, ref now, 5, 5, held: false);
            back.AddRange(Run(paused, ref now, 6 * 60, 4 * 60, 5));
            Check(back is [{ Ended: false, Minutes: 5 }], "back with its time: no « has left », « ends in 5 min » once: " + string.Join(", ", back));
        }),
        ("Prep away 1780: behaviour: sent away by the player with time left: still nothing said, even after the time it had; a meal is decided at once", () =>
        {
            var sent = new PrepTracker(PrepKind.Chocobo);
            var now = Start;
            Run(sent, ref now, 25 * 60, 20 * 60, 5);
            Check(Away(sent, ref now, 40 * 60, 5, held: false).Count == 0, "sent away with 20 min left: nothing, now or later");
            var nearly = new PrepTracker(PrepKind.Chocobo);
            now = Start;
            Run(nearly, ref now, 60, 40, 0);
            Check(Away(nearly, ref now, 30, 0, held: false).Count == 0, "sent away with 35 s left, more than 15 s: the player's choice");
            var meal = new PrepTracker(PrepKind.Meal);
            now = Start;
            Run(meal, ref now, 60, 5, 0);
            Check(meal.Read(now, null, 0, false) == new PrepNotice(PrepKind.Meal, true, 0), "a meal's status gone by itself: told at once (no grace)");
        }),
        ("Prep away 1780: the watcher holds the companion while mounted, swimming, in a duty or a cutscene, just after a load, or where mounts are not allowed", () =>
        {
            var watcher = Read("src/TenshoAssistant/Reminders/PrepReminderWatcher.Dalamud.cs");
            Check(watcher.Contains("Say(chocobo.Read(now,outNow ? info.TimeLeft:null,options.ChocoboThreshold,busy,!outNow && Held(now,territory)));", StringComparison.Ordinal), "held given when out of sight");
            foreach (var flag in new[] { "Mounted", "RidingPillion", "Mounting", "InFlight", "Diving", "BoundByDuty", "WatchingCutscene", "OccupiedInCutSceneEvent" })
                Check(watcher.Contains($"condition[ConditionFlag.{flag}]", StringComparison.Ordinal), "held: " + flag);
            Check(watcher.Contains("|| settling.Holds(now) || MountAllowed?.Invoke(territory)==false", StringComparison.Ordinal), "held just after a load (X-781: until it is seen back, PrepSettling) and where mounts are not allowed");
            Check(watcher.Contains("if(territory!=lastTerritory){lastTerritory=territory;settling.Loaded(now);}", StringComparison.Ordinal), "a zone change starts the settling time");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("MountAllowed=territory=>territory==0 ? null:data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().GetRowOrDefault(territory)?.Mount}", StringComparison.Ordinal), "TerritoryType.Mount");
        }),
    ];
}
