using Newtonsoft.Json;
using TenshoAssistant.Gear;

// X-221 (the user's request of 12/11/2026, K-37): the bonus points left of a manual's Grace. The cap counts the bonus points (the game's help:
// « Bonus maximum : 1 750 000 de points d'expérience »); the status's Param is the level from which the bonus is halved (70 read in game);
// each experience line (LogMessage 589: job, points, bonus percent) adds the manual's part. Kept per character through a reload; unknown when
// Tensho did not see the start or when experience was gained without it; a new manual starts a new count.
internal static class Grace1760Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const string Jobs = "8:50:1000,9:42:0", JobsLater = "8:50:3500,9:42:0";

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static GraceCount Watching(ushort param = 70) => ManualGrace.Observe(null, ManualGrace.CrafterGrace, param, 64_800f, Jobs, false, out _);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Grace 1760: behaviour: the count from the manual's start, the manual's part of each line, halved from the Param's level, never below 0", () =>
        {
            var count = Watching();
            Check(count.StartSeen && count.Used == 0 && ManualGrace.Left(count) == 1_750_000, "the start seen: the whole cap left: " + ManualGrace.Left(count));
            Check(ManualGrace.Gain(count, 2_500, 150, 50) && ManualGrace.Left(count) == 1_748_500, "2,500 points at +150% below level 70: 1,500 of them the manual's: " + ManualGrace.Left(count));
            // With a meal's 3% beside the manual: 2,530 × 150 ÷ 253 = 1,500.
            Check(ManualGrace.Gain(count, 2_530, 153, 50) && ManualGrace.Left(count) == 1_747_000, "with another bonus, the manual's part only: " + ManualGrace.Left(count));
            Check(!ManualGrace.Gain(count, 2_000, 0, 50) && ManualGrace.Left(count) == 1_747_000, "a line without bonus: nothing");
            Check(ManualGrace.Gain(count, 1_750, 75, 70) && ManualGrace.Left(count) == 1_746_250, "from level 70, +75%: 750 of 1,750");
            Check(ManualGrace.Gain(count, 2_000, 100, 69) && ManualGrace.Left(count) == 1_745_250, "a percent below the manual's rate (its cap reached in this line): never more than the percent shown");
            count.Used = 1_749_999.5;
            Check(ManualGrace.Gain(count, 100_000, 150, 50) && ManualGrace.Left(count) == 0, "never below 0");
            Check(ManualGrace.Rate(70, 69) == 150 && ManualGrace.Rate(70, 70) == 75 && ManualGrace.Rate(40, 39) == 150 && ManualGrace.Rate(80, 80) == 75 && ManualGrace.Rate(70, 0) is null, "the rates");
            Check(ManualGrace.Cap(40) == 250_000 && ManualGrace.Cap(50) == 500_000 && ManualGrace.Cap(70) == 1_750_000 && ManualGrace.Cap(80) == 2_000_000 && ManualGrace.Cap(60) is null && ManualGrace.Cap(0) is null, "the four manuals' caps, by the level they halve from");
        }),
        ("Grace 1760: behaviour: the start not seen (held at the first reading, no count saved): unknown, its lines not counted; a manual not known: unknown", () =>
        {
            var held = ManualGrace.Observe(null, ManualGrace.CrafterGrace, 70, 30_000f, Jobs, true, out var changed);
            Check(changed && !held.StartSeen && ManualGrace.Left(held) is null, "held at the first look: unknown");
            Check(!ManualGrace.Gain(held, 2_500, 150, 50) && ManualGrace.Left(held) is null, "its lines are not counted");
            var later = ManualGrace.Observe(held, ManualGrace.CrafterGrace, 70, 29_999f, JobsLater, false, out _);
            Check(ReferenceEquals(later, held) && !later.StartSeen, "still unknown while the same manual lasts");
            var odd = Watching(param: 60);
            Check(odd.StartSeen && ManualGrace.Left(odd) is null && !ManualGrace.Gain(odd, 2_500, 150, 50), "a Param that names no known manual: no number");
        }),
        ("Grace 1760: behaviour: a reload goes on with the saved count (through the settings' text), unless experience was gained in between or a new manual was used", () =>
        {
            var count = Watching();
            ManualGrace.Gain(count, 2_500, 150, 50);
            count = ManualGrace.Observe(count, ManualGrace.CrafterGrace, 70, 60_000f, JobsLater, false, out var changed);
            Check(changed && count.Jobs == JobsLater, "the jobs' experience after the line: saved");
            // As Dalamud saves the settings (Newtonsoft), then a reload of the plugin.
            var saved = JsonConvert.DeserializeObject<Dictionary<ulong, List<GraceCount>>>(JsonConvert.SerializeObject(new Dictionary<ulong, List<GraceCount>> { [42] = [count] }))![42].Single();
            var again = ManualGrace.Observe(saved, ManualGrace.CrafterGrace, 70, 59_000f, JobsLater, true, out changed);
            Check(ReferenceEquals(again, saved) && again.StartSeen && ManualGrace.Left(again) == 1_748_500 && !changed, "the same manual, nothing gained in between: the count goes on: " + ManualGrace.Left(again));
            var gained = ManualGrace.Observe(JsonConvert.DeserializeObject<GraceCount>(JsonConvert.SerializeObject(count)), ManualGrace.CrafterGrace, 70, 59_000f, "8:51:200,9:42:0", true, out changed);
            Check(changed && !gained.StartSeen && ManualGrace.Left(gained) is null, "experience gained while Tensho was not running: unknown");
            var renewed = ManualGrace.Observe(JsonConvert.DeserializeObject<GraceCount>(JsonConvert.SerializeObject(count)), ManualGrace.CrafterGrace, 70, 64_800f, JobsLater, true, out changed);
            Check(changed && !renewed.StartSeen, "a new manual used while Tensho was not running (its time started again): unknown");
            var other = ManualGrace.Observe(JsonConvert.DeserializeObject<GraceCount>(JsonConvert.SerializeObject(count)), ManualGrace.CrafterGrace, 80, 50_000f, JobsLater, true, out changed);
            Check(changed && !other.StartSeen && other.Param == 80, "another manual: unknown");
        }),
        ("Grace 1760: behaviour: a new manual used while Tensho watches starts a new count; the seconds left alone are not a change to save", () =>
        {
            var count = Watching();
            ManualGrace.Gain(count, 2_500, 150, 50);
            var same = ManualGrace.Observe(count, ManualGrace.CrafterGrace, 70, 64_000f, Jobs, false, out var changed);
            Check(ReferenceEquals(same, count) && !changed && same.Remaining == 64_000f, "the time going down: nothing to save");
            var renewed = ManualGrace.Observe(count, ManualGrace.CrafterGrace, 70, 64_800f, Jobs, false, out changed);
            Check(changed && !ReferenceEquals(renewed, count) && renewed.StartSeen && ManualGrace.Left(renewed) == 1_750_000, "its time started again: a new count from the whole cap");
            var revised = ManualGrace.Observe(renewed, ManualGrace.CrafterGrace, 80, 64_700f, Jobs, false, out changed);
            Check(changed && revised.StartSeen && ManualGrace.Left(revised) == 2_000_000, "another manual: its own cap");
        }),
        ("Grace 1760: behaviour: which Grace boosts which jobs; the jobs' snapshot is in the jobs' order", () =>
        {
            Check(ManualGrace.StatusFor(8) == 45 && ManualGrace.StatusFor(15) == 45 && ManualGrace.StatusFor(16) == 46 && ManualGrace.StatusFor(18) == 46
                && ManualGrace.StatusFor(7) == 0 && ManualGrace.StatusFor(19) == 0 && ManualGrace.StatusFor(42) == 0, "crafters 45, gatherers 46, no other job");
            Check(ManualGrace.JobsOf(45).SequenceEqual(new uint[] { 8, 9, 10, 11, 12, 13, 14, 15 }) && ManualGrace.JobsOf(46).SequenceEqual(new uint[] { 16, 17, 18 }) && !ManualGrace.JobsOf(48).Any(), "their jobs");
            Check(ManualGrace.Snapshot([(9, 42, 0), (8, 50, 1000)]) == Jobs, "by job: " + ManualGrace.Snapshot([(9, 42, 0), (8, 50, 1000)]));
            Check(ManualGrace.Share(2_500, 150, 150) == 1_500 && ManualGrace.Share(2_500, 0, 150) == 0 && ManualGrace.Share(-5, 150, 150) == 0, "the share");
        }),
        ("Grace 1760: wiring: the watcher reads LogMessage 589's parameters and your statuses, counts the job worn only, saves per character; the Gear page shows it under the last job; both languages; the evidence", () =>
        {
            var watcher = Read("src/TenshoAssistant/Gear/GraceWatcher.Dalamud.cs");
            Check(watcher.Contains("message.LogMessageId!=ManualGrace.ExperienceMessage", StringComparison.Ordinal) && ManualGrace.ExperienceMessage == 589
                && watcher.Contains("!message.TryGetIntParameter(0,out var job) || !message.TryGetIntParameter(1,out var gained) || !message.TryGetIntParameter(2,out var percent)", StringComparison.Ordinal), "LogMessage 589, its three parameters");
            Check(watcher.Contains("if(job!=worn || gained<0 || percent is <0 or >10_000)", StringComparison.Ordinal) && watcher.Contains("objects.LocalPlayer is not { } me", StringComparison.Ordinal), "your job worn only, your own statuses");
            Check(watcher.Contains("TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);", StringComparison.Ordinal) && watcher.Contains("chat.LogMessage-=OnLogMessage;", StringComparison.Ordinal), "cut in competitive content, unsubscribed");
            Check(watcher.Contains("if(changed)save();", StringComparison.Ordinal) && watcher.Contains("all[character]=next", StringComparison.Ordinal), "saved per character when it changes");
            var config = Read("src/TenshoAssistant/Configuration.cs");
            Check(config.Contains("public Dictionary<ulong,List<TenshoAssistant.Gear.GraceCount>>? GraceCounts { get; set; }", StringComparison.Ordinal) && TenshoAssistant.ConfigurationReset.Kept.Contains("GraceCounts"), "the player's data, kept by « reset all »");
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("graceWatcher=new Gear.GraceWatcher(", StringComparison.Ordinal) && plugin.Contains("()=>graceWatcher?.Dispose()", StringComparison.Ordinal) && plugin.Contains("mainWindow.GraceSource=()=>graceWatcher?.Shown;", StringComparison.Ordinal), "registered");
            var page = Read("src/TenshoAssistant/MainWindow.JobProgress.cs");
            Check(page.Contains("if(boosted==0 || index+1<rows.Count && ManualGrace.StatusFor(rows[index+1].Job)==boosted)continue;", StringComparison.Ordinal) && page.Contains("if(grace.Status==boosted)DrawGrace(grace);", StringComparison.Ordinal), "under the last job it boosts");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(new[] { "GraceLeft", "GraceUnknown", "GraceTip", "GraceUnknownTip", "GraceManualTip" }.All(key => text.Contains($"\"{key}\": ", StringComparison.Ordinal)), "the words, " + language);
            }
            var evidence = Read("docs/evidence/0.1231-grace-cap.md");
            Check(evidence.Contains("Bonus maximum : 1 750 000 de\n  points d'expérience", StringComparison.Ordinal) && evidence.Contains("LogMessage 589", StringComparison.Ordinal), "the game's help cited, the line read");
        }),
    ];
}
