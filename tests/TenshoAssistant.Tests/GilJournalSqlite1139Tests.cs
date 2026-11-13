using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Inventory;

// 0.1139 (X-173, user's choice of 12/11): the gil journal in the player's data file, TenshoData.db (SQLite); the files of before converted once and
// kept aside. Behaviour tests (R28): real files in a temporary folder; a stop simulated inside a save and inside a conversion, and between the
// conversion and the move of the old file; an unreadable old file; a damaged data file and one of a newer layout, never written over; a data file
// that cannot be created (JSON as before); two writers on one file (two game clients) and a reader while the game writes.
internal static class GilJournalSqlite1139Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,12,5,0,0,TimeSpan.Zero);
    // An id above long.MaxValue: SQLite integers are signed, the same bits must come back.
    private const ulong Character=0x8000_0000_0000_0042UL;

    private static void InTemp(Action<string,string,string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-gil-1139-"+Guid.NewGuid().ToString("N"));
        var folder=Path.Combine(root,"GilJournal");Directory.CreateDirectory(folder);
        try{run(root,folder,Path.Combine(root,TenshoDataDatabase.FileName));}
        finally{SqliteConnection.ClearAllPools();try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    private static GilJournalStore Store(string folder,string database,List<string>? warnings=null)
        =>new(folder,database,(error,message)=>warnings?.Add(message+" "+error.Message));
    private static List<GilEntry> Entries(int count,long start=1000)
        =>Enumerable.Range(0,count).Select(index=>new GilEntry(At.AddMinutes(index),index%2==0 ? 100+index:-(50+index),start+index,
            (GilSource)(index%3),(GilCategory)(index%9))).ToList();
    private static bool Same(IReadOnlyList<GilEntry> left,IReadOnlyList<GilEntry> right)=>left.SequenceEqual(right);
    private static string? Aside(string folder)
        =>Directory.GetDirectories(folder,"before-sqlite-*").SelectMany(directory=>Directory.GetFiles(directory,"*.json")).FirstOrDefault();
    private static bool Throws<T>(Action run) where T:Exception
    {
        try{run();return false;}catch(T){return true;}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gil SQLite 1139: the file of before converted once (same entries, id above long.MaxValue), kept aside byte for byte, read again after a restart",() => InTemp((_,folder,database) =>
        {
            var json=Path.Combine(folder,GilJournalStore.FileName(Character));
            var before=Entries(3);GilJournal.Write(json,before);var bytes=File.ReadAllBytes(json);
            var store=Store(folder,database);
            Check(store.Mode=="sqlite","SQLite in use: "+store.Mode);
            Check(Same(store.Load(Character,At),before),"the converted journal has the same entries");
            Check(!File.Exists(json),"the file of before is no longer read");
            Check(Aside(folder) is { } kept && File.ReadAllBytes(kept).SequenceEqual(bytes),"kept aside unchanged");
            var restarted=Store(folder,database);
            Check(Same(restarted.Load(Character,At),before),"after a restart: the same entries, none twice");
            var newer=Entries(5,2000);restarted.Save(Character,newer);
            Check(Same(Store(folder,database).Load(Character,At),newer),"a save replaces the journal whole");
        })),
        ("Gil SQLite 1139: a stop between the conversion and the move of the old file never brings the old entries back over newer ones",() => InTemp((_,folder,database) =>
        {
            var json=Path.Combine(folder,GilJournalStore.FileName(Character));
            var old=Entries(3);GilJournal.Write(json,old);
            var store=Store(folder,database);store.Load(Character,At);
            var newer=Entries(6,5000);store.Save(Character,newer);
            File.Copy(Aside(folder)!,json);                          // the old file still in place, as if the move had not happened
            var restarted=Store(folder,database);
            Check(Same(restarted.Load(Character,At),newer),"the newer journal is kept; the old file is not read again");
            Check(!File.Exists(json),"the old file set aside again");
            Check(Directory.GetDirectories(folder,"before-sqlite-*").SelectMany(directory=>Directory.GetFiles(directory)).Count()==2,"both copies kept, none removed");
        })),
        ("Gil SQLite 1139: a stop inside a save leaves the last save whole; inside a conversion, the old file stays and converts at the next start",() => InTemp((_,folder,database) =>
        {
            var store=Store(folder,database);
            var first=Entries(4);store.Save(1,first);
            store.DuringSave=()=>throw new IOException("simulated stop inside the transaction");
            Check(Throws<IOException>(()=>store.Save(1,Entries(9,7000))),"the save fails");
            Check(Same(Store(folder,database).Load(1,At),first),"the save of before is whole");
            var json=Path.Combine(folder,GilJournalStore.FileName(2));
            var old=Entries(2);GilJournal.Write(json,old);var bytes=File.ReadAllBytes(json);
            Check(Throws<IOException>(()=>store.Load(2,At)),"the conversion fails");
            Check(File.Exists(json) && File.ReadAllBytes(json).SequenceEqual(bytes) && Aside(folder) is null,"the old file stays where it was, unchanged");
            Check(Same(Store(folder,database).Load(2,At),old),"converted at the next start");
        })),
        ("Gil SQLite 1139: an unreadable file of before is left in place and never written over",() => InTemp((_,folder,database) =>
        {
            var json=Path.Combine(folder,GilJournalStore.FileName(Character));
            File.WriteAllText(json,"{ not a journal");
            var store=Store(folder,database);
            Check(Throws<Exception>(()=>store.Load(Character,At)),"the load fails (the watcher then never saves)");
            Check(Throws<InvalidOperationException>(()=>store.Save(Character,Entries(2))),"a save is refused while the old file is not converted");
            Check(File.ReadAllText(json)=="{ not a journal" && Aside(folder) is null,"the file is untouched, in place");
        })),
        ("Gil SQLite 1139: a damaged data file and one of a newer layout are refused and never written over",() => InTemp((root,folder,database) =>
        {
            var junk=new byte[8192];new Random(1139).NextBytes(junk);File.WriteAllBytes(database,junk);
            var json=Path.Combine(folder,GilJournalStore.FileName(Character));GilJournal.Write(json,Entries(2));var jsonBytes=File.ReadAllBytes(json);
            var warnings=new List<string>();var store=Store(folder,database,warnings);
            Check(store.Mode=="blocked","damaged: blocked, " +store.Mode);
            Check(Throws<IOException>(()=>store.Load(Character,At)) && Throws<IOException>(()=>store.Save(Character,Entries(3))),"no load, no save");
            Check(File.ReadAllBytes(database).SequenceEqual(junk),"the damaged file is unchanged");
            Check(File.ReadAllBytes(json).SequenceEqual(jsonBytes),"the old journal file is unchanged");
            Check(warnings.Any(warning=>warning.Contains("not written this session",StringComparison.Ordinal)),"said in the log");

            var newer=Path.Combine(root,"newer.db");
            var made=Store(folder,newer);made.Save(7,Entries(2));
            using(var open=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=newer,Pooling=false}.ToString()))
            {open.Open();using var command=open.CreateCommand();command.CommandText="PRAGMA user_version=2;";command.ExecuteNonQuery();}
            var later=Store(folder,newer);
            Check(later.Mode=="blocked" && Throws<IOException>(()=>later.Save(7,Entries(5))),"a newer layout is refused");
            using(var open=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=newer,Pooling=false}.ToString()))
            {
                open.Open();using var command=open.CreateCommand();
                command.CommandText="SELECT (SELECT user_version FROM pragma_user_version)||'/'||(SELECT COUNT(*) FROM gil_entries);";
                Check(command.ExecuteScalar() as string=="2/2","layout and rows unchanged: "+command.ExecuteScalar());
            }
        })),
        ("Gil SQLite 1139: a data file that cannot be created leaves the JSON files in use as before",() => InTemp((_,folder,database) =>
        {
            Directory.CreateDirectory(database);                         // a folder in the way: no file can be created there
            var store=Store(folder,database);
            Check(store.Mode=="json","JSON as before: "+store.Mode);
            var entries=Entries(3);store.Save(Character,entries);
            Check(File.Exists(Path.Combine(folder,GilJournalStore.FileName(Character))),"written as a JSON file");
            Check(Same(Store(folder,database).Load(Character,At),entries),"read back");
        })),
        ("Gil SQLite 1139: two writers on one file (two game clients) and a reader while they write: every journal read is one save, whole",() => InTemp((_,folder,database) =>
        {
            var first=Store(folder,database);var second=Store(folder,database);var reader=Store(folder,database);
            var small=Entries(100);var large=Entries(200,9000);
            first.Save(1,small);second.Save(2,small);
            var errors=new System.Collections.Concurrent.ConcurrentQueue<string>();
            void Write(GilJournalStore store,ulong id)
            {
                try{for(var round=0;round<15;round++)store.Save(id,round%2==0 ? large:small);}catch(Exception error){errors.Enqueue(error.Message);}
            }
            var writers=new[]{Task.Run(()=>Write(first,1)),Task.Run(()=>Write(second,2)),Task.Run(()=>Write(second,1)),Task.Run(()=>Write(first,2))};
            while(!writers.All(task=>task.IsCompleted))
                foreach(var id in new ulong[]{1,2})
                {
                    var read=reader.Load(id,At);
                    if(!Same(read,small) && !Same(read,large))errors.Enqueue($"journal {id} read half written: {read.Count} entries");
                }
            Task.WaitAll(writers);
            Check(errors.IsEmpty,string.Join("; ",errors.Take(3)));
            foreach(var id in new ulong[]{1,2}){var last=reader.Load(id,At);Check(Same(last,small) || Same(last,large),"journal "+id+" is one whole save");}
        })),
        ("Gil SQLite 1139: wiring (the plugin passes TenshoData.db to the watcher, saves and loads through the store, folds the journal in at unload)",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("new Inventory.GilJournalStore(",StringComparison.Ordinal) && File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Persistence.cs")).Contains("TenshoDataDatabase.FileName",StringComparison.Ordinal),"plugin (the data file in the persistence family since 0.1239, X-153)");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/GilWatcher.Dalamud.cs"));
            Check(watcher.Contains("()=>store.Save(id,copy)",StringComparison.Ordinal) && watcher.Contains("store.Checkpoint();",StringComparison.Ordinal)
                && watcher.Contains("if(!dirty || character==0 || loadFailed)return;",StringComparison.Ordinal),"watcher");
            var security=File.ReadAllText(Path.GetFullPath("SECURITY.md"));
            var halves=security.Split("\n---",2);
            Check(halves.Length==2 && halves[0].Contains("TenshoData.db",StringComparison.Ordinal) && halves[1].Contains("TenshoData.db",StringComparison.Ordinal),"SECURITY, both halves");
        }),
    ];
}
