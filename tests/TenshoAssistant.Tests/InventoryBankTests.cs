using System.Text.Json.Nodes;
using TenshoAssistant.Inventory;

internal static class InventoryBankTests
{
    private static readonly InventoryItem Nq = new(new(42), false);
    private static readonly InventoryItem Hq = new(new(42), true);
    private static readonly DateTimeOffset Time = new(2026, 10, 27, 15, 0, 0, TimeSpan.FromHours(1));

    public static (string Name, Action Run)[] Cases =>
    [
        ("Bank scopes contain only explicit storage pages", () =>
        {
            Check(BankStorageScope.ContainerIds(BankStorageKind.Retainer).SequenceEqual(Enumerable.Range(10000, 7)), "retainer pages");
            Check(BankStorageScope.ContainerIds(BankStorageKind.Saddlebag).SequenceEqual(new[] { 4000, 4001 }), "saddlebag pages");
            Check(BankStorageScope.ContainerIds(BankStorageKind.PremiumSaddlebag).SequenceEqual(new[] { 4100, 4101 }), "premium pages");
            Throws<ArgumentOutOfRangeException>(() => BankStorageScope.ContainerIds((BankStorageKind)99));
            Throws<NotSupportedException>(() => ((IList<int>)BankStorageScope.ContainerIds(BankStorageKind.Retainer))[0] = 9);
        }),
        ("Bank retains distinct retainers and characters with HQ isolation", () =>
        {
            using var bank = Ready(new MemoryStore());
            var first = Entry(1, BankStorageKind.Retainer, 100, 7);
            Import(bank, first);
            Import(bank, Entry(1, BankStorageKind.Retainer, 101, 20));
            Import(bank, Entry(2, BankStorageKind.Retainer, 100, 30));
            Import(bank, Entry(1, BankStorageKind.Saddlebag, 0, 40));
            Import(bank, Entry(2, BankStorageKind.Saddlebag, 0, 50));
            Import(bank, Entry(1, BankStorageKind.PremiumSaddlebag, 0, 60));
            Check(bank.State.Entries.Count == 6, "all keys segregated");
            Check(Quantity(bank.State.Entries.Single(e => e.Key == first.Key), Nq) == 7, "retainer isolated");
            Check(Quantity(first, Hq) == 2 && Quantity(first, Nq) == 7, "quality isolated");
        }),
        ("Bank overwrite replaces a key and complete empty clears old quantities", () =>
        {
            using var bank = Ready(new MemoryStore());
            Import(bank, Entry(1, BankStorageKind.Retainer, 100, 7));
            var before = bank.State;
            Import(bank, Entry(1, BankStorageKind.Retainer, 100, 3));
            Check(bank.State.Entries.Count == 1 && Quantity(bank.State.Entries[0], Nq) == 3, "replace not append");
            Import(bank, Entry(1, BankStorageKind.Retainer, 100, 0));
            Check(Quantity(bank.State.Entries[0], Nq) == 0, "complete empty clears");
            Check(Quantity(before.Entries[0], Nq) == 7, "old state immutable");
            Check(bank.State.Entries is not BankEntry[], "array not exposed");
            Throws<NotSupportedException>(() => ((IList<BankEntry>)bank.State.Entries).Clear());
            var array = new[] { Entry(1, BankStorageKind.Saddlebag, 0, 3) };
            var copied = new BankState(array, false, false, null);
            array[0] = Entry(2, BankStorageKind.Saddlebag, 0, 4);
            Check(copied.Entries[0].Key.CharacterId == 1, "state copies caller array");
        }),
        ("Bank rejects partial unknown missing extra and wrong-owner groups", () =>
        {
            using var bank = Ready(new MemoryStore());
            var valid = Entry(1, BankStorageKind.Retainer, 100, 7);
            Import(bank, valid);
            foreach (var coverage in new[] { InventoryCoverage.Partial, InventoryCoverage.Unknown })
            {
                var invalid = valid with { Snapshot = new(Time, "test", valid.Snapshot.Containers.Values.Select(c =>
                    new InventoryContainerSnapshot(c.Location, coverage, []))) };
                Check(!Await(bank.ImportAsync(invalid)).Success, "incomplete rejected");
            }
            var missing = valid with { Snapshot = new(Time, "test", valid.Snapshot.Containers.Values.Skip(1)) };
            var extra = valid with { Snapshot = new(Time, "test", valid.Snapshot.Containers.Values.Append(
                new(new(new(100), new(10007)), InventoryCoverage.Complete, []))) };
            var wrong = valid with { Snapshot = new(Time, "test", valid.Snapshot.Containers.Values.Select(c =>
                new InventoryContainerSnapshot(new(new(999), c.Location.Container), InventoryCoverage.Complete, []))) };
            var impossible = valid with { Snapshot = new(Time, "test", valid.Snapshot.Containers.Values.Select(c =>
                new InventoryContainerSnapshot(c.Location, InventoryCoverage.Complete, [new(Nq, long.MaxValue)]))) };
            foreach (var invalid in new[] { missing, extra, wrong, impossible, valid with { Key = new(1, BankStorageKind.Retainer, 0) },
                Entry(1, BankStorageKind.Saddlebag, 0, 1) with { Key = new(1, BankStorageKind.Saddlebag, 5) },
                valid with { Key = new(0, BankStorageKind.Retainer, 100) } })
                Check(!Await(bank.ImportAsync(invalid)).Success, "invalid rejected");
            Check(bank.State.Entries.Count == 1 && Quantity(bank.State.Entries[0], Nq) == 7, "cache preserved");
        }),
        ("Bank JSON roundtrip preserves UTC metadata and aggregated quality counts", () => WithDirectory(directory =>
        {
            var path = Path.Combine(directory, "nested", "bank.json");
            var store = new FileInventoryBankStore(path);
            Check(store.Load().Length == 0, "missing file empty");
            var entry = Entry(1, BankStorageKind.Retainer, 100, 7);
            store.Save([entry, Entry(2, BankStorageKind.Saddlebag, 0, 0)]);
            var loaded = new FileInventoryBankStore(path).Load();
            Check(loaded.Length == 2 && loaded[0].Key == entry.Key && loaded[0].Name == entry.Name, "identity");
            Check(Quantity(loaded[0], Nq) == 7 && Quantity(loaded[0], Hq) == 2, "counts");
            Check(loaded[0].Snapshot.ObservedAt == Time && loaded[0].Snapshot.ObservedAt.Offset == TimeSpan.Zero
                && loaded[0].Snapshot.Source == "bank-test", "UTC/source");
            Check(Quantity(loaded[1], Nq) == 0, "empty roundtrip");
            var json = JsonNode.Parse(File.ReadAllText(path))!;
            Check(json["schemaVersion"]!.GetValue<int>() == 1, "version");
            Check(json["entries"]![0]!["containers"]![0]!["items"]!.AsArray().Count == 2, "aggregated NQ/HQ");
        })),
        ("Bank corrupt and unsupported files fail closed without overwrite", () => WithDirectory(directory =>
        {
            var path = Path.Combine(directory, "bank.json");
            foreach (var text in new[] { "{broken", "{\"schemaVersion\":99,\"savedAtUtc\":\"2026-10-27T00:00:00Z\",\"entries\":[]}" })
            {
                File.WriteAllText(path, text);
                var store = new FileInventoryBankStore(path);
                Throws<System.IO.InvalidDataException>(() => store.Load());
                Throws<InvalidOperationException>(() => store.Save([]));
                Check(File.ReadAllText(path) == text, "failed load preserves bytes");
                Throws<System.IO.InvalidDataException>(() => new FileInventoryBankStore(path).Save([]));
                Check(File.ReadAllText(path) == text, "save without load protects bytes");
                using var bank = Ready(new FileInventoryBankStore(path));
                Check(bank.State.Error is not null && !Await(bank.ImportAsync(Entry(1, BankStorageKind.Saddlebag, 0, 1))).Success,
                    "failed service load blocks imports");
            }
        })),
        ("Bank JSON rejects duplicates invalid counts missing fields and size overflow", () => WithDirectory(directory =>
        {
            var path = Path.Combine(directory, "bank.json");
            new FileInventoryBankStore(path).Save([Entry(1, BankStorageKind.Saddlebag, 0, 3)]);
            var baseline = File.ReadAllText(path);
            Action<Action<JsonNode>> reject = mutation =>
            {
                var json = JsonNode.Parse(baseline)!;
                mutation(json);
                var text = json.ToJsonString();
                File.WriteAllText(path, text);
                Throws<InvalidDataException>(() => new FileInventoryBankStore(path).Load());
                Check(File.ReadAllText(path) == text, "invalid file unchanged");
            };
            reject(j => j["entries"]!.AsArray().Add(j["entries"]![0]!.DeepClone()));
            reject(j => j["entries"]![0]!["containers"]!.AsArray().Add(j["entries"]![0]!["containers"]![0]!.DeepClone()));
            reject(j => j["entries"]![0]!["containers"]![0]!["items"]!.AsArray().Add(
                j["entries"]![0]!["containers"]![0]!["items"]![0]!.DeepClone()));
            reject(j => j["entries"]![0]!["containers"]![0]!["items"]![0]!["quantity"] = 0);
            reject(j => j["entries"]![0]!["containers"]![0]!["items"]![0]!["quantity"] = -1);
            reject(j => j["entries"]![0]!["containers"]![0]!["items"]![0]!["quantity"] = long.MaxValue);
            reject(j => j["entries"]![0]!["containers"]![0]!["items"]![0]!["itemId"] = 0);
            reject(j => j["entries"]![0]!["observedAtUtc"] = "2026-10-27T01:00:00+01:00");
            reject(j => j["entries"]![0]!.AsObject().Remove("source"));
            File.WriteAllText(path, baseline.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1"));
            Throws<InvalidDataException>(() => new FileInventoryBankStore(path).Load());
            using (var file = new FileStream(path, FileMode.Create)) file.SetLength(16 * 1024 * 1024 + 1);
            Throws<InvalidDataException>(() => new FileInventoryBankStore(path).Load());
        })),
        ("Bank atomic saves retain recovery backup and reject corrupt primary", () => WithDirectory(directory =>
        {
            var path = Path.Combine(directory, "bank.json");
            var store = new FileInventoryBankStore(path);
            store.Save([Entry(1, BankStorageKind.Saddlebag, 0, 3)]);
            store.Save([Entry(1, BankStorageKind.Saddlebag, 0, 7)]);
            Check(Quantity(store.Load()[0], Nq) == 7 && File.Exists(path + ".bak"), "replacement committed");
            File.Delete(path);
            Check(Quantity(store.Load()[0], Nq) == 3, "missing primary recovers prior version");
            store.Save([Entry(1, BankStorageKind.Saddlebag, 0, 9)]);
            Check(Quantity(store.Load()[0], Nq) == 9, "save after recovery");
            File.WriteAllText(path, "corrupt");
            Throws<InvalidDataException>(() => store.Load());
            Check(!Directory.EnumerateFiles(directory, "*.tmp").Any(), "no temporary debris");
        })),
        ("Bank save IO failure preserves published entries and permits retry", () =>
        {
            var store = new MemoryStore();
            using var bank = Ready(store);
            Import(bank, Entry(1, BankStorageKind.Saddlebag, 0, 3));
            var before = bank.State;
            store.FailSave = true;
            var failed = Await(bank.ImportAsync(Entry(1, BankStorageKind.Saddlebag, 0, 7)));
            Check(!failed.Success && failed.Message.Contains("Injected IO failure", StringComparison.Ordinal), "save failure in result");
            Check(Quantity(bank.State.Entries[0], Nq) == 3 && bank.State.Error is null && !bank.State.Importing, "rollback allows retry");
            Check(Quantity(before.Entries[0], Nq) == 3, "prior state intact");
            store.FailSave = false;
            Import(bank, Entry(1, BankStorageKind.Saddlebag, 0, 9));
            Check(Quantity(bank.State.Entries[0], Nq) == 9 && bank.State.Error is null, "retry");
        }),
        ("Bank real filesystem save failure preserves disk and cache", () => WithDirectory(directory =>
        {
            var path = Path.Combine(directory, "bank.json");
            var store = new FileInventoryBankStore(path);
            store.Save([Entry(1, BankStorageKind.Saddlebag, 0, 3)]);
            using var bank = Ready(store);
            var original = File.ReadAllText(path);
            // The backup target being a directory forces atomic replacement to fail on supported platforms.
            Directory.CreateDirectory(path + ".bak");
            Check(!Await(bank.ImportAsync(Entry(1, BankStorageKind.Saddlebag, 0, 9))).Success, "filesystem failure");
            Check(File.ReadAllText(path) == original && Quantity(bank.State.Entries[0], Nq) == 3, "disk/cache unchanged");
            Check(!Directory.EnumerateFiles(directory, "*.tmp").Any(), "temporary cleaned");
        })),
        ("Bank rejects loading imports and dispose completes tasks without late publication", () =>
        {
            using var store = new BlockingStore(blockLoad: true);
            var bank = new InventoryBankService(store);
            try
            {
                Signal(store.Entered);
                Check(bank.State.Loading && !Await(bank.ImportAsync(Entry(1, BankStorageKind.Saddlebag, 0, 3))).Success,
                    "loading rejected");
                bank.Dispose();
                Await(bank.Ready);
                Await(bank.Completion);
                var disposedState = bank.State;
                store.Release.Set();
                Signal(store.Exited);
                Check(ReferenceEquals(disposedState, bank.State) && bank.State.Entries.Count == 0, "no late load");
                Check(!Await(bank.ImportAsync(Entry(1, BankStorageKind.Saddlebag, 0, 3))).Success, "disposed rejected");
            }
            finally { bank.Dispose(); store.Release.Set(); }
        }),
        ("Bank permits one pending import and disposal cancels it without late publication", () =>
        {
            using var store = new BlockingStore(blockLoad: false);
            var bank = Ready(store);
            try
            {
                var pending = bank.ImportAsync(Entry(1, BankStorageKind.Saddlebag, 0, 3));
                Signal(store.Entered);
                Check(bank.State.Importing && bank.State.Entries.Count == 0 && !pending.IsCompleted, "durability precedes publication");
                Check(!Await(bank.ImportAsync(Entry(2, BankStorageKind.Saddlebag, 0, 5))).Success, "second pending rejected");
                bank.Dispose();
                Check(!Await(pending).Success, "pending cancelled");
                Await(bank.Completion);
                var disposedState = bank.State;
                store.Release.Set();
                Signal(store.Exited);
                Check(ReferenceEquals(bank.State, disposedState) && bank.State.Entries.Count == 0, "no late save publication");
            }
            finally { bank.Dispose(); store.Release.Set(); }
        }),
    ];

    private static BankEntry Entry(ulong character, BankStorageKind kind, ulong storage, long quantity)
    {
        var owner = kind == BankStorageKind.Retainer ? storage : character;
        var containers = BankStorageScope.ContainerIds(kind).Select((id, index) => new InventoryContainerSnapshot(
            new(new(owner), new(id)), InventoryCoverage.Complete,
            index == 0 && quantity > 0 ? new InventoryStack[] { new(Nq, quantity), new(Hq, 1), new(Hq, 1) } : []));
        return new(new(character, kind, storage), $"Storage {storage}", new(Time, "bank-test", containers));
    }

    private static long Quantity(BankEntry entry, InventoryItem item) =>
        entry.Snapshot.GetQuantity(item, entry.Snapshot.Containers.Keys).ExactQuantity!.Value;
    private static InventoryBankService Ready(IInventoryBankStore store)
    {
        var bank = new InventoryBankService(store);
        Await(bank.Ready);
        return bank;
    }
    private static void Import(InventoryBankService bank, BankEntry entry) => Check(Await(bank.ImportAsync(entry)).Success, "import succeeded");
    private static T Await<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    private static void Await(Task task) => task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    private static void Signal(ManualResetEventSlim signal) => Check(signal.Wait(TimeSpan.FromSeconds(5)), "worker reached checkpoint");
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}");
    }

    private static void WithDirectory(Action<string> run)
    {
        var root = Path.GetFullPath(Path.GetTempPath());
        var target = Path.GetFullPath(Path.Combine(root, "TenshoInventoryBankTests-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(target);
        try { run(target); }
        finally
        {
            if (!string.Equals(Path.GetDirectoryName(target), Path.TrimEndingDirectorySeparator(root), StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(target).StartsWith("TenshoInventoryBankTests-", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing cleanup outside exact test directory.");
            Directory.Delete(target, recursive: true);
        }
    }

    private sealed class MemoryStore : IInventoryBankStore
    {
        private BankEntry[] entries = [];
        public bool FailSave;
        public BankEntry[] Load() => entries.ToArray();
        public void Save(IReadOnlyList<BankEntry> next)
        {
            if (FailSave) throw new IOException("Injected IO failure.");
            entries = next.ToArray();
        }
    }

    private sealed class BlockingStore(bool blockLoad) : IInventoryBankStore, IDisposable
    {
        public ManualResetEventSlim Entered { get; } = new();
        public ManualResetEventSlim Release { get; } = new();
        public ManualResetEventSlim Exited { get; } = new();
        public BankEntry[] Load()
        {
            if (blockLoad) Block();
            return [];
        }
        public void Save(IReadOnlyList<BankEntry> entries) => Block();
        private void Block()
        {
            Entered.Set();
            Release.Wait(TimeSpan.FromSeconds(10));
            Exited.Set();
        }
        public void Dispose()
        {
            Release.Set();
            // Wait for the store callback before disposing synchronization objects it uses.
            if (Entered.IsSet) Signal(Exited);
            Entered.Dispose(); Release.Dispose(); Exited.Dispose();
        }
    }
}
