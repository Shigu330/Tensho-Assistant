// 0.1422, X-433 step 1 (measure): the runtime's suspensions of every thread, read from its own events, with their reason, so the readers'
// slow passes about 25 s after a load are named (collections, or something else: tiered compilation, code patching). The tracker's sums and
// notes; a real listener in this process sees a collection's suspension; the plugin listens in developer mode only, two minutes, and lets go.
using TenshoAssistant.Diagnostics;

internal static class RuntimeSuspensions1422Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTime At = new(2026, 11, 15, 14, 12, 10, DateTimeKind.Utc);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Runtime suspensions 1422: begin and restart make one suspension; its reason says whether it was a collection; long ones give a note", () =>
        {
            var tracker = new SuspensionTracker(20, 2);
            Check(tracker.See("GCSuspendEEBegin_V1", At, 0) is null, "a beginning gives no note");
            var note = tracker.See("GCRestartEEEnd_V1", At.AddMilliseconds(320), null);
            Check(note is not null && note.Contains("320 ms", StringComparison.Ordinal) && note.Contains("not for a collection", StringComparison.Ordinal), "a long suspension not for a collection: " + note);
            Check(tracker.See("GCSuspendEEBegin_V1", At.AddSeconds(1), 1) is null && tracker.See("GCRestartEEEnd_V1", At.AddSeconds(1).AddMilliseconds(5), null) is null, "a short one: no note");
            Check(tracker.Count == 2 && Math.Abs(tracker.TotalMs - 325) < 0.01 && Math.Abs(tracker.OtherMs - 320) < 0.01 && Math.Abs(tracker.LongestMs - 320) < 0.01, "sums: " + tracker.Summary(TimeSpan.FromMinutes(2)));
            Check(tracker.See("GCRestartEEEnd_V1", At.AddSeconds(2), null) is null && tracker.Count == 2, "a restart without its beginning is not counted");
            Check(SuspensionTracker.ForCollection(1) && SuspensionTracker.ForCollection(6) && !SuspensionTracker.ForCollection(0), "1 and 6 are collections");
            tracker.See("TieredCompilationBackgroundJitStop", At, null, 41);
            tracker.See("TieredCompilationBackgroundJitStop", At, null, 9);
            Check(tracker.TieredMethods == 50, "tiered methods summed");
            // At most MaximumNotes notes.
            for (var i = 0; i < 3; i++) { tracker.See("GCSuspendEEBegin_V1", At.AddSeconds(10 + i), 1); tracker.See("GCRestartEEEnd_V1", At.AddSeconds(10 + i).AddMilliseconds(30), null); }
            Check(tracker.Count == 5, "all counted");
            var summary = tracker.Summary(TimeSpan.FromMinutes(2));
            Check(summary.StartsWith("Runtime suspensions in the 120 s after the load: 5, 415 ms in all, 320 ms not for a collection, the longest 320 ms; tiered compilation recompiled 50 methods", StringComparison.Ordinal), summary);
        }),
        ("Runtime suspensions 1422: a listener in this process sees the suspension of a forced collection, then lets go", () =>
        {
            var notes = new List<string>();
            var listener = new RuntimeSuspensionListener(note => { lock (notes) notes.Add(note); });
            try
            {
                var waited = System.Diagnostics.Stopwatch.StartNew();
                while (listener.Tracker.Count == 0 && waited.Elapsed < TimeSpan.FromSeconds(20))
                {
                    GC.Collect();
                    Thread.Sleep(100);
                }
                Check(listener.Tracker.Count > 0, "a suspension seen within 20 s");
            }
            finally { Check(RuntimeSuspensionWatch.End(listener) && listener.IsDisposed && !RuntimeSuspensionWatch.End(listener), "ended once"); }
            var count = listener.Tracker.Count;
            GC.Collect();
            Thread.Sleep(200);
            Check(listener.Tracker.Count <= count + 1, "nothing more after it ended (one in flight at most)");
        }),
        ("Runtime suspensions 1422: the plugin listens in developer mode only, two minutes, released at the unload; the right source and keywords", () =>
        {
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            var start = plugin.IndexOf("if(configuration.DeveloperMode)\n            {\n                var suspensions=new Diagnostics.RuntimeSuspensionListener(note=>log.Information(note));", StringComparison.Ordinal);
            Check(start > 0 && start > plugin.IndexOf("mainWindow.LoadMilliseconds=", StringComparison.Ordinal), "developer mode, after the load line");
            Check(plugin.Contains("registrations.Register(()=>{},()=>suspensions.Dispose());", StringComparison.Ordinal), "released with the plugin's registrations");
            Check(plugin.Contains("Task.Delay(Diagnostics.RuntimeSuspensionWatch.Length)", StringComparison.Ordinal) && RuntimeSuspensionWatch.Length == TimeSpan.FromMinutes(2), "two minutes");
            Check(RuntimeSuspensionListener.RuntimeSource == "Microsoft-Windows-DotNETRuntime" && RuntimeSuspensionListener.GcKeyword == 1 && RuntimeSuspensionListener.TieredCompilationKeyword == 0x400000000, "source and keywords");
            Check(Read("src/TenshoAssistant/Diagnostics/RuntimeSuspensions.cs").Contains("EnableEvents(source,EventLevel.Informational,(EventKeywords)(GcKeyword|TieredCompilationKeyword|", StringComparison.Ordinal), "enabled at the informational level");
        }),
    ];
}
