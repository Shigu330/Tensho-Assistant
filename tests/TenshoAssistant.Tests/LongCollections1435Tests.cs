// 0.1435, X-433 measure 9: at the load of 17:39 a gap of 513 ms between two of the game's frames went with a 432 ms stop for a collection that was
// not of generation 2. The collections of generation 0 and 1 of 100 ms or more are now named with their generation, kind, reason, begin and end;
// shorter ones stay counted only. The frame-gap measure stops during a competitive cut (R03.2).
using TenshoAssistant.Diagnostics;

internal static class LongCollections1435Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTime At = new(2026, 11, 15, 16, 39, 30, DateTimeKind.Utc);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Long collections 1435: one of generation 1 of 432 ms gives a note with its generation; a short one gives none", () =>
        {
            var tracker = new CollectionTracker();
            tracker.See("GCStart_V2", At, 70, 1, 0, 0);
            var note = tracker.See("GCEnd_V1", At.AddMilliseconds(432), 70, 1);
            var local = At.ToLocalTime();
            Check(note == $"Blocking collection of generation 1 (small allocations) from {local:HH:mm:ss.fff} to {local.AddMilliseconds(432):HH:mm:ss.fff}: 432 ms.", "the note: " + note);
            tracker.See("GCStart_V2", At.AddSeconds(1), 71, 0, 0, 0);
            Check(tracker.See("GCEnd_V1", At.AddSeconds(1).AddMilliseconds(99), 71, 0) is null, "99 ms: no note");
            Check(tracker.Ended == 2 && tracker.Generation2 == 0, "counted, not of generation 2");
            Check(new CollectionTracker().ShortNoteMs == 100, "100 ms by default");
        }),
        ("Long collections 1435: the frame-gap measure stops during a competitive cut", () =>
        {
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("void Gap(){if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;if(gaps.Frame(", StringComparison.Ordinal), "the cut first");
        }),
    ];
}
