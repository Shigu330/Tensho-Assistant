using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Inventory;

// 0.1149 (X-173, user's choice of 12/11): the bank and inventories in the player's data file, TenshoData.db, still under the profile's lock (X-141)
// and one character at a time. Behaviour tests (R28): real files in a temporary folder; the folder of before converted whole in one transaction or
// not at all (a stop inside, an unreadable character file); the former shared file split then converted; a data file that blocks (newer layout)
// or cannot be created (files as before); two clients at once on one data file; the lock held by the other client; SQLite kept busy by another
// writer (tried again, never a blocked bank); a tampered row fails closed; the desktop application's copy reads it, never converting.
internal static class BankSqlite1149Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static BankEntry Entry(ulong character,BankStorageKind kind,ulong storage = 0,uint item = 5057)
    {
        var owner = new InventoryOwnerId(kind == BankStorageKind.Retainer ? storage : character);
        var containers = BankStorageScope.ContainerIds(kind).Select(id => new InventoryContainerSnapshot(new(owner,new(id)),InventoryCoverage.Complete,
            id == BankStorageScope.ContainerIds(kind)[0] ? [new InventoryStack(new(new(item),false),3,0)] : [])).ToArray();
        return new(new(character,kind,storage),kind == BankStorageKind.Retainer ? "Servant « 1 »" : "Name (World)",new(DateTimeOffset.UtcNow,"test",containers));
    }

    private static T Await<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
    private static InventoryBankService Ready(PerCharacterBankStore store)
    {
        var bank = new InventoryBankService(store,TimeSpan.FromMilliseconds(100));
        bank.Ready.WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
        Check(bank.State.Error is null,"bank loaded: " + bank.State.Error);
        return bank;
    }
    private static void Import(InventoryBankService bank,BankEntry entry)
    {
        var result = Await(bank.ImportAsync(entry));
        Check(result.Success,"import saved: " + result.Message);
    }

    private static void InTemp(Action<string,string,string,string> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-bank-1149-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root,Path.Combine(root,"InventoryBank"),Path.Combine(root,"InventoryBank.json"),Path.Combine(root,TenshoDataDatabase.FileName)); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static TenshoDataFile Data(string database,List<string>? warnings = null) => new(database,(error,message) => warnings?.Add(message + " " + error.Message));
    private static PerCharacterBankStore Store(string folder,string legacy,string database,int lockWaitMs = 10_000) => new(folder,legacy,lockWaitMs,Data(database));
    // Entries compared by what is saved (the file's JSON of each entry), in order.
    private static string Text(IEnumerable<BankEntry> entries) => string.Join("\n",FileInventoryBankStore.EntryRows(entries.ToArray()));
    private static string[] Aside(string root) => Directory.GetDirectories(root,"before-sqlite-*").SelectMany(directory => Directory.GetFiles(directory,"*",SearchOption.AllDirectories)).ToArray();
    private static long Rows(string database)
    {
        using var open = new TenshoDataDatabase(database).Open();
        return Convert.ToInt64(TenshoDataDatabase.Scalar(open,"SELECT COUNT(*) FROM bank_entries;"));
    }
    private static bool Marked(string database)
    {
        using var open = new TenshoDataDatabase(database).Open();
        return TenshoDataDatabase.Converted(open,PerCharacterBankStore.Family);
    }
    private static bool Throws<T>(Action run) where T : Exception
    {
        try { run(); return false; } catch (T) { return true; }
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Bank SQLite 1149: the folder of before converted whole (a character kept only in its .bak too), kept aside byte for byte, the same after a restart",() => InTemp((root,folder,legacy,database) =>
        {
            var one = new[] { Entry(1,BankStorageKind.Saddlebag),Entry(1,BankStorageKind.Retainer,99,item: 2),Entry(1,BankStorageKind.Bags) };
            var two = new[] { Entry(0x8000_0000_0000_0002UL,BankStorageKind.Retainer,0x8000_0000_0000_0007UL),Entry(0x8000_0000_0000_0002UL,BankStorageKind.Currency) };
            var three = new[] { Entry(3,BankStorageKind.Armoury) };
            var files = new PerCharacterBankStore(folder,legacy);
            files.Save([.. one,.. two,.. three]);
            files.Save([.. three.Select(entry => entry with { Name = "Saved twice" })]);
            File.Delete(Path.Combine(folder,"3.json"));
            Check(File.Exists(Path.Combine(folder,"3.json.bak")),"character 3 kept only in its .bak");
            var expected = new PerCharacterBankStore(folder,legacy).Load();
            var bytes = Directory.GetFiles(folder).OrderBy(path => Path.GetFileName(path)).Select(path => (Path.GetFileName(path),File.ReadAllBytes(path))).ToArray();
            var store = Store(folder,legacy,database);
            Check(store.Mode == "sqlite","SQLite in use: " + store.Mode);
            Check(Text(store.Load()) == Text(expected),"the same entries, in the same order, after the conversion");
            Check(!Directory.Exists(folder) && Marked(database) && Rows(database) == 6,"folder moved, marked converted, six rows");
            var kept = Directory.GetDirectories(root,"before-sqlite-*").Single();
            Check(Directory.GetFiles(Path.Combine(kept,"InventoryBank")).OrderBy(path => Path.GetFileName(path)).Select(path => (Path.GetFileName(path),File.ReadAllBytes(path)))
                .Zip(bytes).All(pair => pair.First.Item1 == pair.Second.Item1 && pair.First.Item2.SequenceEqual(pair.Second.Item2)) && Aside(root).Length == bytes.Length,"every file of before kept aside, unchanged");
            Check(Text(Store(folder,legacy,database).Load()) == Text(expected),"after a restart");
            Check(!Directory.Exists(folder),"no folder made again");
        })),
        ("Bank SQLite 1149: the former shared file split then converted; changes are per character, erase on command only, a whole-list save deletes nothing",() => InTemp((root,folder,legacy,database) =>
        {
            new FileInventoryBankStore(legacy).Save([Entry(5,BankStorageKind.Saddlebag),Entry(6,BankStorageKind.Retainer,9),Entry(6,BankStorageKind.Bags)]);
            var store = Store(folder,legacy,database);
            Check(store.Load().Length == 3 && Rows(database) == 3,"three storages converted from the former file");
            Check(!File.Exists(legacy) && File.Exists(legacy + ".migrated") && !Directory.Exists(folder) && Aside(root).Any(path => path.EndsWith("6.json",StringComparison.Ordinal)),
                "the former file kept as .migrated, the split folder set aside");
            using var bank = Ready(store);
            Import(bank,Entry(5,BankStorageKind.Retainer,1));
            Check(Await(bank.ForgetAsync(new BankKey(6,BankStorageKind.Retainer,9))).Success,"forget one storage");
            Check(Await(bank.EraseCharacterAsync(5)).Success,"erase a character on command");
            var after = Store(folder,legacy,database).Load();
            Check(after.Length == 1 && after[0].Key == new BankKey(6,BankStorageKind.Bags,0),"only character 6's bags left: " + after.Length);
            var whole = Store(folder,legacy,database);
            whole.Save(whole.Load().Where(entry => false).ToArray());
            Check(Rows(database) == 1,"a whole-list save never deletes a character");
            Check(!Directory.Exists(folder),"nothing written to the files of before");
        })),
        ("Bank SQLite 1149: a stop inside the conversion and an unreadable character file leave the files in place, nothing in the data file, every write refused",() => InTemp((root,folder,legacy,database) =>
        {
            new PerCharacterBankStore(folder,legacy).Save([Entry(1,BankStorageKind.Saddlebag),Entry(2,BankStorageKind.Bags)]);
            var stopped = Store(folder,legacy,database);
            stopped.DuringConversion = () => throw new IOException("stop inside the transaction");
            Check(Throws<IOException>(() => stopped.Load()),"the stop is seen");
            Check(File.Exists(Path.Combine(folder,"1.json")) && File.Exists(Path.Combine(folder,"2.json")) && Aside(root).Length == 0,"the files stay");
            Check(Rows(database) == 0 && !Marked(database),"no row, no mark");
            Check(Throws<InvalidOperationException>(() => stopped.Apply(new BankChange(Put: Entry(1,BankStorageKind.Bags)),[])),"a failed load refuses every write");
            var good = File.ReadAllBytes(Path.Combine(folder,"2.json"));
            File.WriteAllText(Path.Combine(folder,"2.json"),"{ broken");
            var broken = Store(folder,legacy,database);
            Check(Throws<InvalidDataException>(() => broken.Load()),"an unreadable character file fails the whole conversion");
            Check(Rows(database) == 0 && !Marked(database) && File.ReadAllText(Path.Combine(folder,"2.json")) == "{ broken" && File.Exists(Path.Combine(folder,"1.json")),"nothing converted, nothing moved");
            Check(Throws<InvalidOperationException>(() => broken.Save([Entry(1,BankStorageKind.Bags)])),"and nothing written");
            File.WriteAllBytes(Path.Combine(folder,"2.json"),good);
            Check(Store(folder,legacy,database).Load().Length == 2 && Marked(database),"converted at the next start once readable");
        })),
        ("Bank SQLite 1149: a data file of a newer layout blocks the bank and is never written; no data file possible: the files as before",() => InTemp((root,folder,legacy,database) =>
        {
            new PerCharacterBankStore(folder,legacy).Save([Entry(1,BankStorageKind.Saddlebag)]);
            using (var open = new TenshoDataDatabase(database).Open()) TenshoDataDatabase.Run(open,"PRAGMA user_version=9;");
            SqliteConnection.ClearAllPools();
            var bytes = File.ReadAllBytes(database);var file = File.ReadAllBytes(Path.Combine(folder,"1.json"));
            var blocked = Store(folder,legacy,database);
            Check(blocked.Mode == "blocked","blocked: " + blocked.Mode);
            Check(Throws<IOException>(() => blocked.Load()) && Throws<InvalidOperationException>(() => blocked.Save([Entry(1,BankStorageKind.Bags)])),"unavailable, nothing written");
            Check(File.ReadAllBytes(database).SequenceEqual(bytes) && File.ReadAllBytes(Path.Combine(folder,"1.json")).SequenceEqual(file) && Aside(root).Length == 0,"the data file and the bank file untouched");
            var nowhere = Path.Combine(root,"missing-folder","sub",TenshoDataDatabase.FileName);
            var json = new PerCharacterBankStore(folder,legacy,data: new TenshoDataFile(nowhere,(_,_) => { }));
            Check(json.Mode == "json" && json.Load().Length == 1,"no data file can be made: the files as before");
            json.Apply(new BankChange(Put: Entry(1,BankStorageKind.Bags)),json.Load());
            Check(new PerCharacterBankStore(folder,legacy).Load().Length == 2,"written to the character's file");
        })),
        ("Bank SQLite 1149: two clients on one data file at the same time lose nothing; the lock held by the other: refused, nothing written",() => InTemp((root,folder,legacy,database) =>
        {
            new PerCharacterBankStore(folder,legacy).Save([Entry(4,BankStorageKind.Saddlebag)]);
            using var first = Ready(Store(folder,legacy,database));
            using var second = Ready(Store(folder,legacy,database));
            var runs = new[] { (Bank: first,Base: 1000UL),(Bank: second,Base: 2000UL) }.Select(side => Task.Run(() =>
            {
                for (var i = 0UL; i < 20; i++) Import(side.Bank,Entry(4,BankStorageKind.Retainer,side.Base + i));
            })).ToArray();
            Task.WaitAll(runs,TimeSpan.FromSeconds(120));
            Check(runs.All(run => run.IsCompletedSuccessfully),"both clients finished");
            Check(Store(folder,legacy,database).Load().Count(entry => entry.Key.CharacterId == 4) == 41,"41 storages from two clients at once, none lost: " + Rows(database));
            var store = Store(folder,legacy,database,lockWaitMs: 200);
            using var bank = Ready(store);
            var before = Rows(database);
            using (new FileStream(store.LockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            {
                var result = Await(bank.ImportAsync(Entry(4,BankStorageKind.Bags)));
                Check(!result.Success,"refused while the lock is held: " + result.Message);
                Check(Throws<BankLockBusyException>(() => Store(folder,legacy,database,lockWaitMs: 200).Load()),"a load waits, then says busy (not a failed read)");
            }
            Check(Rows(database) == before,"nothing written while held");
            Import(bank,Entry(4,BankStorageKind.Bags));
            Check(Rows(database) == before + 1,"saved once the lock is free");
        })),
        ("Bank SQLite 1149: the data file kept busy by another writer (another family's save): the change waits, then goes through",() => InTemp((root,folder,legacy,database) =>
        {
            var store = Store(folder,legacy,database);
            Check(store.Load().Length == 0,"empty bank");
            var other = new TenshoDataDatabase(database).Open();
            TenshoDataDatabase.Run(other,"BEGIN IMMEDIATE;");
            var release = Task.Run(() => { Thread.Sleep(1500); TenshoDataDatabase.Run(other,"ROLLBACK;"); other.Dispose(); });
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var saved = store.Apply(new BankChange(Put: Entry(1,BankStorageKind.Bags)),[]);
            Check(release.Wait(TimeSpan.FromSeconds(30)) && watch.ElapsedMilliseconds >= 1000,"it waited for the other writer: " + watch.ElapsedMilliseconds + " ms");
            Check(saved.Length == 1 && Rows(database) == 1,"then saved");
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/PerCharacterBankStore.cs"));
            Check(code.Contains("catch(SqliteException error) when(error.SqliteErrorCode is 5 or 6){throw new BankLockBusyException(LockPath,error);}",StringComparison.Ordinal),
                "busy past every wait: the lock's own exception (tried again by the service, never a blocked bank)");
        })),
        ("Bank SQLite 1149: a tampered row (key that disagrees, damaged entry) fails closed and blocks every write",() => InTemp((root,folder,legacy,database) =>
        {
            var store = Store(folder,legacy,database);
            store.Load();
            store.Apply(new BankChange(Put: Entry(1,BankStorageKind.Bags)),[]);
            using (var open = new TenshoDataDatabase(database).Open()) TenshoDataDatabase.Run(open,"UPDATE bank_entries SET character=2;");
            var moved = Store(folder,legacy,database);
            Check(Throws<InvalidDataException>(() => moved.Load()) && Throws<InvalidOperationException>(() => moved.Apply(new BankChange(Put: Entry(2,BankStorageKind.Bags)),[])),"a key that disagrees: refused");
            using (var open = new TenshoDataDatabase(database).Open()) TenshoDataDatabase.Run(open,"UPDATE bank_entries SET character=1,entry='{\"characterId\":1}';");
            Check(Throws<InvalidDataException>(() => Store(folder,legacy,database).Load()),"a damaged entry: refused");
            Check(Rows(database) == 1,"nothing written over it");
        })),
        ("Bank SQLite 1149: the desktop application's copy shows the same bank, from the files before the conversion, never converting its copy",() => InTemp((root,folder,legacy,database) =>
        {
            var entries = new[] { Entry(1,BankStorageKind.Saddlebag),Entry(1,BankStorageKind.Retainer,8) };
            new PerCharacterBankStore(folder,legacy).Save(entries);
            Store(folder,legacy,database).Load();
            var mirror = Path.Combine(root,"mirror");
            var copy = TenshoDataDatabase.CopyForDesktop(database,Path.Combine(mirror,TenshoDataDatabase.FileName));
            Check(copy.Copied == 1,"copied: " + string.Join("; ",copy.Problems));
            Directory.CreateDirectory(Path.Combine(mirror,"InventoryBank"));
            var app = new PerCharacterBankStore(Path.Combine(mirror,"InventoryBank"),Path.Combine(mirror,"InventoryBank.json"),data: Data(Path.Combine(mirror,TenshoDataDatabase.FileName)));
            Check(Text(app.Load()) == Text(new PerCharacterBankStore(folder,legacy,data: Data(database)).Load()),"the application shows the same storages");
            Check(Directory.Exists(Path.Combine(mirror,"InventoryBank")) && !Directory.GetDirectories(mirror,"before-sqlite-*").Any(),"an emptied copy of the folder: nothing to convert, nothing moved");
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.DesktopData.cs")).Replace("\r\n","\n");
            Check(window.Contains("var json=File.Exists(legacy) || Directory.Exists(files) && Directory.EnumerateFiles(files,\"*.json*\").Any();")
                && window.Contains("new PerCharacterBankStore(files,legacy,data:!json && File.Exists(file) ? new TenshoDataFile(file,warn):null)")
                && window.Contains("case \"bank\":\n                UseDesktopBank(folder,warn);")
                && window.Contains("UseDesktopVoyages(folder,warn);\n                UseDesktopBank(folder,warn);"),
                "the app reads its copy of the data file once no bank file is left, the copy of the files before, never converting");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.IndexOf("var tenshoData=new TenshoDataFile(",StringComparison.Ordinal) is var made and > 0
                && made < plugin.IndexOf("Path.Combine(pluginInterface.GetPluginConfigDirectory(), \"InventoryBank.json\"), data: tenshoData));",StringComparison.Ordinal),"the plugin's bank on the data file, made before it");
        })),
    ];
}
