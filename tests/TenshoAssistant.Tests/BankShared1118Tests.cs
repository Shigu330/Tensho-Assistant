using TenshoAssistant.Inventory;

// 0.1118 (X-141, audit A02): two game clients on the same profile share InventoryBank/. Every change writes only the character concerned,
// read again under the profile's lock; a character is erased only by « Erase this character ». Behaviour tests (R28): two stores and two
// services on one real folder, at the same time; the lock held by the other side; the former file split by two clients at once; .bak.
internal static class BankShared1118Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static BankEntry Entry(ulong character,BankStorageKind kind,ulong storage = 0)
    {
        var owner = new InventoryOwnerId(kind == BankStorageKind.Retainer ? storage : character);
        var containers = BankStorageScope.ContainerIds(kind).Select(id => new InventoryContainerSnapshot(new(owner,new(id)),InventoryCoverage.Complete,
            id == BankStorageScope.ContainerIds(kind)[0] ? [new InventoryStack(new(new(5057),false),3,0)] : [])).ToArray();
        return new(new(character,kind,storage),kind == BankStorageKind.Retainer ? "Servant" : "Name (World)",new(DateTimeOffset.UtcNow,"test",containers));
    }

    private static T Await<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
    private static InventoryBankService Ready(PerCharacterBankStore store)
    {
        var bank = new InventoryBankService(store);
        bank.Ready.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        Check(bank.State.Error is null,"bank loaded: " + bank.State.Error);
        return bank;
    }
    private static void Import(InventoryBankService bank,BankEntry entry)
    {
        var result = Await(bank.ImportAsync(entry));
        Check(result.Success,"import saved: " + result.Message);
    }

    private static void InTemp(Action<string,string> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-bank-1118-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(Path.Combine(root,"InventoryBank"),Path.Combine(root,"InventoryBank.json")); }
        finally { try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static ILookup<ulong,BankEntry> OnDisk(string folder,string legacy) => new PerCharacterBankStore(folder,legacy).Load().ToLookup(entry => entry.Key.CharacterId);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Bank 1118: two clients on the same profile each keep their characters (the old code erased the other's file at the next capture)",() => InTemp((folder,legacy) =>
        {
            using var first = Ready(new PerCharacterBankStore(folder,legacy));
            using var second = Ready(new PerCharacterBankStore(folder,legacy));
            Import(first,Entry(1,BankStorageKind.Saddlebag));
            Import(second,Entry(2,BankStorageKind.Bags));
            Import(first,Entry(1,BankStorageKind.Retainer,99));
            Import(second,Entry(2,BankStorageKind.Retainer,77));
            var disk = OnDisk(folder,legacy);
            Check(disk[1].Count() == 2 && disk[2].Count() == 2,$"both characters kept on disk: {disk[1].Count()} and {disk[2].Count()}");
            // The same character captured by both clients: the second one reads the file again and keeps the first one's storage.
            Import(first,Entry(3,BankStorageKind.Saddlebag));
            Import(second,Entry(3,BankStorageKind.Retainer,7));
            Check(OnDisk(folder,legacy)[3].Count() == 2,"one character from two clients: both storages kept");
            Check(second.State.Entries.Count(entry => entry.Key.CharacterId == 3) == 2,"the client shows the character as saved, the other's storage included");
        })),
        ("Bank 1118: two clients capturing at the same time lose nothing (read-modify-write under the profile's lock)",() => InTemp((folder,legacy) =>
        {
            // 0.1201 (failed once, on a busy machine: « import saved: Bank save failed »): the lock file is asked for every 50 ms, without
            // a queue, so a client saving 25 times in a row can keep the other one waiting past its 10 s; that bound has its own tests
            // (AuditFixes1142Tests, BankSqlite1149Tests). Here the wait is longer than the test can last: what is proved is that two
            // clients writing at once lose nothing, whatever the machine's speed.
            using var first = Ready(new PerCharacterBankStore(folder,legacy,lockWaitMs: 110_000));
            using var second = Ready(new PerCharacterBankStore(folder,legacy,lockWaitMs: 110_000));
            var runs = new[] { (Bank: first,Base: 1000UL),(Bank: second,Base: 2000UL) }.Select(side => Task.Run(() =>
            {
                for (var i = 0UL; i < 25; i++) Import(side.Bank,Entry(4,BankStorageKind.Retainer,side.Base + i));
            })).ToArray();
            Task.WaitAll(runs,TimeSpan.FromSeconds(120));
            Check(runs.All(run => run.IsCompletedSuccessfully),"both clients finished");
            Check(OnDisk(folder,legacy)[4].Count() == 50,"50 storages from two clients at once, none lost: " + OnDisk(folder,legacy)[4].Count());
            Check(!Directory.EnumerateFiles(folder,"*.tmp").Any(),"no temporary file left");
        })),
        ("Bank 1118: a character is erased only by its explicit command; the other client's next capture does not bring it back nor erase others",() => InTemp((folder,legacy) =>
        {
            using var first = Ready(new PerCharacterBankStore(folder,legacy));
            Import(first,Entry(1,BankStorageKind.Saddlebag));
            Import(first,Entry(2,BankStorageKind.Saddlebag));
            using var second = Ready(new PerCharacterBankStore(folder,legacy));
            Check(second.State.Entries.Count == 2,"the second client sees both");
            Check(Await(first.EraseCharacterAsync(2)).Success && !File.Exists(Path.Combine(folder,"2.json")) && !File.Exists(Path.Combine(folder,"2.json.bak")),"erased on command: its file and copy");
            Import(second,Entry(1,BankStorageKind.Retainer,5));
            Check(!File.Exists(Path.Combine(folder,"2.json")) && OnDisk(folder,legacy)[1].Count() == 2,"the other client's capture writes only its character");
            Check(Await(second.ForgetAsync(new BankKey(1,BankStorageKind.Retainer,5))).Success && OnDisk(folder,legacy)[1].Count() == 1,"forgetting one storage writes only that character");
            var store = new PerCharacterBankStore(folder,legacy);
            store.Save(store.Load().Where(entry => false).ToArray());
            Check(File.Exists(Path.Combine(folder,"1.json")),"a whole-list save never deletes a character");
        })),
        ("Bank 1118: the other client holding the lock: the change fails, nothing written, the bank shown unchanged",() => InTemp((folder,legacy) =>
        {
            var store = new PerCharacterBankStore(folder,legacy,lockWaitMs: 200);
            using var bank = Ready(store);
            Import(bank,Entry(1,BankStorageKind.Saddlebag));
            var before = File.ReadAllBytes(Path.Combine(folder,"1.json"));
            using (new FileStream(store.LockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            {
                var result = Await(bank.ImportAsync(Entry(1,BankStorageKind.Retainer,3)));
                Check(!result.Success && result.Message.StartsWith("Bank save failed",StringComparison.Ordinal),"refused while held: " + result.Message);
            }
            Check(File.ReadAllBytes(Path.Combine(folder,"1.json")).SequenceEqual(before) && bank.State.Entries.Count == 1,"file and shown bank unchanged");
            Import(bank,Entry(1,BankStorageKind.Retainer,3));
            Check(OnDisk(folder,legacy)[1].Count() == 2,"saved once the lock is free");
        })),
        ("Bank 1118: the former shared file split by two clients starting at once, once; a restart from the .bak; a corrupt file still fails closed",() => InTemp((folder,legacy) =>
        {
            new FileInventoryBankStore(legacy).Save([Entry(5,BankStorageKind.Saddlebag),Entry(6,BankStorageKind.Retainer,9),Entry(6,BankStorageKind.Bags)]);
            var loads = Enumerable.Range(0,2).Select(_ => Task.Run(() => new PerCharacterBankStore(folder,legacy).Load())).ToArray();
            Task.WaitAll(loads,TimeSpan.FromSeconds(60));
            Check(loads.All(load => load.Result.Length == 3),"both clients read the three storages");
            Check(File.Exists(legacy + ".migrated") && !File.Exists(legacy + ".migrated.1") && !File.Exists(legacy) && !Directory.Exists(folder + ".migrating"),"split once, the former file kept beside");
            // Restart after a lost main file: the .bak copy is read, then written back as the main file with the new storage.
            using (var bank = Ready(new PerCharacterBankStore(folder,legacy))) Import(bank,Entry(5,BankStorageKind.Retainer,1));
            File.Delete(Path.Combine(folder,"5.json"));
            Check(File.Exists(Path.Combine(folder,"5.json.bak")),"the copy is there");
            using (var bank = Ready(new PerCharacterBankStore(folder,legacy)))
            {
                Check(bank.State.Entries.Count(entry => entry.Key.CharacterId == 5) == 1,"restart reads the .bak");
                Import(bank,Entry(5,BankStorageKind.Retainer,2));
            }
            Check(File.Exists(Path.Combine(folder,"5.json")) && OnDisk(folder,legacy)[5].Count() == 2,"written back from the copy, nothing lost");
            File.WriteAllText(Path.Combine(folder,"6.json"),"{ broken");
            var corrupt = new PerCharacterBankStore(folder,legacy);
            var failed = false;
            try { corrupt.Load(); } catch (Exception) { failed = true; }
            var blocked = false;
            try { corrupt.Apply(new BankChange(Put: Entry(5,BankStorageKind.Bags)),[]); } catch (InvalidOperationException) { blocked = true; }
            Check(failed && blocked && File.ReadAllText(Path.Combine(folder,"6.json")) == "{ broken","a corrupt file fails closed and blocks every write");
        })),
    ];
}
