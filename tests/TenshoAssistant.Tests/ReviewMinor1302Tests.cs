using System.Diagnostics;
using System.Text.RegularExpressions;
using TenshoAssistant;
using TenshoAssistant.Beastmaster;
using TenshoAssistant.Desktop;

// 0.1302 (X-284, review of block 0.1278, minor points): M1 the beast team plans saved off the game's thread (a file held by another
// program made AtomicFile wait up to about 750 ms on the frame); M2 the reference pictures of the error state (a hook in the plugin:
// the pages forget their sheets); M4 the test run's last line no longer says « N/N passed » when a quarantine entry fails the run.
// (M3, a page never still stops the gate, is in the agent's images.py, its selftest.)
internal static class ReviewMinor1302Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static BeastTeams.SavedPlans Plans(int rank) => new([], new() { [7] = "key" }, new() { [1] = rank });

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minor 1302 M1: a held file never makes the caller wait; the plans are written once it is free, the last one kept", () =>
        {
            var folder = Path.Combine(Path.GetTempPath(), "tensho-tests", "beast-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var file = Path.Combine(folder, "BeastTeams.json");
                File.WriteAllText(file, "old");
                var warnings = new List<string>();
                var writer = new BackgroundWriter((_, message) => { lock (warnings) warnings.Add(message); });
                BeastTeams.Write(Plans(1));   // the serializer warmed up: the time measured below is the save's own
                var held = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                var clock = Stopwatch.StartNew();
                BeastTeams.SaveLater(writer, file, Plans(2));
                BeastTeams.SaveLater(writer, file, Plans(3));
                var spent = clock.Elapsed;
                // A write on this thread would wait here for the file (AtomicFile tries again 50, 100, 150… ms): the caller went on at once.
                Check(spent < TimeSpan.FromMilliseconds(150), $"the caller (the game's thread) did not wait: {spent.TotalMilliseconds:0} ms");
                Thread.Sleep(250);
                Check(File.Exists(file) && warnings.Count == 0, "still waiting for the file, nothing lost");
                held.Dispose();
                Check(writer.Flush(TimeSpan.FromSeconds(10)), "written once the file was free");
                Check(warnings.Count == 0, "no write failed: " + string.Join("; ", warnings));
                var saved = BeastTeams.Read(File.ReadAllText(file));
                Check(saved?.Ranks?[1] == 3, "the last plans asked are on disk, in order");
                Check(Directory.GetFiles(folder).Length == 1, "no temporary file left");
            }
            finally { try { Directory.Delete(folder, true); } catch (IOException) { } }
        }),
        ("Review minor 1302 M1: the window saves the beast plans through the writer, flushed when it is disposed", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            var save = page[page.IndexOf("private void SaveBeastPlans()", StringComparison.Ordinal)..];
            save = save[..save.IndexOf("internal BoardPlan? BeastPlanFor", StringComparison.Ordinal)];
            Check(save.Contains("BeastTeams.SaveLater(beastWriter ??= new BackgroundWriter(", StringComparison.Ordinal) && !save.Contains("AtomicFile.Write", StringComparison.Ordinal), "off the game's thread");
            Check(Read("src/TenshoAssistant/MainWindow.Lifetime.cs").Contains("FlushBeastPlans();", StringComparison.Ordinal), "the last write waited for at unload");
        }),
        ("Review minor 1302 M2: an error picture must show an error; the others neither an error nor a wait", () =>
        {
            Check(ReferenceErrors.ShownFor(["Allied.Title", "Allied.SheetsUnavailable"], "error").Length == 0, "an error picture showing its error");
            Check(ReferenceErrors.ShownFor(["Allied.Title"], "error").SequenceEqual(new[] { ReferenceErrors.NoErrorShown }), "an error picture showing no error is refused");
            Check(ReferenceErrors.ShownFor(["Relics.SheetsUnavailable", "Relics.Loading"], "error").SequenceEqual(new[] { "Relics.Loading" }), "an error picture still waiting is refused");
            Check(ReferenceErrors.ShownFor(["Allied.SheetsUnavailable"], "").SequenceEqual(new[] { "Allied.SheetsUnavailable" }), "a page picture showing an error is refused");
            Check(ReferenceErrors.ShownFor(["Weather.Loading"], "loading").Length == 0 && ReferenceErrors.ShownFor(["Weather.Loading"], "").Length == 1, "a loading picture may wait");
            var plan = ReferencePlan.Plan(DesktopPages.AsIs.Concat(DesktopPages.NeedsLink));
            Check(ReferencePlan.ErrorPages.All(page => plan.Any(shot => shot.Id == page + "-error" && shot.Page == page && shot.Setup == "error")), "each error page pictured");
            Check(plan.Select(shot => shot.Id).Distinct().Count() == plan.Count, "one name per picture");
        }),
        ("Review minor 1302 M2: the hook: the pages forget their sheets; the pictures make the sheets fail, then give them back", () =>
        {
            var loaders = Read("src/TenshoAssistant/MainWindow.SheetLoaders.cs");
            Check(loaders.Contains("alliedCatalog=null;alliedTried=false;", StringComparison.Ordinal) && loaders.Contains("relicCatalog=null;relicCatalogTried=false;relicCatalogTask=null;", StringComparison.Ordinal), "the two error pages forget");
            var references = Read("src/TenshoAssistant.Desktop/References.cs");
            Check(references.Contains("if(shot.Setup==\"error\" && !host.SheetsFail(true))", StringComparison.Ordinal) && references.Contains("if(shot.Setup==\"error\")host.SheetsFail(false);", StringComparison.Ordinal), "failed for the picture, given back after");
            Check(references.Contains("var shown=ReferenceErrors.ShownFor(asked,shot.Setup);", StringComparison.Ordinal), "the picture's check follows its setup");
            Check(Read("src/TenshoAssistant.Desktop/DesktopContract.cs").Contains("new(Main,Kind.Method,\"ForgetSheetPages\",false,[],", StringComparison.Ordinal), "in the desktop contract");
        }),
        ("Review minor 1302 M4: a wrong quarantine entry: the last line says the run failed, never « N/N passed »", () =>
        {
            var results = new[] { new TestResult("G", "a", true, null, 0, null), new TestResult("G", "b", true, null, 0, null) };
            var clean = new TestRun(results, [], 1, 1);
            Check(ParallelTests.CountLine(clean) == "2/2 passed", "a clean run");
            var wrong = new TestRun(results, ["FAIL quarantine without a reason: x"], 1, 1);
            var line = ParallelTests.CountLine(wrong);
            Check(wrong.Failed == 1 && line.Contains("FAILED", StringComparison.Ordinal) && !Regex.IsMatch(line, "^[0-9]+/[0-9]+ passed"), "said, and not read as a pass by the scripts: " + line);
            var failing = new TestRun([results[0], new TestResult("G", "b", false, "x", 0, null)], [], 1, 1);
            Check(ParallelTests.CountLine(failing) == "1/2 passed", "a failed test is counted as before");
        }),
    ];
}
