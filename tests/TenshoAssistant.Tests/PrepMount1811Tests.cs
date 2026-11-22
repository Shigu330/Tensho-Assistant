using TenshoAssistant.Reminders;

// X-798 (0.1811, the user's reading of 22/11, test T-0.1786-2: « the timer stops »): while mounted the game's companion timer is frozen; a
// ride longer than the time left gives no false « has left » nor « ends » at the dismount (behaviour, R28.14).
internal static class PrepMount1811Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset Start = new(2026, 11, 22, 13, 0, 0, TimeSpan.Zero);

    // Readings every 5 s with the companion out, from `from` seconds left down to `to`.
    private static List<PrepNotice> Out(PrepTracker tracker, ref DateTimeOffset now, double from, double to, int threshold)
    {
        var said = new List<PrepNotice>();
        for (var left = from; left >= to; left -= 5)
        {
            tracker.Watch(now, true, false);
            if (tracker.Read(now, left, threshold, false) is { } notice) said.Add(notice);
            now = now.AddSeconds(5);
        }
        return said;
    }

    // Readings every 5 s without the companion: held or not, mounted (frozen) or not.
    private static List<PrepNotice> Away(PrepTracker tracker, ref DateTimeOffset now, int seconds, int threshold, bool held, bool frozen)
    {
        var said = new List<PrepNotice>();
        for (var spent = 0; spent < seconds; spent += 5)
        {
            tracker.Watch(now, false, tracker.WantsHeld(now, false) && held);
            if (tracker.Read(now, null, threshold, false, held, frozen) is { } notice) said.Add(notice);
            now = now.AddSeconds(5);
        }
        return said;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Prep mount 1811: behaviour: 4 min left, a 20-minute ride, the dismount: no « has left », and its time back counts on", () =>
        {
            var chocobo = new PrepTracker(PrepKind.Chocobo);
            var now = Start;
            // Out with 6 min left, threshold 5: « ends in 5 min » said once on the way down to 4 min.
            var said = Out(chocobo, ref now, 6 * 60, 4 * 60, 5);
            Check(said.Count == 1 && !said[0].Ended, "the threshold said once: " + string.Join(", ", said));
            // Mounted: it goes out of sight, held and frozen, for 20 minutes (much longer than the 4 left).
            said = Away(chocobo, ref now, 20 * 60, 5, held: true, frozen: true);
            Check(said.Count == 0, "nothing during the ride");
            // The dismount: nothing holds it; the game brings it back a few seconds later, with the 4 minutes it had.
            said = Away(chocobo, ref now, 10, 5, held: false, frozen: false);
            Check(said.Count == 0, "the dismount: nothing said within the grace");
            said = Out(chocobo, ref now, 4 * 60 - 5, 3 * 60, 5);
            Check(said.Count == 0, "back with its time: neither « ends » again (said already) nor « has left »: " + string.Join(", ", said));
        }),
        ("Prep mount 1811: behaviour: not back after the ride: it still had time, nothing said (never a false « has left »)", () =>
        {
            var chocobo = new PrepTracker(PrepKind.Chocobo);
            var now = Start;
            Out(chocobo, ref now, 2 * 60, 60, 0);
            var said = Away(chocobo, ref now, 20 * 60, 0, held: true, frozen: true);
            said.AddRange(Away(chocobo, ref now, 2 * 60, 0, held: false, frozen: false));
            Check(said.Count == 0, "a minute left when it went, 20 min mounted: frozen, so gone with time left: nothing: " + string.Join(", ", said));
        }),
        ("Prep mount 1811: behaviour: held without a mount (swimming): the time goes on as before, its end told once", () =>
        {
            var chocobo = new PrepTracker(PrepKind.Chocobo);
            var now = Start;
            Out(chocobo, ref now, 2 * 60, 60, 0);
            var said = Away(chocobo, ref now, 20 * 60, 0, held: true, frozen: false);
            said.AddRange(Away(chocobo, ref now, 2 * 60, 0, held: false, frozen: false));
            Check(said is [{ Ended: true }], "20 min held, not mounted, a minute left: it ran out during the absence, « has left » once (as in 0.1786): " + string.Join(", ", said));
            var near = new PrepTracker(PrepKind.Chocobo);
            now = Start;
            Out(near, ref now, 60, 30, 0);
            said = Away(near, ref now, 25, 0, held: true, frozen: false);
            said.AddRange(Away(near, ref now, 30, 0, held: false, frozen: false));
            Check(said is [{ Ended: true }], "held, not mounted, ending during the absence: « has left » once, as before: " + string.Join(", ", said));
        }),
        ("Prep mount 1811: the watcher gives « frozen » for the mount only; the evidence says it with the reading", () =>
        {
            var watcher = Read("src/TenshoAssistant/Reminders/PrepReminderWatcher.Dalamud.cs");
            Check(watcher.Contains("busy,!outNow && Held(now,territory),!outNow && Mounted()));") && watcher.Contains("private bool Mounted()=>") && watcher.Contains("        Mounted()\n        || condition[ConditionFlag.Swimming]"), "frozen = mounted; held keeps the mount and the rest");
            var source = Read("src/TenshoAssistant/Reminders/PrepReminders.cs");
            Check(source.Contains("var measuredAt=goneHeld ? now-frozenFor-(frozenSince is { } still ? now-still:TimeSpan.Zero):goneAt.Value;"), "the time mounted taken off");
            var evidence = Read("docs/evidence/0.1780-companion-away.md");
            Check(evidence.Contains("## 0.1811: the companion's timer is frozen while mounted (X-798)") && evidence.Contains("## 0.1811 : le minuteur du compagnon est figé pendant la monture (X-798)") && evidence.Contains("T-0.1786-2"), "both halves, the reading cited");
        }),
    ];
}
