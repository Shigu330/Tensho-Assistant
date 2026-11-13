// 0.1202 (relay 37, first point): the slowest tests of Verify made quicker, nothing taken from them; « --only TEXT » for targeted runs.
internal static class TestFilter
{
    /// <summary>« --only TEXT » keeps the tests whose name holds TEXT (case ignored); any other arguments keep them all.</summary>
    public static (string Name, Action Run)[] Only((string Name, Action Run)[] tests, string[] args)
    {
        if (args.Length != 2 || args[0] != "--only" || args[1].Length == 0) return tests;
        return tests.Where(test => test.Name.Contains(args[1], StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}

internal static class VerifySpeed1202Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void InTemp(Action<string> run)
    {
        var folder = Path.Combine(Path.GetTempPath(), "tensho-1202-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try { run(folder); }
        finally { try { Directory.Delete(folder, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Verify speed 1202: « --only TEXT » keeps the tests whose name holds it, case ignored; no argument, or any other, keeps them all", () =>
        {
            (string Name, Action Run)[] tests = [("Alpha one", () => { }), ("beta two", () => { }), ("Gamma", () => { })];
            Check(TestFilter.Only(tests, ["--only", "ONE"]).Select(test => test.Name).SequenceEqual(["Alpha one"]), "one kept");
            Check(TestFilter.Only(tests, ["--only", "a"]).Length == 3, "every name with an a");
            Check(TestFilter.Only(tests, ["--only", "nothing"]).Length == 0, "none");
            Check(TestFilter.Only(tests, []).Length == 3 && TestFilter.Only(tests, ["--only"]).Length == 3 && TestFilter.Only(tests, ["--only", ""]).Length == 3
                && TestFilter.Only(tests, ["--other", "a"]).Length == 3, "a full run keeps them all");
            var program = File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/Program.cs")).ReplaceLineEndings("\n");
            var filter = program.IndexOf("allTests = TestFilter.Only(allTests, args);", StringComparison.Ordinal);
            Check(filter > 0 && filter < program.IndexOf("foreach (var test in allTests)", StringComparison.Ordinal), "wired before the run");
        }),
        ("Verify speed 1202: the Universalis pause stays 3 s in every service the plugin makes; only tests shorten it", () =>
        {
            using var market = new TenshoAssistant.Market.MarketService(new HttpClient());
            using var board = new TenshoAssistant.Market.MarketBoardService(new HttpClient());
            Check(TenshoAssistant.Market.MarketService.UsualRequestPause == TimeSpan.FromSeconds(3) && market.RequestPause == TimeSpan.FromSeconds(3) && board.RequestPause == TimeSpan.FromSeconds(3), "3 s by default");
            var source = string.Join("\n", Directory.EnumerateFiles(Path.GetFullPath("src"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
            Check(!System.Text.RegularExpressions.Regex.IsMatch(source, @"(?<![A-Za-z])RequestPause\s*=(?!=)"), "never set outside the tests");
            foreach (var file in new[] { "src/TenshoAssistant/Market/MarketService.cs", "src/TenshoAssistant/Market/MarketBoardService.cs" })
                Check(!File.ReadAllText(Path.GetFullPath(file)).Contains("FromSeconds(3)-", StringComparison.Ordinal), "one pause, no second 3 s written by hand: " + file);
        }),
        ("Bank file 1202: behaviour: the saved file held an instant by another program: the replace is tried again, the new version in place, the old one as backup", () => InTemp(folder =>
        {
            var path = Path.Combine(folder, "4.json"); var backup = path + ".bak"; var next = path + ".new.tmp";
            File.WriteAllText(path, "old"); File.WriteAllText(next, "new");
            var holder = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); // no FileShare.Delete: as a scanner reading it
            var failed = new List<int>();
            try { TenshoAssistant.AtomicFile.ReplaceWhenFree(next, path, backup, wait: attempt => { failed.Add(attempt); holder.Dispose(); }); }
            finally { holder.Dispose(); }
            Check(failed.SequenceEqual([1]), "refused once while held, then done: " + string.Join(",", failed));
            Check(File.ReadAllText(path) == "new" && File.ReadAllText(backup) == "old" && !File.Exists(next), "the new version in place, the old one kept as the backup");
            // Held for every attempt: an error at the last one, and the saved file as it was.
            File.WriteAllText(next, "newer");
            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var waits = 0; var thrown = false;
                try { TenshoAssistant.AtomicFile.ReplaceWhenFree(next, path, backup, attempts: 3, wait: _ => waits++); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { thrown = true; }
                Check(thrown && waits == 2, "given up after the last attempt: " + waits);
            }
            Check(File.ReadAllText(path) == "new" && File.ReadAllText(next) == "newer", "nothing replaced, the new version left to its caller");
            // No file in place yet: moved.
            var fresh = Path.Combine(folder, "5.json"); File.WriteAllText(next, "first");
            TenshoAssistant.AtomicFile.ReplaceWhenFree(next, fresh, fresh + ".bak");
            Check(File.ReadAllText(fresh) == "first" && !File.Exists(next) && !File.Exists(fresh + ".bak"), "moved when there is no file yet");
        })),
        ("Bank file 1202: behaviour: a bank save while another program holds the file for a moment is not lost (the fallback JSON store)", () => InTemp(folder =>
        {
            var path = Path.Combine(folder, "bank.json");
            var store = new TenshoAssistant.Inventory.FileInventoryBankStore(path);
            store.Save([]);
            var holder = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var release = Task.Run(() => { Thread.Sleep(120); holder.Dispose(); });
            try { store.Save([]); }
            finally { release.Wait(5000); holder.Dispose(); }
            Check(File.Exists(path) && File.Exists(path + ".bak") && !Directory.EnumerateFiles(folder, "*.tmp").Any(), "saved once the file is free, the copy kept, no temporary file left");
            var wiring = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/BankFileStore.cs"));
            Check(wiring.Contains("AtomicFile.ReplaceWhenFree(temporary, path, path + \".bak\");", StringComparison.Ordinal) && !wiring.Contains("File.Replace(", StringComparison.Ordinal), "the store replaces through it");
        })),
    ];
}
