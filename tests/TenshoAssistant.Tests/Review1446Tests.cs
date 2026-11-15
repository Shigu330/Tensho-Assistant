using System.Text.RegularExpressions;
using TenshoAssistant.Diagnostics;
using TenshoAssistant.Link;

// 0.1446, X-443 and X-444 (review of block 0.1436-0.1445): the players' line of 0.1438 no longer lets one think the freeze of about a second
// is gone (I1: two loads out of twelve since 0.1438 froze from a blocking collection « induced for low memory »); the log lines the block
// cites are kept in docs/evidence and the getters counted right (M1); 0.1439's game-API leaves are gone (M2); a competitive cut ends the run
// of frames (M4); the link's serialization is prepared off the game's thread also when the link is turned on later (M5).
internal static class Review1446Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(30);

    /// <summary>The section of a version, in CHANGELOG.md or, once archived, in docs/changelog/.</summary>
    private static string Section(string version)
    {
        foreach (var file in new[] { Path.GetFullPath("CHANGELOG.md") }.Concat(Directory.GetFiles(Path.GetFullPath("docs/changelog"), "*.md")))
        {
            var text = Read(file);
            var start = text.IndexOf("\n## " + version + "\n", StringComparison.Ordinal);
            if (start < 0) continue;
            var end = text.IndexOf("\n## ", start + 4, StringComparison.Ordinal);
            return end < 0 ? text[start..] : text[start..end];
        }
        throw new Exception("no section " + version);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review 1446 (X-443): the players' lines of 0.1438 say a freeze of about a second can still happen, in both halves", () =>
        {
            var section = Section("0.1438.0");
            foreach (var claim in new[] { "(un gel d'environ une seconde y a été mesuré)", "(one freeze of about a second was measured there)" })
                Check(!section.Contains(claim, StringComparison.Ordinal), "still in 0.1438: " + claim);
            var french = section.Split('\n').Single(line => line.StartsWith("- Pour les joueurs : ", StringComparison.Ordinal));
            var english = section.Split('\n').Single(line => line.StartsWith("- For players: ", StringComparison.Ordinal));
            Check(french.Contains("Un gel d'environ une seconde peut encore arriver", StringComparison.Ordinal) && french.Contains("encore à l'étude", StringComparison.Ordinal), french);
            Check(english.Contains("A freeze of about a second can still happen", StringComparison.Ordinal) && english.Contains("still under study", StringComparison.Ordinal), english);
            foreach (var line in new[] { french, english }) Check(!PlayerNotes1182Tests.AgentWords.IsMatch(line), "plain words: " + line);
        }),
        ("Review 1446 (X-443): the evidence of the freezes keeps the two collections « induced for low memory » and says what is not proven", () =>
        {
            var evidence = Read("docs/evidence/0.1446-low-memory-collections.md");
            Check(evidence.Contains("Blocking collection of generation 2 (induced for low memory) from 18:42:25.527 to 18:42:26.333: 806 ms", StringComparison.Ordinal), "the raw line of 0.1443");
            Check(evidence.Contains("Blocking collection of generation 2 (induced for low memory) from 18:49:40.526 to 18:49:41.398: 871 ms", StringComparison.Ordinal), "the raw line of 0.1445");
            Check(evidence.Contains("Pas prouvé : ce qui manque de mémoire", StringComparison.Ordinal) && evidence.Contains("Not proven: what runs short of memory", StringComparison.Ordinal), "what is not proven, both halves");
            Check(Regex.Matches(evidence, @"^\| 0\.14\d\d \| \d\d:\d\d:\d\d \|", RegexOptions.Multiline).Count == 24, "the twelve loads in each half");
        }),
        ("Review 1446 (X-444 M1): the log lines of 0.1438 and 0.1445 are kept in docs/evidence, cited by the code; 18 getters, not 17", () =>
        {
            var guard = Read("docs/evidence/0.1438-reader-guard.md");
            Check(guard.Contains("Game frame gap: 994 ms without a frame, ended 18:11:53.389.", StringComparison.Ordinal), "the 994 ms gap");
            foreach (var reader in new[] { "CraftActionCatalogLoader.Update", "ItemUseCatalogLoader.Update", "ItemCatalogLoader.OnUpdate", "RecipeCatalogLoader.OnUpdate", "GatheringCatalogLoader.OnUpdate", "DalamudInventoryReader.OnUpdate" })
                Check(guard.Contains("Slow reader: " + reader, StringComparison.Ordinal), "the six readers: " + reader);
            Check(guard.Contains("the last one ready 25087 ms after the first subscribed", StringComparison.Ordinal), "the compiler's last class");
            var link = Read("docs/evidence/0.1445-link-json-getters.md");
            var names = link.Split('\n').Single(line => line.Contains("Slow reader: methods the game's thread compiled meanwhile (23)", StringComparison.Ordinal));
            Check(Regex.Matches(names, @"dynamicClass\.\w+Getter").Count == 18, "18 getters in the raw line");
            Check(Section("0.1445.0").Contains("dont 18 accesseurs", StringComparison.Ordinal) && Section("0.1445.0").Contains("18 of them getters", StringComparison.Ordinal), "the CHANGELOG says 18");
            Check(!Section("0.1445.0").Contains("17 accesseurs", StringComparison.Ordinal) && !Section("0.1445.0").Contains("17 of them", StringComparison.Ordinal), "no longer 17");
            Check(Read("src/TenshoAssistant/Diagnostics/ReaderCompiler.cs").Contains("docs/evidence/0.1438-reader-guard.md", StringComparison.Ordinal), "ReaderCompiler cites it");
            var feed = Read("src/TenshoAssistant/Link/LocalLinkFeed.Dalamud.cs");
            Check(feed.Contains("docs/evidence/0.1445-link-json-getters.md", StringComparison.Ordinal) && !feed.Contains("17 getters", StringComparison.Ordinal), "LocalLinkFeed cites it, with 18");
            Check(!Read("tests/TenshoAssistant.Tests/LinkPrepare1445Tests.cs").Contains("made 17 JSON getters", StringComparison.Ordinal), "the 0.1445 test header");
        }),
        ("Review 1446 (X-444 M2): the readers' compiler follows the plugin's own callees only; 0.1439's game-API leaves are gone", () =>
        {
            var graph = Read("src/TenshoAssistant/Diagnostics/CallGraph.cs");
            Check(!graph.Contains("GameApi", StringComparison.Ordinal) && !graph.Contains("Func<MethodBase,bool>? leaf", StringComparison.Ordinal), "no leaf rule");
            var warmup = Read("src/TenshoAssistant/Diagnostics/JitWarmup.cs");
            Check(warmup.Contains("CallGraph.Closure(methods,owner.Assembly,methods.Count+callees,stop).Where(", StringComparison.Ordinal) && !warmup.Contains("CallGraph.GameApi", StringComparison.Ordinal), "the readers' path");
            Check(!File.Exists(Path.GetFullPath("tests/TenshoAssistant.Tests/GameApiCallees1439Tests.cs")), "its test removed with it");
        }),
        ("Review 1446 (X-444 M4): a competitive cut ends the run of frames: the first frame after it is no gap; counted once in the summary", () =>
        {
            var gaps = new FrameGaps();
            var at = DateTimeOffset.Now;
            Check(gaps.Frame(0, at) is null && gaps.Frame(16, at) is null, "steady frames");
            gaps.Cut(); gaps.Cut(); gaps.Cut();   // every frame of the cut says it
            Check(gaps.Frame(60_000, at) is null, "the first frame after a minute of cut is not a gap");
            Check(gaps.Long == 0 && gaps.CutPeriods == 1, "no gap, one cut period: " + gaps.Long + ", " + gaps.CutPeriods);
            Check(gaps.Frame(60_200, at) is { } note && note.StartsWith("Game frame gap: 200 ms", StringComparison.Ordinal), "the run goes on after the cut");
            var summary = gaps.Summary(TimeSpan.FromMinutes(2));
            Check(summary.Contains("; 1 competitive cut(s), not counted.", StringComparison.Ordinal) && summary.Contains("1 gaps of 150 ms or more", StringComparison.Ordinal), summary);
            Check(!new FrameGaps().Summary(TimeSpan.FromMinutes(2)).Contains("competitive", StringComparison.Ordinal), "nothing said without a cut");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("void Gap(){if(TenshoAssistant.Competitive.CompetitiveGate.Cut){gaps.Cut();return;}", StringComparison.Ordinal), "the plugin ends the run at a cut");
        }),
        ("Review 1446 (X-444 M5): the preparation runs once, on another thread; the feed publishes nothing before it has run, also for a link turned on later", () =>
        {
            using var hold = new ManualResetEventSlim(false);
            var calls = 0;
            var thread = Environment.CurrentManagedThreadId;
            var ran = -1;
            var preparation = new LinkPreparation(() => { Interlocked.Increment(ref calls); ran = Environment.CurrentManagedThreadId; hold.Wait(Long); return "line"; });
            Check(!preparation.Ready(), "not ready while it runs");
            Check(!preparation.Ready() && preparation.Started == 1, "started once however often asked");
            hold.Set();
            var until = DateTime.UtcNow + Long;
            while (!preparation.Ready() && DateTime.UtcNow < until) Thread.Sleep(5);
            Check(preparation.Ready() && Volatile.Read(ref calls) == 1 && preparation.Started == 1, "ready after it ran, once");
            Check(ran != thread, "on another thread");
            var failed = new LinkPreparation(() => null);
            failed.Start(); failed.Start();
            until = DateTime.UtcNow + Long;
            while (!failed.Ready() && DateTime.UtcNow < until) Thread.Sleep(5);
            Check(failed.Ready() && failed.Started == 1, "a failed preparation does not stop the link");
            var feed = Read("src/TenshoAssistant/Link/LocalLinkFeed.Dalamud.cs");
            var update = feed.IndexOf("private void OnUpdate(IFramework _)", StringComparison.Ordinal);
            var off = feed.IndexOf("if(!configuration.DesktopLink){Stop();return;}", update, StringComparison.Ordinal);
            var ready = feed.IndexOf("if(!prepared.Ready())return;", update, StringComparison.Ordinal);
            var publish = feed.IndexOf("server.Publish(", update, StringComparison.Ordinal);
            Check(update > 0 && off > update && ready > off && publish > ready, "after the setting is read, before the server and the first serialization");
            Check(feed.Contains("if(configuration.DesktopLink)prepared.Start();", StringComparison.Ordinal) && !feed.Contains("Task.Run(LocalLinkProtocol.Prepare)", StringComparison.Ordinal), "started early when the link is on at the creation");
        }),
    ];
}
