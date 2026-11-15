// 0.1431, X-433 measure 7: the runtime's allocation ticks read in developer mode: the large and pinned ones counted by thread (the game's,
// the JIT warm-up's, others), and one made while a background collection runs named with its thread, type, size and time, to see which
// thread allocates a large object during the background collections that end with the readers' hitches about 20 s after a load.
using TenshoAssistant.Diagnostics;

internal static class LargeAllocations1431Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTime At = new(2026, 11, 15, 15, 34, 17, DateTimeKind.Utc);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Large allocations 1431: counted by thread; one during a background collection gives a note with its thread, type, size and time", () =>
        {
            var tracker = new LargeAllocationTracker(2);
            Check(tracker.See(At, 0, 102_400, "System.String", 7, 7, 9, At) is null && tracker.Large == 0, "small objects: nothing");
            Check(tracker.See(At, 1, 200_000, "System.Byte[]", 7, 7, 9, null) is null && tracker.Large == 1 && tracker.OnGameThread == 1, "a large one outside a background collection: counted only");
            var note = tracker.See(At.AddMilliseconds(300), 1, 262_144, "System.Byte[]", 7, 7, 9, At);
            var local = At.ToLocalTime();
            Check(note == $"Large object allocated on the game's thread at {local.AddMilliseconds(300):HH:mm:ss.fff} during the background collection begun {local:HH:mm:ss.fff}: System.Byte[], 256 KB.", "the note: " + note);
            var warmup = tracker.See(At.AddMilliseconds(310), 2, 1024, null, 9, 7, 9, At);
            Check(warmup is not null && warmup.StartsWith("Pinned object allocated on the JIT warm-up's thread at ", StringComparison.Ordinal) && warmup.EndsWith(": ?, 1 KB.", StringComparison.Ordinal), "the warm-up's, pinned: " + warmup);
            Check(tracker.See(At.AddMilliseconds(320), 1, 90_000, "X", 3, 7, 9, At) is null && tracker.DuringBackground == 3, "past the cap: counted, not said");
            Check(tracker.Summary(TimeSpan.FromMinutes(2)) == "Large or pinned allocation ticks in the 120 s after the load: 4 (2 on the game's thread, 1 on the JIT warm-up's), 3 during a background collection.", tracker.Summary(TimeSpan.FromMinutes(2)));
        }),
        ("Large allocations 1431: the collections tell whether a background one runs, from its beginning to its end", () =>
        {
            var collections = new CollectionTracker();
            Check(collections.BackgroundSince is null, "none at first");
            collections.See("GCStart_V2", At, 50, 2, 1, 0);
            collections.See("GCStart_V2", At.AddMilliseconds(40), 51, 0, 2, 0);
            collections.See("GCEnd_V1", At.AddMilliseconds(60), 51, 0);
            Check(collections.BackgroundSince == At, "running through the foreground one");
            collections.See("GCEnd_V1", At.AddMilliseconds(552), 50, 2);
            Check(collections.BackgroundSince is null, "over at its end");
            collections.See("GCStart_V2", At.AddSeconds(1), 52, 2, 0, 0);
            Check(collections.BackgroundSince is null, "a blocking one is not a background one");
        }),
        ("Large allocations 1431: a listener in this process sees a large allocation on this thread; the warm-up names its thread; the plugin says the summary", () =>
        {
            var listener = new RuntimeSuspensionListener(_ => { });
            var kept = new List<byte[]>();
            try
            {
                var waited = System.Diagnostics.Stopwatch.StartNew();
                while (listener.Large.Large == 0 && waited.Elapsed < TimeSpan.FromSeconds(20))
                {
                    for (var i = 0; i < 8; i++) kept.Add(new byte[300_000]);
                    Thread.Sleep(50);
                    if (kept.Count > 40) kept.Clear();
                }
                Check(listener.Large.Large > 0, "a large allocation tick seen within 20 s: " + listener.Large.Summary(TimeSpan.Zero));
            }
            finally { RuntimeSuspensionWatch.End(listener); }
            using (var warmup = JitWarmup.Start(() => [typeof(SlowCompilations)], TimeSpan.Zero))
                Check(warmup.Wait(TimeSpan.FromSeconds(30)) && Interlocked.Read(ref ThreadClock.WarmupThread) != 0 || !OperatingSystem.IsWindows(), "the warm-up's thread named");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("log.Information(suspensions.Large.Summary(Diagnostics.RuntimeSuspensionWatch.Length));", StringComparison.Ordinal), "said with the other summaries");
        }),
    ];
}
