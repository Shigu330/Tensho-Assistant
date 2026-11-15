// 0.1430, X-433 measure 6: the runtime's collections, read from its own events: each collection of generation 2 named with its kind
// (background or blocking), when it began and ended and how many other collections ended meanwhile, to compare with the readers' hitches
// about 20 s after a load (the game's thread waiting with no processor time). The tracker's notes and counts; a real listener in this process
// sees a forced collection; the plugin says the summary with the others.
using TenshoAssistant.Diagnostics;

internal static class Collections1430Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTime At = new(2026, 11, 15, 15, 18, 56, DateTimeKind.Utc);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Collections 1430: a background collection of generation 2 gives one note with its begin, end and the collections made meanwhile", () =>
        {
            var tracker = new CollectionTracker(2);
            Check(tracker.See("GCStart_V2", At, 40, 2, 1, 0) is null, "a beginning gives no note");
            Check(tracker.See("GCStart_V2", At.AddMilliseconds(50), 41, 0, 2, 0) is null && tracker.See("GCEnd_V1", At.AddMilliseconds(75), 41, 0) is null, "a foreground one of generation 0 meanwhile: no note");
            var note = tracker.See("GCEnd_V1", At.AddMilliseconds(411), 40, 2);
            var local = At.ToLocalTime();
            Check(note == $"Background collection of generation 2 (small allocations) from {local:HH:mm:ss.fff} to {local.AddMilliseconds(411):HH:mm:ss.fff}: 411 ms, 1 other collections ended meanwhile.", "the note: " + note);
            Check(tracker.Ended == 2 && tracker.Generation2 == 1 && tracker.Background == 1, "counted");
            Check(tracker.See("GCEnd_V1", At.AddSeconds(1), 99, 2) is null && tracker.Ended == 2, "an end without its beginning is not counted");
            tracker.See("GCStart_V2", At.AddSeconds(2), 42, 2, 0, 4);
            var blocking = tracker.See("GCEnd_V1", At.AddSeconds(2).AddMilliseconds(30), 42, 2);
            Check(blocking is not null && blocking.StartsWith("Blocking collection of generation 2 (large allocations) from ", StringComparison.Ordinal) && blocking.Contains(": 30 ms, 0 other", StringComparison.Ordinal), "a blocking one: " + blocking);
            tracker.See("GCStart_V2", At.AddSeconds(3), 43, 2, 1, 0);
            Check(tracker.See("GCEnd_V1", At.AddSeconds(4), 43, 2) is null && tracker.Generation2 == 3, "past the notes' cap: counted, not said");
            Check(tracker.Summary(TimeSpan.FromMinutes(2)) == "Collections in the 120 s after the load: 4, 3 of generation 2 (2 in the background).", tracker.Summary(TimeSpan.FromMinutes(2)));
            Check(tracker.See("GCEnd_V1", At, null, 2) is null, "no count: nothing");
        }),
        ("Collections 1430: the table of collections begun is bounded when their ends are missed", () =>
        {
            var tracker = new CollectionTracker();
            for (var count = 0; count < CollectionTracker.OpenCap * 3; count++) tracker.See("GCStart_V2", At.AddMilliseconds(count), count, 0, 0, 0);
            // The oldest were dropped: their ends count nothing; the latest still end.
            Check(tracker.See("GCEnd_V1", At.AddSeconds(5), 0, 0) is null && tracker.Ended == 0, "the oldest dropped");
            tracker.See("GCEnd_V1", At.AddSeconds(5), CollectionTracker.OpenCap * 3 - 1, 0);
            Check(tracker.Ended == 1, "the latest kept");
        }),
        ("Collections 1430: a listener in this process sees forced collections end, then lets go; the plugin says the summary", () =>
        {
            var listener = new RuntimeSuspensionListener(_ => { });
            try
            {
                var waited = System.Diagnostics.Stopwatch.StartNew();
                while (listener.Collections.Generation2 == 0 && waited.Elapsed < TimeSpan.FromSeconds(20))
                {
                    GC.Collect();
                    Thread.Sleep(100);
                }
                Check(listener.Collections.Generation2 > 0 && listener.Collections.Ended >= listener.Collections.Generation2, "a collection of generation 2 seen within 20 s: " + listener.Collections.Summary(TimeSpan.Zero));
            }
            finally { RuntimeSuspensionWatch.End(listener); }
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("log.Information(suspensions.Collections.Summary(Diagnostics.RuntimeSuspensionWatch.Length));", StringComparison.Ordinal), "said with the other summaries, in developer mode");
            Check(Read("src/TenshoAssistant/Diagnostics/RuntimeSuspensions.cs").Contains("if(written.EventName is \"GCStart_V2\" or \"GCEnd_V1\")", StringComparison.Ordinal), "the listener feeds it");
        }),
    ];
}
