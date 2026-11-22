using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Collections;
using TenshoAssistant.Inventory;

// X-415 (0.1389, review of 0.1375 to 0.1382, minors): M1 the setting cited under its current name in the living documents (ratchet, both
// languages); M2 behaviour tests (R28.14) of X-336 M4 for the achievements and the gil journal: a file of before written again but held by
// another client is neither merged nor set aside, then merged once free; M4 one way to write the server bar's time left. X-574 (0.1793, the
// user's choice of 17/11): M2's files of before are no longer read nor merged at all, held or free; its cases say that now.
internal static class ReviewMinors1389Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1389-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static TenshoDataFile Data(string root) => new(Path.Combine(root, TenshoDataDatabase.FileName), (_, _) => { });
    private static FileStream Hold(string file) => new(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1389 M1: the living documents cite « Game data and what you observed only » under its current name", () =>
        {
            var files = new List<string> { "README.md", "CONTRIBUTING.md", "docs/FAQ.md" };
            files.AddRange(new[] { "ARCHITECTURE", "PUBLISHING", "REFERENCES", "SETTINGS" }.Select(name => "docs/project/" + name + ".md"));
            var old = new[] { "observées seulement", "game and observed data only", "Game data and observed" };
            var found = files.Where(file => File.Exists(Path.GetFullPath(file)))
                .Where(file => old.Any(text => File.ReadAllText(Path.GetFullPath(file)).Contains(text, StringComparison.OrdinalIgnoreCase))).ToList();
            Check(found.Count == 0, "the former name in " + string.Join(", ", found));
        }),
        ("Review minors 1389 M2: an achievement file written again and held is never read nor set aside; free, still not read (X-574)", () => InTemp(root =>
        {
            var file = Path.Combine(root, "AchievementProgress.json");
            new AchievementProgressData(file, Data(root)).Save(7, new Dictionary<uint, AchievementProgress> { [1] = new(1, 10, At) }, []);
            File.WriteAllText(file, AchievementProgressStore.Write(new() { [7] = new() { [2] = new(4, 10, At.AddHours(1)) } }));
            var bytes = File.ReadAllBytes(file);
            var hold = Hold(file);
            try
            {
                var held = new AchievementProgressData(file, Data(root)).Load(At);
                Check(held[7].Count == 1 && !held[7].ContainsKey(2), "held: its reading not read");
            }
            finally { hold.Dispose(); }
            Check(File.ReadAllBytes(file).SequenceEqual(bytes), "held while read: left in place, unchanged");
            var all = new AchievementProgressData(file, Data(root)).Load(At);
            Check(all[7].Count == 1 && all[7][1].Current == 1, "free: still not read nor merged: " + all[7].Count);
            Check(File.ReadAllBytes(file).SequenceEqual(bytes) && Directory.GetDirectories(root, "before-sqlite-*").Length == 0, "never set aside");
        })),
        ("Review minors 1389 M2: a gil journal written again and held is never read nor set aside; free, still not read (X-574)", () => InTemp(root =>
        {
            var folder = Path.Combine(root, "GilJournal"); Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, GilJournalStore.FileName(7));
            var one = new GilEntry(At, 100, 100, GilSource.Character, GilCategory.Other);
            var two = new GilEntry(At.AddHours(1), 50, 150, GilSource.Character, GilCategory.Other);
            new GilJournalStore(folder, Data(root)).Save(7, [one]);
            GilJournal.Write(file, [two]);
            var bytes = File.ReadAllBytes(file);
            var hold = Hold(file);
            try
            {
                var held = new GilJournalStore(folder, Data(root)).Load(7, At);
                Check(held.SequenceEqual([one]), "held: its entry not read");
            }
            finally { hold.Dispose(); }
            Check(File.ReadAllBytes(file).SequenceEqual(bytes), "held while read: left in place, unchanged");
            var journal = new GilJournalStore(folder, Data(root)).Load(7, At);
            Check(journal.SequenceEqual([one]) && File.ReadAllBytes(file).SequenceEqual(bytes) && Directory.GetDirectories(folder, "before-sqlite-*").Length == 0, "free: still not read, never set aside");
        })),
        ("Review minors 1389 M4: one way to write a time left in the server bar (no TenshoCulture.Days)", () =>
        {
            var sources = Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                .Where(path => File.ReadAllText(path).Contains("TenshoCulture.Days(", StringComparison.Ordinal)).Select(Path.GetFileName).ToList();
            Check(sources.Count == 0, "TenshoCulture.Days still used: " + string.Join(", ", sources));
            var bar = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Alarms/AlarmDtrEntry.Dalamud.cs"));
            Check(bar.Contains("=>UI.TimeLeft.Remaining(left,text);", StringComparison.Ordinal), "the server bar writes through TimeLeft (Write)");
        }),
    ];
}
