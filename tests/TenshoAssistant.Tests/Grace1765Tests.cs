using Newtonsoft.Json;
using TenshoAssistant.Gear;

// X-769 M1 (review of 0.1764, 22/11): one reading that did not see a manual's Grace wiped its count, and the status seen again was taken
// for a start seen: the whole cap shown, far from the 18 hours of a new manual (a wrong number instead of « unknown », R06). Now a reading
// that misses a status keeps its count; a status seen again goes on only when the jobs' experience did not move in between; a new count
// is from the manual's start only when its seconds left are within two minutes of the 18 hours written in each manual's description.
internal static class Grace1765Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const string Jobs = "8:50:1000,9:42:0", JobsLater = "8:50:3500,9:42:0";

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static Dictionary<uint, (ushort Param, float Remaining)> Held(float remaining, ushort param = 70, uint status = ManualGrace.CrafterGrace) => new() { [status] = (param, remaining) };

    private static readonly Dictionary<uint, (ushort Param, float Remaining)> None = [];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Grace 1765: behaviour: the review's case: a reading that misses the status keeps its count; seen again, the count goes on (not the whole cap)", () =>
        {
            var seen = new HashSet<uint>();
            var kept = ManualGrace.Read([], Held(64_799f), _ => Jobs, false, seen, out var current, out var changed);
            Check(changed && current.Count == 1 && current[0].StartSeen && ManualGrace.Left(current[0]) == 1_750_000 && seen.SetEquals([ManualGrace.CrafterGrace]), "a manual used while Tensho watches: counted from its start");
            Check(ManualGrace.Gain(current[0], 2_500, 150, 50) && ManualGrace.Left(current[0]) == 1_748_500, "one line counted");
            var count = current[0];
            kept = ManualGrace.Read(kept, None, _ => Jobs, false, seen, out current, out changed);
            Check(!changed && current.Count == 0 && kept.Count == 1 && ReferenceEquals(kept[0], count) && seen.Count == 0, "one reading without the status: its count kept as it is, nothing to save, nothing shown");
            kept = ManualGrace.Read(kept, Held(60_000f), _ => Jobs, false, seen, out current, out changed);
            Check(current.Count == 1 && ReferenceEquals(current[0], count) && ManualGrace.Left(current[0]) == 1_748_500, "seen again with the same experience: the count goes on: " + ManualGrace.Left(current[0]));
            kept = ManualGrace.Read(kept, None, _ => Jobs, false, seen, out _, out _);
            kept = ManualGrace.Read(kept, Held(59_000f), _ => JobsLater, false, seen, out current, out changed);
            Check(changed && current.Count == 1 && !current[0].StartSeen && ManualGrace.Left(current[0]) is null, "seen again after experience gained while it was not seen (its lines not counted): unknown, not the whole cap");
            kept = ManualGrace.Read(kept, Held(58_999f), _ => "8:50:9000,9:42:0", false, seen, out current, out _);
            Check(!current[0].StartSeen && ManualGrace.Left(current[0]) is null, "unknown while the same manual lasts");
        }),
        ("Grace 1765: behaviour: with no count, a status first seen far from its 18 hours is unknown; within two minutes of them, from its start", () =>
        {
            var old = ManualGrace.Observe(null, ManualGrace.CrafterGrace, 70, 30_000f, Jobs, false, true, out var changed);
            Check(changed && !old.StartSeen && ManualGrace.Left(old) is null, "30,000 s left while Tensho watches (count lost): unknown, never the whole cap");
            Check(ManualGrace.FullSeconds == 64_800 && ManualGrace.StartSeconds == 120, "18 hours, two minutes");
            Check(ManualGrace.Observe(null, ManualGrace.CrafterGrace, 70, 64_680f, Jobs, false, true, out _).StartSeen, "two minutes after the use (a loading screen): from its start");
            Check(!ManualGrace.Observe(null, ManualGrace.CrafterGrace, 70, 64_679f, Jobs, false, true, out _).StartSeen, "later: unknown");
            Check(!ManualGrace.Observe(null, ManualGrace.CrafterGrace, 70, 64_800f, Jobs, true, true, out _).StartSeen, "at the first reading after a load: unknown, however new");
            var running = ManualGrace.Observe(null, ManualGrace.CrafterGrace, 70, 64_800f, Jobs, false, false, out _);
            var renewed = ManualGrace.Observe(running, ManualGrace.CrafterGrace, 70, 64_790f, Jobs, false, false, out _);
            Check(ReferenceEquals(running, renewed), "the same manual a few seconds later: the same count");
            running.Remaining = 20_000f;
            var other = ManualGrace.Observe(running, ManualGrace.CrafterGrace, 80, 40_000f, Jobs, false, false, out _);
            Check(!other.StartSeen && other.Param == 80, "another manual seen with 40,000 s left: unknown");
        }),
        ("Grace 1765: behaviour: through the settings' text (a reload), a status not seen at the first reading is checked when it comes back", () =>
        {
            var seen = new HashSet<uint>();
            var kept = ManualGrace.Read([], Held(64_800f), _ => Jobs, false, seen, out var current, out _);
            ManualGrace.Gain(current[0], 2_500, 150, 50);
            kept = ManualGrace.Read(kept, Held(64_000f), _ => JobsLater, false, seen, out _, out _);
            List<GraceCount> Reload() => JsonConvert.DeserializeObject<Dictionary<ulong, List<GraceCount>>>(JsonConvert.SerializeObject(new Dictionary<ulong, List<GraceCount>> { [42] = kept }))![42];
            // The first reading after the load sees no status yet; the second sees it.
            var fresh = new HashSet<uint>();
            var reloaded = ManualGrace.Read(Reload(), None, _ => JobsLater, true, fresh, out current, out var changed);
            Check(!changed && current.Count == 0 && reloaded.Count == 1 && reloaded[0].StartSeen, "not seen at the first reading: the saved count is kept");
            ManualGrace.Read(reloaded, Held(63_000f), _ => JobsLater, false, fresh, out current, out _);
            Check(current[0].StartSeen && ManualGrace.Left(current[0]) == 1_748_500, "seen at the second, nothing gained in between: the count goes on: " + ManualGrace.Left(current[0]));
            fresh.Clear();
            reloaded = ManualGrace.Read(Reload(), None, _ => JobsLater, true, fresh, out _, out _);
            ManualGrace.Read(reloaded, Held(63_000f), _ => "8:51:100,9:42:0", false, fresh, out current, out _);
            Check(!current[0].StartSeen && ManualGrace.Left(current[0]) is null, "experience gained while Tensho was not running: unknown");
            var both = ManualGrace.Read(Reload(), Held(40_000f, 70, ManualGrace.GathererGrace), _ => Jobs, false, [], out current, out _);
            Check(both.Count == 2 && both[0].Status == ManualGrace.GathererGrace && both[1].Status == ManualGrace.CrafterGrace && current.Count == 1 && !current[0].StartSeen,
                "a Grace held and one kept: both saved, the held one first; the gatherer's first seen at 40,000 s: unknown");
        }),
        ("Grace 1765: wiring: the watcher reads through ManualGrace.Read, keeps what it returns, shows only the Graces held, and checks again after a loading screen", () =>
        {
            var watcher = Read("src/TenshoAssistant/Gear/GraceWatcher.Dalamud.cs");
            Check(watcher.Contains("var next=ManualGrace.Read(counts,held,grace=>Snapshot(PlayerState.Instance(),grace),firstLook,seenBefore,out var current,out var changed);", StringComparison.Ordinal), "one reading");
            Check(watcher.Contains("if(next.Count>0)all[character]=next;", StringComparison.Ordinal) && watcher.Contains("Volatile.Write(ref shown,current.Select(", StringComparison.Ordinal), "kept: all; shown: those held");
            Check(watcher.Contains("if(current.FirstOrDefault(count=>count.Status==ManualGrace.StatusFor(job)) is not { } count)continue;", StringComparison.Ordinal), "lines counted on the Graces held only");
            Check(watcher.Contains("lock(gate)gains.Clear();\n                seenBefore.Clear();", StringComparison.Ordinal) && watcher.Contains("levels.Clear();seenBefore.Clear();}", StringComparison.Ordinal), "a loading screen or another character: checked again");
            Check(!watcher.Contains("if(saved is not null)changed=true;continue;", StringComparison.Ordinal), "a status not seen no longer wipes its count");
            var evidence = Read("docs/evidence/0.1231-grace-cap.md");
            Check(evidence.Contains("Duration: 18 hours", StringComparison.Ordinal) && evidence.Contains("Durée maximum d'effet : 18 heures", StringComparison.Ordinal), "the 18 hours cited from the game");
        }),
    ];
}
