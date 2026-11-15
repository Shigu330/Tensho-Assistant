// 0.1437 (X-440): the minor findings of the review of 0.1428 to 0.1435. M1: the header of CacheStream1432Tests no longer calls the burst
// the cause. M2: a period with the plugin interface hidden (no Draw) is not read as a frame gap. M3: the French channel names are proved
// by the French rows of the LogFilter sheet in the 0.1434 evidence.
using System.Text.Json;
using TenshoAssistant.Diagnostics;

internal static class Review1437Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset At = new(2026,11,15,18,30,0,TimeSpan.FromHours(1));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Review 1437 M1: the 0.1432 test header says the burst was reduced, not that it was the cause",() =>
        {
            var header = string.Join("\n",Read("tests/TenshoAssistant.Tests/CacheStream1432Tests.cs").Split('\n').TakeWhile(line => line.StartsWith("//",StringComparison.Ordinal)));
            Check(!header.Contains("(fix",StringComparison.Ordinal),"no « fix » in the header");
            Check(header.Contains("burst reduced",StringComparison.Ordinal) && header.Contains("not the only cause",StringComparison.Ordinal) && header.Contains("X-433 stays open",StringComparison.Ordinal),header);
        }),
        ("Review 1437 M2: the interface hidden ends the run of frames; the time it was hidden is no gap; the summary counts the periods",() =>
        {
            var gaps = new FrameGaps(150,3,5);
            Check(gaps.Frame(0,At) is null && gaps.Frame(16,At.AddMilliseconds(16)) is null,"ordinary frames");
            gaps.Hidden();
            gaps.Hidden();   // HideUi may be fired twice in a row: one period
            Check(gaps.Frame(30_016,At.AddSeconds(30)) is null,"30 s of a cutscene: no gap note");
            Check(gaps.Long == 0 && gaps.HiddenPeriods == 1,"no long gap, one hidden period");
            Check(gaps.Frame(30_516,At.AddMilliseconds(30_516)) is { } note && note.StartsWith("Game frame gap: 500 ms",StringComparison.Ordinal),"a real gap after it is still seen");
            var summary = gaps.Summary(TimeSpan.FromMinutes(2));
            Check(summary.EndsWith("; 1 period(s) with the plugin interface hidden, not counted.",StringComparison.Ordinal),summary);
            Check(summary.Contains("the longest: 500 ms",StringComparison.Ordinal) && !summary.Contains("30000 ms",StringComparison.Ordinal),summary);
            var fresh = new FrameGaps();
            fresh.Hidden();
            Check(fresh.HiddenPeriods == 0 && fresh.Summary(TimeSpan.FromMinutes(2)).EndsWith("the longest: none.",StringComparison.Ordinal),"hidden before any frame: nothing to count, summary as before");
            var source = Read("src/TenshoAssistant/Diagnostics/FrameGaps.cs");
            Check(!source.Contains("Framework.Update each",StringComparison.Ordinal) && source.Contains("one call of the plugin interface's Draw each",StringComparison.Ordinal),"the summary names Draw");
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            var on = plugin.IndexOf("pluginInterface.UiBuilder.HideUi+=GapHidden;",StringComparison.Ordinal);
            var off = plugin.IndexOf("pluginInterface.UiBuilder.Draw-=Gap;\n                    pluginInterface.UiBuilder.HideUi-=GapHidden;",StringComparison.Ordinal);
            Check(on > 0 && off > on,"HideUi listened with Draw, and let go with it at the watch's end");
        }),
        ("Review 1437 M3: every French channel name and prefix taken from the game is in the raw French rows of the evidence",() =>
        {
            var evidence = Read("docs/evidence/0.1434-chat-logfilter.md");
            Check(evidence.Contains("XbmProbe \"<sqpack>\" French LogFilter",StringComparison.Ordinal),"the French command");
            using var fr = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json")));
            var names = fr.RootElement.GetProperty("ChatChannels");
            var notRaw = new HashSet<string>{"TellIn","TellOut","Emote","Crafting","Gathering","Progress","Announcement","Combat","Other","Ls2","Ls3","Ls4","Ls5","Ls6","Ls7","Cwls2","Cwls3","Cwls4","Cwls5","Cwls6","Cwls7"};
            foreach(var name in names.EnumerateObject().Where(name => !notRaw.Contains(name.Name)))
                Check(evidence.Contains("|" + name.Value.GetString() + "|",StringComparison.Ordinal),"raw row for " + name.Name + " (" + name.Value.GetString() + ")");
            foreach(var prefix in new[]{"[CL]","[RdN]","[LSIM 1]","[LSIM 8]"})
                Check(evidence.Contains("|" + prefix,StringComparison.Ordinal),"raw example with " + prefix);
            Check(fr.RootElement.GetProperty("ChatChannelsShort").GetProperty("Novice").GetString() == "RdN","the short name RdN");
        }),
    ];
}
