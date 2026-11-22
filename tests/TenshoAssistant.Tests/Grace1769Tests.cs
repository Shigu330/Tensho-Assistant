using TenshoAssistant;
using TenshoAssistant.Gear;

// X-770 M1 (review of 0.1768, 22/11): the count of a Grace the game took away (cap reached, removed by hand) was kept for ever; a manual of
// the same Param used later, read with no more time left than the old one and no experience gained in between, went on with the old count
// (close to 0) instead of « unknown » (R06). Now a count whose status is not seen at 30 readings in a row is dropped; a status missed for a
// few readings (after a load or a loading screen) still goes on with its count.
internal static class Grace1769Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const string Jobs = "8:50:1000,9:42:0";

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static Dictionary<uint, (ushort Param, float Remaining)> Held(float remaining, ushort param = 70, uint status = ManualGrace.CrafterGrace) => new() { [status] = (param, remaining) };

    private static readonly Dictionary<uint, (ushort Param, float Remaining)> None = [];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Grace 1769: behaviour: the review's case: a Grace taken away is dropped after 30 readings; a manual used later is unknown, not the old count", () =>
        {
            var seen = new HashSet<uint>();
            var kept = ManualGrace.Read([], Held(64_800f), _ => Jobs, false, seen, out var current, out _);
            ManualGrace.Gain(current[0], 1_165_000, 150, 50);
            Check(ManualGrace.Left(current[0]) == 1_051_000, "the manual counted: " + ManualGrace.Left(current[0]));
            kept = ManualGrace.Read(kept, Held(30_000f), _ => Jobs, false, seen, out current, out _);
            var old = current[0];
            // The game takes the status away (cap reached, or removed by hand): 29 readings keep the count, the 30th drops it.
            var changed = false;
            for (var reading = 1; reading < ManualGrace.GoneReadings; reading++)
            {
                kept = ManualGrace.Read(kept, None, _ => Jobs, false, seen, out current, out changed);
                Check(!changed && kept.Count == 1 && ReferenceEquals(kept[0], old) && current.Count == 0, "reading " + reading + ": kept, nothing to save");
            }
            kept = ManualGrace.Read(kept, None, _ => Jobs, false, seen, out current, out changed);
            Check(ManualGrace.GoneReadings == 30 && changed && kept.Count == 0 && current.Count == 0, "the 30th reading without the status: the count dropped, to save");
            // A manual used later, read with less time left than the old one and no experience gained: unknown.
            kept = ManualGrace.Read(kept, Held(28_800f), _ => Jobs, true, [], out current, out _);
            Check(current.Count == 1 && !ReferenceEquals(current[0], old) && !current[0].StartSeen && ManualGrace.Left(current[0]) is null, "the new manual: unknown, not the old count");
        }),
        ("Grace 1769: behaviour: a status missed for a few readings (a load, a loading screen) goes on with its count and starts the readings again", () =>
        {
            var seen = new HashSet<uint>();
            var kept = ManualGrace.Read([], Held(64_800f), _ => Jobs, false, seen, out var current, out _);
            ManualGrace.Gain(current[0], 2_500, 150, 50);
            var count = current[0];
            for (var reading = 0; reading < ManualGrace.GoneReadings - 1; reading++) kept = ManualGrace.Read(kept, None, _ => Jobs, false, seen, out _, out _);
            Check(count.Missed == ManualGrace.GoneReadings - 1, "29 readings missed: " + count.Missed);
            kept = ManualGrace.Read(kept, Held(60_000f), _ => Jobs, false, seen, out current, out _);
            Check(ReferenceEquals(current[0], count) && count.Missed == 0 && ManualGrace.Left(count) == 1_748_500, "seen again: the same count, its missed readings started again");
            for (var reading = 0; reading < ManualGrace.GoneReadings - 1; reading++) kept = ManualGrace.Read(kept, None, _ => Jobs, false, seen, out _, out _);
            Check(kept.Count == 1 && ReferenceEquals(kept[0], count), "29 more readings missed after it came back: still kept");
            kept = ManualGrace.Read(kept, None, _ => Jobs, false, seen, out _, out var changed);
            Check(changed && kept.Count == 0, "the 30th in a row: dropped");
            var crafter = new GraceCount { Status = ManualGrace.CrafterGrace, Param = 70, Remaining = 1_000f, Jobs = Jobs };
            var gatherer = new GraceCount { Status = ManualGrace.GathererGrace, Param = 50, Remaining = 1_000f, Jobs = Jobs, Missed = 5 };
            var both = ManualGrace.Read([crafter, gatherer], Held(500f, 50, ManualGrace.GathererGrace), _ => Jobs, false, seen, out current, out _);
            Check(both.Count == 2 && current.Count == 1 && ReferenceEquals(current[0], gatherer) && gatherer.Missed == 0 && crafter.Missed == 1,
                "each Grace its own readings: the gatherer's held starts again, the crafter's not held counts one: " + crafter.Missed + ", " + gatherer.Missed);
        }),
        ("Grace 1769: behaviour: the readings missed are not saved (a reload starts them again) and the saved settings keep their format", () =>
        {
            var count = new GraceCount { Status = ManualGrace.CrafterGrace, Param = 70, Used = 10, StartSeen = true, Remaining = 1_000f, Jobs = Jobs, Missed = 12 };
            // The settings' own way (SettingsJson, System.Text.Json since 0.1745).
            var text = System.Text.Json.JsonSerializer.Serialize(count, SettingsJson.Options);
            Check(!text.Contains("Missed", StringComparison.Ordinal) && text.Contains("\"Remaining\": 1000", StringComparison.Ordinal), "not in the settings' text: " + text);
            var back = System.Text.Json.JsonSerializer.Deserialize<GraceCount>(text, SettingsJson.Options)!;
            Check(back.Missed == 0 && back.StartSeen && back.Used == 10, "read back: 0 readings missed, the rest kept");
        }),
        ("Grace 1769: wiring: nothing read in a competitive content; a dropped count said in the log; the evidence", () =>
        {
            var watcher = Read("src/TenshoAssistant/Gear/GraceWatcher.Dalamud.cs");
            Check(watcher.Contains("player.ContentId==0 || TenshoAssistant.Competitive.CompetitiveGate.Cut)\n            {\n                // X-769", StringComparison.Ordinal), "the competitive gate with the loading screen: nothing read, checked again after");
            Check(watcher.Contains("if(!held.ContainsKey(count.Status) && !next.Any(kept=>ReferenceEquals(kept,count)))", StringComparison.Ordinal) && watcher.Contains("its count is dropped.", StringComparison.Ordinal), "the log line of a dropped count");
            var grace = Read("src/TenshoAssistant/Gear/ManualGrace.cs");
            Check(grace.Contains("its count is dropped.</summary>\n    internal int Missed { get; set; }", StringComparison.Ordinal), "the readings missed not saved");
            var evidence = Read("docs/evidence/0.1231-grace-cap.md");
            Check(evidence.Contains("Since 0.1769 (X-770, review of 0.1768)", StringComparison.Ordinal) && evidence.Contains("the game gives nothing to tell them apart", StringComparison.Ordinal), "the evidence: the change and the case left");
        }),
    ];
}
