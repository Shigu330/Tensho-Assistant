using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;
using TenshoAssistant.Inventory;

// 0.1173 (X-182 continued, night review N3 for the bank and the discoveries, N26): two game clients, or two versions of the plugin the same day.
// Behaviour tests (R28: data loss, concurrency), on real files in a temporary folder: a bank character file written again after the conversion (an
// older version still running) is merged storage by storage, the newest reading of each kept and the others staying; an older reading never takes
// back a newer one; a Discoveries.json written again is merged without adding counts up (a whole base saved again is not doubled), a copy already
// set aside is not read again; « unchanged, not written » is decided against the database: a storage the other client forgot is written again.
// X-574 (0.1793, the user's choice of 17/11): nothing is converted nor merged any more: a file of before written again is never read, the
// database's own values stay (the cases about the merges say that now).
internal static class ConcurrentData1173Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = DateTimeOffset.UtcNow.AddDays(-2);

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1173-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static TenshoDataFile Data(string root) => new(Path.Combine(root, TenshoDataDatabase.FileName), (_, _) => { });
    private static PerCharacterBankStore Bank(string root) => new(Path.Combine(root, "InventoryBank"), Path.Combine(root, "InventoryBank.json"), 10_000, Data(root));
    private static BankEntry Retainer(ulong retainer, DateTimeOffset at, uint item = 5057, int quantity = 3)
    {
        var owner = new InventoryOwnerId(retainer);
        var ids = BankStorageScope.ContainerIds(BankStorageKind.Retainer);
        var containers = ids.Select(id => new InventoryContainerSnapshot(new(owner, new(id)), InventoryCoverage.Complete,
            id == ids[0] ? [new InventoryStack(new(new(item), false), quantity, 0)] : [])).ToArray();
        return new(new(7, BankStorageKind.Retainer, retainer), "Servant " + retainer, new(at, "test", containers));
    }
    private static T Await<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
    private static InventoryBankService Ready(PerCharacterBankStore store)
    {
        var bank = new InventoryBankService(store, TimeSpan.FromMilliseconds(100));
        bank.Ready.WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
        Check(bank.State.Error is null, "bank loaded: " + bank.State.Error);
        return bank;
    }
    private static long Quantity(BankEntry entry) { long sum = 0; foreach (var container in entry.Snapshot.Containers.Values) foreach (var stack in container.Stacks) sum += stack.Quantity; return sum; }
    private static string[] Aside(string root) => Directory.GetDirectories(root, "before-sqlite-*").SelectMany(directory => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)).ToArray();
    // The older version writes again, after the conversion: its file's date set past the conversion's, as a later save would have it.
    private static void Later(string path) => File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(5));
    private static DiscoveryFact Fact(uint subject, int seen, DateTimeOffset last) => new() { Kind = DiscoveryKind.FishCatch, Subject = subject, Detail = 1, Territory = 135, Map = 16,
        CellX = DiscoveryStore.Cell(120, -40).X, CellZ = DiscoveryStore.Cell(120, -40).Z, X = 120, Z = -40, LevelMin = 1, LevelMax = 1, Seen = seen, First = At.AddDays(-3), Last = last };
    private static void WriteBase(string root, params DiscoveryFact[] facts) => DiscoveryFiles.Write(Path.Combine(root, "Discoveries.json"), new DiscoveryFile { Written = DateTimeOffset.UtcNow, Facts = [.. facts] });
    private static DiscoveryFact Mine(string root, uint subject) => new DiscoveryDatabase(Path.Combine(root, DiscoveryDatabase.FileName)).ReadMine().Facts.Single(fact => fact.Subject == subject);
    private static (string Name, byte[] Bytes)[] Files(string folder) => !Directory.Exists(folder) ? [] : Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
        .Order(StringComparer.Ordinal).Select(path => (path, File.ReadAllBytes(path))).ToArray();
    private static bool Same((string Name, byte[] Bytes)[] left, (string Name, byte[] Bytes)[] right)
        => left.Length == right.Length && left.Zip(right).All(pair => pair.First.Name == pair.Second.Name && pair.First.Bytes.SequenceEqual(pair.Second.Bytes));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Concurrent data 1173: a bank file written again (an older version still running) is never read: the data file's storages stay, the file untouched (X-574)", () => InTemp(root =>
        {
            var folder = Path.Combine(root, "InventoryBank");
            var file = Path.Combine(folder, PerCharacterBankStore.FileName(7));
            var store = Bank(root);
            store.Load();
            store.Save([Retainer(1, At, quantity: 3), Retainer(2, At, quantity: 4)]);
            // The older version finds no folder, reads servant 1 again and saves only what it saw.
            new FileInventoryBankStore(file).Save([Retainer(1, At.AddHours(1), quantity: 9)]);
            Later(file);
            var bytes = Files(folder);
            var loaded = Bank(root).Load().OrderBy(entry => entry.Key.StorageId).ToArray();
            Check(loaded.Length == 2, "both storages of the data file: " + loaded.Length);
            Check(loaded[0].Snapshot.ObservedAt == At && Quantity(loaded[0]) == 3, "servant 1: the data file's reading, the file's not read");
            Check(loaded[1].Snapshot.ObservedAt == At, "servant 2 unchanged");
            Check(bytes.Length > 0 && Same(Files(folder), bytes) && Aside(root).Length == 0, "the file of before left as it is, nothing set aside");
        })),
        ("Concurrent data 1173: an older reading in a bank file written again never takes back a newer one; a damaged file is never read (N3, X-574)", () => InTemp(root =>
        {
            var folder = Path.Combine(root, "InventoryBank");
            var store = Bank(root);
            store.Load();
            store.Apply(new BankChange(Put: Retainer(1, At.AddHours(2), quantity: 5)), []);
            var file = Path.Combine(folder, PerCharacterBankStore.FileName(7));
            new FileInventoryBankStore(file).Save([Retainer(1, At, quantity: 1), Retainer(3, At, quantity: 2)]);
            Later(file);
            var loaded = Bank(root).Load().OrderBy(entry => entry.Key.StorageId).ToArray();
            Check(loaded.Length == 1 && loaded[0].Snapshot.ObservedAt == At.AddHours(2) && Quantity(loaded[0]) == 5, "servant 1 keeps the newer reading; nothing taken from the file");
            File.WriteAllText(file, "{ not a bank");
            Later(file);
            Check(Bank(root).Load().Length == 1, "a damaged file: not read, the bank still loads");
            Check(File.ReadAllText(file) == "{ not a bank" && Aside(root).Length == 0, "the damaged file left where it is");
        })),
        ("Concurrent data 1173: « unchanged » is decided against the database: a storage the other client forgot is written again (N26)", () => InTemp(root =>
        {
            var first = Ready(Bank(root));
            var second = Ready(Bank(root));
            var reading = Retainer(1, DateTimeOffset.UtcNow.AddMinutes(-10));
            Check(Await(second.ImportAsync(reading)).Success, "the second client saves the servant");
            using (var reloaded = Ready(Bank(root))) Check(reloaded.State.Entries.Count == 1, "saved");
            Check(Await(first.ForgetAsync(reading.Key)).Success, "the first client forgets it");
            Check(Bank(root).Load().Length == 0, "forgotten in the database");
            Check(Await(second.ImportAsync(Retainer(1, reading.Snapshot.ObservedAt.AddMinutes(2)))).Success, "the same contents read again within the hour");
            var third = Bank(root).Load();
            Check(third.Length == 1, "a third client sees the servant again (the old code skipped the write on its memory)");
            Check(Await(second.ImportAsync(Retainer(1, reading.Snapshot.ObservedAt.AddMinutes(4)))).Success, "read again, unchanged");
            Check(Bank(root).Load()[0].Snapshot.ObservedAt == reading.Snapshot.ObservedAt.AddMinutes(2), "unchanged in the database under the hour: not written (0.605 kept)");
            first.Dispose(); second.Dispose();
        })),
        ("Concurrent data 1173: a Discoveries.json written again beside the database is never read nor merged: the database keeps its own counts (X-574)", () => InTemp(root =>
        {
            var database = new DiscoveryDatabase(Path.Combine(root, DiscoveryDatabase.FileName));
            database.Write([Fact(4869, 2, At)], [], false);
            // An older version saves its whole base again, with one more sighting and a new cell.
            WriteBase(root, Fact(4869, 3, At.AddHours(1)), Fact(4870, 1, At.AddHours(1)));
            Later(Path.Combine(root, "Discoveries.json"));
            var bytes = File.ReadAllBytes(Path.Combine(root, "Discoveries.json"));
            var restarted = new DiscoveryDatabase(Path.Combine(root, DiscoveryDatabase.FileName));
            Check(restarted.ReadMine().Facts.Count == 1 && Mine(root, 4869).Seen == 2, "the database's count only: " + Mine(root, 4869).Seen);
            Check(DiscoveryFiles.OldFiles(Path.Combine(root, "Discoveries.json")).SequenceEqual(["Discoveries.json"]), "named as left aside");
            Check(File.ReadAllBytes(Path.Combine(root, "Discoveries.json")).SequenceEqual(bytes) && Aside(root).Length == 0, "the file left where it is, unchanged");
            Check(restarted.Count().Mine == 1, "one fact of yours");
        })),
    ];
}
