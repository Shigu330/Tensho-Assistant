using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;
using TenshoAssistant.Inventory;

// 0.1173 (X-182 continued, night review N3 for the bank and the discoveries, N26): two game clients, or two versions of the plugin the same day.
// Behaviour tests (R28: data loss, concurrency), on real files in a temporary folder: a bank character file written again after the conversion (an
// older version still running) is merged storage by storage, the newest reading of each kept and the others staying; an older reading never takes
// back a newer one; a Discoveries.json written again is merged without adding counts up (a whole base saved again is not doubled), a copy already
// set aside is not read again; « unchanged, not written » is decided against the database: a storage the other client forgot is written again.
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

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Concurrent data 1173: a bank file written again after the conversion is merged storage by storage, the others kept (N3)", () => InTemp(root =>
        {
            var folder = Path.Combine(root, "InventoryBank");
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, PerCharacterBankStore.FileName(7));
            new FileInventoryBankStore(file).Save([Retainer(1, At, quantity: 3), Retainer(2, At, quantity: 4)]);
            Check(Bank(root).Load().Length == 2, "converted: two storages");
            Check(!Directory.Exists(folder) || !Directory.EnumerateFileSystemEntries(folder).Any(), "the folder of before set aside");
            // The older version finds no folder, reads servant 1 again and saves only what it saw.
            Directory.CreateDirectory(folder);
            new FileInventoryBankStore(file).Save([Retainer(1, At.AddHours(1), quantity: 9)]);
            Later(file);
            var loaded = Bank(root).Load().OrderBy(entry => entry.Key.StorageId).ToArray();
            Check(loaded.Length == 2, "servant 2 still there: " + loaded.Length);
            Check(loaded[0].Snapshot.ObservedAt == At.AddHours(1) && Quantity(loaded[0]) == 9,
                "servant 1: the newer reading of the file");
            Check(loaded[1].Snapshot.ObservedAt == At, "servant 2 unchanged");
            Check(Aside(root).Count(path => path.EndsWith(PerCharacterBankStore.FileName(7), StringComparison.Ordinal)) == 2, "both folders of before kept aside, never removed: " + Aside(root).Length);
        })),
        ("Concurrent data 1173: an older reading in a bank file written again never takes back a newer one; a damaged file is only set aside (N3)", () => InTemp(root =>
        {
            var folder = Path.Combine(root, "InventoryBank");
            var store = Bank(root);
            store.Load();
            store.Apply(new BankChange(Put: Retainer(1, At.AddHours(2), quantity: 5)), []);
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, PerCharacterBankStore.FileName(7));
            new FileInventoryBankStore(file).Save([Retainer(1, At, quantity: 1), Retainer(3, At, quantity: 2)]);
            Later(file);
            var loaded = Bank(root).Load().OrderBy(entry => entry.Key.StorageId).ToArray();
            Check(loaded.Length == 2 && loaded[0].Snapshot.ObservedAt == At.AddHours(2) && Quantity(loaded[0]) == 5, "servant 1 keeps the newer reading; servant 3 added (rows saved before any mark are joined, not replaced)");
            Directory.CreateDirectory(folder);
            File.WriteAllText(file, "{ not a bank");
            Later(file);
            Check(Bank(root).Load().Length == 2, "a damaged file: nothing merged, the bank still loads");
            Check(Aside(root).Count(path => path.EndsWith(PerCharacterBankStore.FileName(7), StringComparison.Ordinal)) == 2, "the damaged file kept aside");
            var merged = PerCharacterBankStore.MergeNewest([Retainer(1, At), Retainer(2, At)], [Retainer(2, At.AddMinutes(1), quantity: 8), Retainer(4, At)]);
            Check(merged.Select(entry => entry.Key.StorageId).SequenceEqual([1UL, 2UL, 4UL]) && merged[1].Snapshot.ObservedAt == At.AddMinutes(1), "MergeNewest: newest kept, all storages");
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
        ("Concurrent data 1173: a Discoveries.json written again after the conversion is merged, never added up; a copy set aside is not read again (N3)", () => InTemp(root =>
        {
            WriteBase(root, Fact(4869, 2, At));
            Check(DiscoveryDatabase.ConvertFiles(root, At, (_, error) => throw error) is not null, "converted");
            var told = new List<int>();
            // The older version had read the whole base before the conversion: it saves it again, with one more sighting and a new cell.
            WriteBase(root, Fact(4869, 3, At.AddHours(1)), Fact(4870, 1, At.AddHours(1)));
            Later(Path.Combine(root, "Discoveries.json"));
            DiscoveryDatabase.ConvertFiles(root, At.AddSeconds(1), (_, error) => throw error, told.Add);
            Check(told.SequenceEqual([2]), "two facts merged: " + string.Join(",", told));
            Check(Mine(root, 4869).Seen == 3, "the cell seen by both: 3 sightings, not 5: " + Mine(root, 4869).Seen);
            Check(Mine(root, 4870).Seen == 1, "the new cell added");
            Check(!File.Exists(Path.Combine(root, "Discoveries.json")) && Aside(root).Length == 2, "set aside again, kept");
            // The same base saved again with fewer sightings: the database keeps its own.
            WriteBase(root, Fact(4869, 1, At));
            Later(Path.Combine(root, "Discoveries.json"));
            told.Clear();
            DiscoveryDatabase.ConvertFiles(root, At.AddSeconds(2), (_, error) => throw error, told.Add);
            Check(told.SequenceEqual([0]) && Mine(root, 4869).Seen == 3, "an older copy never takes back the database's count");
            // A copy already set aside put back: not read again.
            var kept = Aside(root).First(path => path.EndsWith("Discoveries.json", StringComparison.Ordinal));
            File.Copy(kept, Path.Combine(root, "Discoveries.json"));
            Later(Path.Combine(root, "Discoveries.json"));
            told.Clear();
            DiscoveryDatabase.ConvertFiles(root, At.AddSeconds(3), (_, error) => throw error, told.Add);
            Check(told.Count == 0, "a copy already set aside is not merged");
            Check(new DiscoveryDatabase(Path.Combine(root, DiscoveryDatabase.FileName)).Count().Mine == 2, "two facts of yours");
        })),
    ];
}
