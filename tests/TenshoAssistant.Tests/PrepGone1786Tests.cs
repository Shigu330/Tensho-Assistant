using TenshoAssistant.Reminders;

// X-777 (the review of 0.1784, I1): a chocobo companion sent away by the player with time left, then held within the grace (a teleport and
// its load, a mount, a city), was later told « has left »: what made it go is now decided when it goes, not by a hold that starts after.
internal static class PrepGone1786Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset Start = new(2026, 11, 22, 8, 0, 0, TimeSpan.Zero);

    // The companion out: Watch sees it, then readings every 5 s from `from` seconds left down to `to`; the notices said.
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

    // Readings every 5 s without the companion for `seconds`, held or not (what the watcher gives when nothing is in sight); the notices.
    private static List<PrepNotice> Away(PrepTracker tracker, ref DateTimeOffset now, int seconds, int threshold, bool held)
    {
        var said = new List<PrepNotice>();
        for (var spent = 0; spent < seconds; spent += 5)
        {
            tracker.Watch(now, false, tracker.WantsHeld(now, false) && held);
            if (tracker.Read(now, null, threshold, false, held) is { } notice) said.Add(notice);
            now = now.AddSeconds(5);
        }
        return said;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Prep gone 1786: behaviour: sent away with 20 min left, then a teleport, its load and a city within the grace: no « has left », ever", () =>
        {
            // The review's trace: sent away at t0 with 20 min left (threshold 5, nothing said yet), a teleport 6 s later (the load, then 20 s of
            // settling, then a city where it cannot be out), 30 min there, then a field where nothing holds it any more.
            var sent = new PrepTracker(PrepKind.Chocobo);
            var now = Start;
            Check(Out(sent, ref now, 21 * 60, 20 * 60, 5).Count == 0, "out with 20 min left: above the threshold, nothing said");
            Check(sent.Watch(now, false, false) == PrepSight.Went, "the frame it goes: seen going, nothing holding it");
            now = now.AddSeconds(2);
            Check(sent.Read(now, null, 5, false, held: false) is null, "the first reading without it: nothing yet");
            now = now.AddSeconds(4);
            sent.Watch(now, false, true);   // the teleport's load, 6 s after: too late to explain it
            var said = Away(sent, ref now, 30 * 60, 5, held: true);
            said.AddRange(Away(sent, ref now, 2 * 60, 5, held: false));
            Check(said.Count == 0, "sent away, then held: nothing, during or after: " + string.Join(", ", said));
            // The same without the watcher's frames: the first reading without it, held by nothing, decides as well.
            var plain = new PrepTracker(PrepKind.Chocobo);
            now = Start;
            for (var left = 21 * 60.0; left >= 20 * 60; left -= 5) { plain.Read(now, left, 5, false); now = now.AddSeconds(5); }
            Check(plain.Read(now, null, 5, false, held: false) is null, "gone, nothing holding it");
            var later = new List<PrepNotice>();
            for (var spent = 0; spent < 32 * 60; spent += 5)
            {
                now = now.AddSeconds(5);
                if (plain.Read(now, null, 5, false, held: spent < 30 * 60) is { } notice) later.Add(notice);
            }
            Check(later.Count == 0, "held only from the second reading on: still sent away: " + string.Join(", ", later));
        }),
        ("Prep gone 1786: behaviour: sent away then mounted at once (before the next reading): no « has left » after a ride longer than its time", () =>
        {
            var sent = new PrepTracker(PrepKind.Chocobo);
            var now = Start;
            Out(sent, ref now, 11 * 60, 10 * 60, 0);
            Check(sent.Watch(now, false, false) == PrepSight.Went, "sent away: nothing holds it the frame it goes");
            now = now.AddSeconds(1.5);
            Check(!sent.WantsHeld(now.AddSeconds(0.1), false), "more than a second after: the watcher no longer asks");
            sent.Watch(now, false, true);   // mounted 1.5 s after it went: not what made it go
            var said = Away(sent, ref now, 30 * 60, 0, held: true);
            said.AddRange(Away(sent, ref now, 60, 0, held: false));
            Check(said.Count == 0, "a ride of 30 min with 10 min it had: nothing: " + string.Join(", ", said));
        }),
        ("Prep gone 1786: behaviour: hidden by the mount a frame before it shows: still held, its time ending during the ride told once after", () =>
        {
            var ridden = new PrepTracker(PrepKind.Chocobo);
            var now = Start;
            Out(ridden, ref now, 4 * 60, 3 * 60, 0);
            Check(ridden.Watch(now, false, false) == PrepSight.Went, "gone a frame before the mount shows");
            // The first reading falls between the two frames: nothing holds it yet.
            now = now.AddSeconds(0.2);
            Check(ridden.Read(now, null, 0, false, held: false) is null, "first reading: nothing");
            now = now.AddSeconds(0.3);
            Check(ridden.WantsHeld(now, false), "within the second: the watcher asks");
            ridden.Watch(now, false, true);
            Check(Away(ridden, ref now, 10 * 60, 0, held: true).Count == 0, "a ride of 10 min, the time running out meanwhile: nothing while mounted");
            var after = Away(ridden, ref now, 30, 0, held: false);
            Check(after is [{ Kind: PrepKind.Chocobo, Ended: true }], "dismounted, not back: « has left », once: " + string.Join(", ", after));
            Check(Away(ridden, ref now, 60, 0, held: false).Count == 0, "not again");
            // A companion back is seen back; a reset forgets the going.
            var back = new PrepTracker(PrepKind.Chocobo);
            now = Start;
            Check(back.Watch(now, false, false) == PrepSight.Same, "never seen out: nothing new");
            Check(back.Watch(now, true, false) == PrepSight.Same && back.Watch(now, false, true) == PrepSight.Went, "out, then gone");
            Check(back.Watch(now.AddSeconds(30), true, false) == PrepSight.Back, "then back");
            back.Watch(now.AddSeconds(40), false, false);
            back.Reset();
            Check(!back.WantsHeld(now.AddSeconds(40.5), false) && back.Watch(now.AddSeconds(41), true, false) == PrepSight.Same, "after a reset: nothing kept");
        }),
        ("Prep gone 1786: the watcher sees the companion every frame, before its 5 s readings, and asks for the holds only when they matter", () =>
        {
            var watcher = Read("src/TenshoAssistant/Reminders/PrepReminderWatcher.Dalamud.cs");
            var frame = watcher.IndexOf("if(client.IsLoggedIn)WatchCompanion(now,territory,loading);", StringComparison.Ordinal);
            var throttle = watcher.IndexOf("if(now<nextRead)return;", StringComparison.Ordinal);
            Check(frame > 0 && throttle > frame, "every frame, before the 5 s readings");
            Check(watcher.Contains("var held=chocobo.WantsHeld(now,outNow) && (loading || Held(now,territory));", StringComparison.Ordinal)
                  && watcher.Contains("var sight=chocobo.Watch(now,outNow,held);", StringComparison.Ordinal), "Watch, with the holds only when asked");
            Check(watcher.Contains("if(loading)settling.Loaded(now);", StringComparison.Ordinal), "a load seen at once (X-781: PrepSettling)");
            Check(watcher.Contains("sightLogs>=SightLogs", StringComparison.Ordinal), "a few log lines per load of the plugin");
        }),
    ];
}
