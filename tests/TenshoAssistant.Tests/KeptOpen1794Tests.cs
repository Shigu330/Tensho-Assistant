using System.Numerics;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;
using TenshoAssistant.Inventory;

// X-750 (0.1794, idea I-024 chosen by the user on 21/11): the SQLite files kept open while the plugin runs (SqliteKeeper), so that a save's
// close no longer folds the journal into the file (Pooling=false: every close of the last connection did, up to 1.7 s measured under load);
// the journal folded in at the unload's Close. Behaviour tests on real files (R28.14: two saves close together, a reload, a save after the
// close) and the wiring (kept open off the frame at the start, closed at unload).
internal static class KeptOpen1794Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset At = new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1794-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static List<GilEntry> Gil(int count, long start)
        => Enumerable.Range(0, count).Select(index => new GilEntry(At.AddMinutes(index), 100 + index, start + index, GilSource.Character, (GilCategory)(index % 9))).ToList();

    private static bool Same(IReadOnlyList<GilEntry> left, IReadOnlyList<GilEntry> right)
        => left.Count == right.Count && left.Zip(right).All(pair => pair.First == pair.Second);

    // The journal still to fold in: the -wal file holds something.
    private static long Journal(string database) => File.Exists(database + "-wal") ? new FileInfo(database + "-wal").Length : 0;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Kept open 1794: behaviour: a save's close folds the journal in only when the file is not kept open", () => InTemp(root =>
        {
            var file = Path.Combine(root, TenshoDataDatabase.FileName);
            var plain = new TenshoDataFile(file, (error, message) => throw new Exception(message, error));
            new GilJournalStore(Path.Combine(root, "GilJournal"), plain).Save(1, Gil(5, 1000));
            Check(Journal(file) == 0, "not kept open: the save's close (the last connection) folded the journal in, as before X-750");
            var kept = new TenshoDataFile(file, (error, message) => throw new Exception(message, error));
            kept.KeepOpen();
            Check(kept.Database()!.Kept, "kept open");
            new GilJournalStore(Path.Combine(root, "GilJournal"), kept).Save(2, Gil(5, 2000));
            Check(Journal(file) > 0, "kept open: the save's close left the journal for later (no fold at each close)");
            kept.Close();
            Check(Journal(file) == 0 && !kept.Database()!.Kept, "closed at unload: the journal folded in, nothing kept");
        })),
        ("Kept open 1794: behaviour: two saves close together while kept open, then the unload and a reload by a fresh instance: both whole", () => InTemp(root =>
        {
            var file = Path.Combine(root, TenshoDataDatabase.FileName);
            var lines = new List<string>();
            var data = new TenshoDataFile(file, (error, message) => throw new Exception(message, error), line => { lock (lines) lines.Add(line); });
            data.KeepOpen();
            Check(lines.Count(line => line.Contains("(X-750).", StringComparison.Ordinal)) == 1 && lines.Any(line => line.StartsWith("Tensho data: a save took ", StringComparison.Ordinal)),
                "the save timed both ways, said once: " + string.Join(" | ", lines));
            var store = new GilJournalStore(Path.Combine(root, "GilJournal"), data);
            var first = Gil(40, 10_000);
            var second = Gil(60, 20_000);
            for (var round = 0; round < 10; round++)
                Parallel.Invoke(() => store.Save(7, first), () => store.Save(8, second));
            data.Close();
            var again = new TenshoDataFile(file, (error, message) => throw new Exception(message, error));
            var reloaded = new GilJournalStore(Path.Combine(root, "GilJournal"), again);
            Check(Same(reloaded.Load(7), first) && Same(reloaded.Load(8), second), "both journals whole after the reload");
            using var open = again.Database()!.Open();
            Check(Convert.ToInt64(TenshoDataDatabase.Scalar(open, "SELECT COUNT(*) FROM save_probe;")) == 1, "the timing wrote one probe row, nothing else");
        })),
        ("Kept open 1794: behaviour: closed is closed: kept again refused, a later save still written (its own connection), Close twice harmless", () => InTemp(root =>
        {
            var file = Path.Combine(root, TenshoDataDatabase.FileName);
            var data = new TenshoDataFile(file, (error, message) => throw new Exception(message, error));
            data.KeepOpen();
            data.Close();
            data.KeepOpen();
            Check(!data.Database()!.Kept && !data.Database()!.KeepOpen(), "not kept again after the unload's close");
            new GilJournalStore(Path.Combine(root, "GilJournal"), data).Save(3, Gil(3, 300));
            data.Close();
            Check(Journal(file) == 0, "a save after the close folds the journal in at its own close, as before");
            Check(Same(new GilJournalStore(Path.Combine(root, "GilJournal"), new TenshoDataFile(file, (error, message) => throw new Exception(message, error))).Load(3), Gil(3, 300)),
                "the later save read back");
            // Closed before it was ever kept (an unload during the start): never kept afterwards.
            var early = new TenshoDataFile(Path.Combine(root, "early.db"), (error, message) => throw new Exception(message, error));
            early.Close();
            early.KeepOpen();
            Check(!early.Database()!.Kept, "an unload before the start's keep: not kept");
        })),
        ("Kept open 1794: behaviour: the discoveries database kept open: saves keep their journal, the close folds it in, a reload reads every fact", () => InTemp(root =>
        {
            var file = Path.Combine(root, DiscoveryDatabase.FileName);
            var database = new DiscoveryDatabase(file);
            Check(database.KeepOpen() && !database.KeepOpen(), "kept once");
            var store = new DiscoveryStore();
            var place = new Vector3(10, 0, 20);
            for (var sighting = 0; sighting < 6; sighting++) store.SeeSource(DiscoveryKind.FishCatch, 4869, 1, 3 + sighting, At.AddMinutes(sighting), 135, 16, place);
            var facts = store.Snapshot(At).Facts;
            database.Write(facts, [], false);
            Check(Journal(file) > 0, "the save's close left the journal for later");
            database.Close();
            Check(Journal(file) == 0, "closed: folded in");
            Check(new DiscoveryDatabase(file).ReadMine().Facts.Count == facts.Count, "every fact read back by a fresh instance");
        })),
        ("Kept open 1794: the plugin keeps both files open off the frame at its start and closes them at unload; the saves stay off the frame", () =>
        {
            var persistence = Read("src/TenshoAssistant/Plugin.Persistence.cs");
            Check(persistence.Contains("registrations.Register(()=>_=Task.Run(tenshoData.KeepOpen),tenshoData.Close);", StringComparison.Ordinal)
                  && persistence.IndexOf("tenshoData.Close", StringComparison.Ordinal) < persistence.IndexOf("bank=new InventoryBankService(", StringComparison.Ordinal),
                "the data file kept open off the frame, registered before the bank: released after it (reverse order)");
            var discoveries = Read("src/TenshoAssistant/Plugin.Discoveries.cs");
            Check(discoveries.Contains("database.KeepOpen();", StringComparison.Ordinal) && discoveries.Contains("_=Task.Run(async()=>", StringComparison.Ordinal), "the discoveries database kept open by the load, which runs off the frame");
            var flush = Read("src/TenshoAssistant/MainWindow.Discoveries.cs");
            Check(flush.IndexOf("DiscoveryDatabase?.Close();", StringComparison.Ordinal) > flush.IndexOf("DiscoveryDatabase?.Checkpoint();", StringComparison.Ordinal), "closed at unload, after the last save and its checkpoint");
            var database = Read("src/TenshoAssistant/TenshoDataDatabase.cs");
            Check(database.Contains("Pooling=false", StringComparison.Ordinal) && database.Contains("public sealed class SqliteKeeper", StringComparison.Ordinal),
                "connections still not pooled (UnloadRelease: nothing to prune); one kept connection instead");
            // The saves of the data file all run off the frame: the bank's queue, the background writers, the completion lists' task.
            Check(Read("src/TenshoAssistant/Inventory/BankService.cs").Contains("_ = Task.Run(RunAsync);", StringComparison.Ordinal), "bank saves on their own task");
            Check(Read("src/TenshoAssistant/Collections/CollectionReader.Completion.Dalamud.cs").Contains("_=Task.Run(()=>\n            {\n                try{lists.Save(list);}", StringComparison.Ordinal), "completion lists saved on a task");
        }),
    ];
}
