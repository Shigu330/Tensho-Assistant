using TenshoAssistant.Reminders;

// X-781 (the review of 0.1789, M1): a chocobo companion sent away by the player within the 20 seconds after a load was taken for held (the
// load's settling time), and a mount just after kept it held for the whole ride, then a false « has left ». The settling now ends once the
// companion has been seen back after the load (PrepSettling).
internal static class PrepSettle1790Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly DateTimeOffset Start = new(2026, 11, 22, 9, 0, 0, TimeSpan.Zero);

    // The watcher, frame by frame (every 0.1 s): the load's frames, the companion out or not, mounted or not; its readings every 5 s.
    private sealed class Watcher(bool oldSettling)
    {
        public readonly PrepTracker Tracker = new(PrepKind.Chocobo);
        public readonly PrepSettling Settling = new();
        public readonly List<PrepNotice> Said = [];
        private DateTimeOffset loadedAt = DateTimeOffset.MinValue, nextRead = DateTimeOffset.MinValue;

        // What 0.1789 did (oldSettling): held within 20 s of a load, seen back or not.
        private bool Holds(DateTimeOffset now) => oldSettling ? now - loadedAt < PrepSettling.Settling : Settling.Holds(now);

        public void Frames(ref DateTimeOffset now, double seconds, bool outNow, double left, bool loading = false, bool mounted = false)
        {
            for (var spent = 0.0; spent < seconds; spent += 0.1)
            {
                if (loading) { Settling.Loaded(now); loadedAt = now; }
                Settling.Seen(now, outNow);
                var held = Tracker.WantsHeld(now, outNow) && (loading || mounted || Holds(now));
                Tracker.Watch(now, outNow, held);
                if (now >= nextRead && !loading)
                {
                    nextRead = now.AddSeconds(5);
                    if (Tracker.Read(now, outNow ? left - spent : null, 0, false, !outNow && (mounted || Holds(now))) is { } notice) Said.Add(notice);
                }
                now = now.AddSeconds(0.1);
            }
        }
    }

    // The review's trace: out with 4 min left; a teleport (3 s of load); back 5 s after it; sent away 10 s after the load with 3 min 40 s
    // left; mounted 2 s later for a 10 min ride; dismounted, not called back.
    private static List<PrepNotice> Trace(bool oldSettling)
    {
        var watcher = new Watcher(oldSettling);
        var now = Start;
        watcher.Frames(ref now, 10, true, 240);
        watcher.Frames(ref now, 3, false, 0, loading: true);
        watcher.Frames(ref now, 5, false, 0);
        watcher.Frames(ref now, 5, true, 225);
        watcher.Frames(ref now, 2, false, 0);
        watcher.Frames(ref now, 10 * 60, false, 0, mounted: true);
        watcher.Frames(ref now, 60, false, 0);
        return watcher.Said;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Prep settle 1790: behaviour: sent away 10 s after a load, once back, then a ride longer than its time: no « has left »", () =>
        {
            var said = Trace(false);
            Check(said.Count == 0, "sent away after it came back from the load: the player's choice, nothing said: " + string.Join(", ", said));
            var before = Trace(true);
            Check(before is [{ Kind: PrepKind.Chocobo, Ended: true }], "what 0.1789 said: a false « has left » after the ride: " + string.Join(", ", before));
        }),
        ("Prep settle 1790: behaviour: the settling holds until the companion is back for 2 s after the load, and starts again at the next load", () =>
        {
            var settling = new PrepSettling();
            Check(!settling.Holds(Start), "no load seen: nothing held");
            settling.Loaded(Start);
            Check(settling.Holds(Start.AddSeconds(5)) && !settling.Holds(Start.AddSeconds(20)), "within 20 s of the load, not after");
            // A glimpse of half a second right after the load: still settling.
            settling.Seen(Start.AddSeconds(1), true);
            settling.Seen(Start.AddSeconds(1.5), true);
            settling.Seen(Start.AddSeconds(1.6), false);
            Check(settling.Holds(Start.AddSeconds(2)), "out for half a second: not back, the load still explains it");
            // Back for two seconds without a break: the load no longer explains it going.
            for (var at = 5.0; at <= 7.5; at += 0.1) settling.Seen(Start.AddSeconds(at), true);
            settling.Seen(Start.AddSeconds(8), false);
            Check(!settling.Holds(Start.AddSeconds(8)), "back for 2 s: sent away now is the player's choice");
            settling.Loaded(Start.AddSeconds(30));
            Check(settling.Holds(Start.AddSeconds(31)), "a new load: settling again");
            // Out during the load's frames: not back until out for 2 s after the load ends.
            for (var at = 30.0; at <= 33.0; at += 0.1) { settling.Loaded(Start.AddSeconds(at)); settling.Seen(Start.AddSeconds(at), true); }
            settling.Seen(Start.AddSeconds(34), true);
            Check(settling.Holds(Start.AddSeconds(34.5)), "seen during the load and 1 s after it: still settling");
            settling.Seen(Start.AddSeconds(35.5), true);
            Check(settling.Holds(Start.AddSeconds(35.6)), "out 1.5 s after the load ended: not yet");
            settling.Seen(Start.AddSeconds(36.1), true);
            Check(!settling.Holds(Start.AddSeconds(36.2)), "out 2 s without a break after the load ended: back");
        }),
        ("Prep settle 1790: behaviour: gone with the load and not back yet, then mounted: still held, its time ending during the ride told once after", () =>
        {
            var watcher = new Watcher(false);
            var now = Start;
            watcher.Frames(ref now, 10, true, 120);
            watcher.Frames(ref now, 3, false, 0, loading: true);
            watcher.Frames(ref now, 4, false, 0);
            watcher.Frames(ref now, 5 * 60, false, 0, mounted: true);
            Check(watcher.Said.Count == 0, "held by the load, then by the mount: nothing during the ride");
            watcher.Frames(ref now, 30, false, 0);
            Check(watcher.Said is [{ Kind: PrepKind.Chocobo, Ended: true }], "dismounted, not back: « has left », once: " + string.Join(", ", watcher.Said));
        }),
    ];
}
