using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Collections;
using TenshoAssistant.Inventory;

// 0.1140 (X-173, user's choice of 12/11): the achievement progress in the player's data file, TenshoData.db, beside the gil journal (one data
// file for the families of X-173, D-141). Behaviour tests (R28): real files in a temporary folder; the file of before converted whole in one
// transaction or not at all; a stop inside a save and inside the conversion; a stop between the conversion and the move of the old file; an
// unreadable old file; one damaged data file blocking every family; a data file that cannot be created (JSON as before); a data file of 0.1139
// opened by this version, its gil rows kept.
internal static class AchievementSqlite1140Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,12,6,0,0,TimeSpan.Zero);
    private const ulong First=18014498549678007UL,Second=0x8000_0000_0000_0007UL;

    private static void InTemp(Action<string,string,string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-ach-1140-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root,Path.Combine(root,"AchievementProgress.json"),Path.Combine(root,TenshoDataDatabase.FileName));}
        finally{SqliteConnection.ClearAllPools();try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    private static TenshoDataFile Data(string database,List<string>? warnings=null)=>new(database,(error,message)=>warnings?.Add(message+" "+error.Message));
    private static Dictionary<ulong,Dictionary<uint,AchievementProgress>> Sample()=>new()
    {
        [First]=new(){[3811]=new(0,1,At.AddDays(-3)),[1357]=new(200,200,At.AddDays(-2))},
        [Second]=new(){[42]=new(7,10,At)},
    };
    private static bool Same(Dictionary<ulong,Dictionary<uint,AchievementProgress>> left,Dictionary<ulong,Dictionary<uint,AchievementProgress>> right)
        =>left.Count==right.Count && left.All(pair=>right.TryGetValue(pair.Key,out var other) && pair.Value.Count==other.Count
            && pair.Value.All(item=>other.TryGetValue(item.Key,out var value) && value==item.Value));
    private static string[] Aside(string root)=>Directory.GetDirectories(root,"before-sqlite-*").SelectMany(directory=>Directory.GetFiles(directory)).ToArray();
    private static bool Throws<T>(Action run) where T:Exception
    {
        try{run();return false;}catch(T){return true;}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Achievements SQLite 1140: the file of before converted whole (ids above long.MaxValue), kept aside byte for byte, the same after a restart",() => InTemp((root,file,database) =>
        {
            File.WriteAllText(file,AchievementProgressStore.Write(Sample()));var bytes=File.ReadAllBytes(file);
            var store=new AchievementProgressData(file,Data(database));
            Check(store.Mode=="sqlite","SQLite in use: "+store.Mode);
            Check(Same(store.Load(At),Sample()),"same progress after the conversion");
            Check(!File.Exists(file) && Aside(root) is [var kept] && File.ReadAllBytes(kept).SequenceEqual(bytes),"the file of before kept aside, unchanged");
            Check(Same(new AchievementProgressData(file,Data(database)).Load(At),Sample()),"after a restart");
            var all=Sample();all[First][3811]=new(1,1,At.AddMinutes(5));
            store.Save(First,all[First],all);
            Check(Same(new AchievementProgressData(file,Data(database)).Load(At),all),"a save replaces that character's rows; the other character untouched");
        })),
        ("Achievements SQLite 1140: a stop between the conversion and the move never brings the old progress back over newer",() => InTemp((root,file,database) =>
        {
            File.WriteAllText(file,AchievementProgressStore.Write(Sample()));
            var store=new AchievementProgressData(file,Data(database));store.Load(At);
            var all=Sample();all[Second][42]=new(10,10,At.AddHours(1));store.Save(Second,all[Second],all);
            File.Copy(Aside(root)[0],file);
            Check(Same(new AchievementProgressData(file,Data(database)).Load(At),all),"newer progress kept");
            Check(!File.Exists(file) && Aside(root).Length==2,"the old file set aside again, both copies kept");
        })),
        ("Achievements SQLite 1140: a stop inside the conversion leaves the old file and nothing half written; inside a save, the last save whole",() => InTemp((root,file,database) =>
        {
            File.WriteAllText(file,AchievementProgressStore.Write(Sample()));var bytes=File.ReadAllBytes(file);
            var data=Data(database);
            var store=new AchievementProgressData(file,data){DuringSave=()=>throw new IOException("simulated stop")};
            Check(Throws<IOException>(()=>store.Load(At)),"the conversion fails");
            Check(File.ReadAllBytes(file).SequenceEqual(bytes) && Aside(root).Length==0,"the old file in place, unchanged");
            using(var open=data.Database()!.Open())
            {
                Check(Convert.ToInt64(TenshoDataDatabase.Scalar(open,"SELECT COUNT(*) FROM achievement_progress;"))==0,"no row of a half conversion");
                Check(!TenshoDataDatabase.Converted(open,AchievementProgressData.Family),"no « converted » mark");
            }
            var restarted=new AchievementProgressData(file,Data(database));
            Check(Same(restarted.Load(At),Sample()),"converted at the next start");
            restarted.DuringSave=()=>throw new IOException("simulated stop");
            var changed=Sample();changed[First].Clear();
            Check(Throws<IOException>(()=>restarted.Save(First,changed[First],changed)),"the save fails");
            Check(Same(new AchievementProgressData(file,Data(database)).Load(At),Sample()),"the save of before whole");
        })),
        // D-145 (0.1205, relay 37): it used to block every save of the family for good; it is now set aside whole (never written over nor
        // removed) and the family goes on: the progress is seen again in game.
        ("Achievements SQLite 1140: an unreadable file of before is set aside whole, never written over; the progress is saved again",() => InTemp((root,file,database) =>
        {
            File.WriteAllText(file,"{bad");
            var store=new AchievementProgressData(file,Data(database));
            Check(store.Load(At).Count==0,"nothing read from it, no error");
            Check(!File.Exists(file) && Aside(root) is [var kept] && File.ReadAllText(kept)=="{bad","set aside byte for byte, never removed");
            store.Save(First,Sample()[First],Sample());
            Check(Same(new AchievementProgressData(file,Data(database)).Load(At),new(){[First]=Sample()[First]}),"saved again");
        })),
        ("Achievements SQLite 1140: one damaged data file blocks every family and is never written over; a file that cannot be created keeps JSON",() => InTemp((root,file,database) =>
        {
            var junk=new byte[4096];new Random(1140).NextBytes(junk);File.WriteAllBytes(database,junk);
            File.WriteAllText(file,AchievementProgressStore.Write(Sample()));var bytes=File.ReadAllBytes(file);
            var warnings=new List<string>();var data=Data(database,warnings);
            var achievements=new AchievementProgressData(file,data);var gil=new GilJournalStore(Path.Combine(root,"GilJournal"),data);
            Check(achievements.Mode=="blocked" && gil.Mode=="blocked","both blocked");
            Check(Throws<IOException>(()=>achievements.Load(At)) && Throws<IOException>(()=>achievements.Save(First,Sample()[First],Sample()))
                && Throws<IOException>(()=>gil.Save(1,[])),"nothing read nor written");
            Check(File.ReadAllBytes(database).SequenceEqual(junk) && File.ReadAllBytes(file).SequenceEqual(bytes),"both files unchanged");
            Check(warnings.Count==1,"said once: "+warnings.Count);

            var folder=Path.Combine(root,"json-only");Directory.CreateDirectory(Path.Combine(folder,TenshoDataDatabase.FileName));
            var json=Path.Combine(folder,"AchievementProgress.json");
            var fallback=new AchievementProgressData(json,Data(Path.Combine(folder,TenshoDataDatabase.FileName)));
            Check(fallback.Mode=="json","JSON as before: "+fallback.Mode);
            fallback.Save(First,Sample()[First],Sample());
            Check(AchievementProgressStore.Read(File.ReadAllText(json)) is { } back && Same(back,Sample()),"the whole file written as before");
        })),
        ("Achievements SQLite 1140: a data file of 0.1139 (gil tables only) is opened, its gil rows kept, the new tables added, the layout number unchanged",() => InTemp((root,file,database) =>
        {
            using(var open=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=database,Pooling=false}.ToString()))
            {
                open.Open();
                TenshoDataDatabase.Run(open,"PRAGMA journal_mode=WAL;CREATE TABLE gil_entries(character INTEGER NOT NULL,seq INTEGER NOT NULL,at INTEGER NOT NULL,delta INTEGER NOT NULL,"
                    +"balance INTEGER NOT NULL,source INTEGER NOT NULL,category INTEGER NOT NULL,PRIMARY KEY(character,seq)) WITHOUT ROWID;"
                    +"CREATE TABLE gil_converted(character INTEGER PRIMARY KEY NOT NULL,at INTEGER NOT NULL);"
                    +$"INSERT INTO gil_entries VALUES(5,0,{At.UtcTicks},-100,900,0,4);INSERT INTO gil_entries VALUES(5,1,{At.AddMinutes(1).UtcTicks},50,950,0,2);PRAGMA user_version=1;");
            }
            var data=Data(database);
            var gil=new GilJournalStore(Path.Combine(root,"GilJournal"),data).Load(5,At);
            Check(gil.Count==2 && gil[0].Delta==-100 && gil[1].Balance==950,"gil rows of 0.1139 kept");
            var store=new AchievementProgressData(file,data);
            Check(store.Load(At).Count==0,"no achievements yet");
            store.Save(First,Sample()[First],Sample());
            Check(new AchievementProgressData(file,Data(database)).Load(At)[First].Count==2,"saved beside the gil journal");
            using var check=data.Database()!.Open();
            Check(Convert.ToInt64(TenshoDataDatabase.Scalar(check,"PRAGMA user_version;"))==TenshoDataDatabase.Schema && TenshoDataDatabase.Schema==1,"layout 1: 0.1139 still opens the file");
        })),
        ("Achievements SQLite 1140: wiring (one data file for the plugin, the progress read off the frame, an unreadable one never written over)",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("var (tenshoData,bank,bankBridge)=RegisterPersistence(",StringComparison.Ordinal) && File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Persistence.cs")).Contains("var tenshoData=new TenshoDataFile(",StringComparison.Ordinal) && plugin.Contains("new Inventory.GilJournalStore(Path.Combine(pluginInterface.GetPluginConfigDirectory(),\"GilJournal\"),tenshoData)",StringComparison.Ordinal)
                && plugin.Contains("new Collections.AchievementProgressData(Path.Combine(pluginInterface.GetPluginConfigDirectory(),\"AchievementProgress.json\"),tenshoData)",StringComparison.Ordinal),"plugin");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Collections/AchievementProgressWatcher.Dalamud.cs"));
            Check(watcher.Contains("loading=data is null ? null:Task.Run(()=>data.Load(DateTimeOffset.UtcNow));",StringComparison.Ordinal)
                && watcher.Contains("if(!read.IsCompleted)return;",StringComparison.Ordinal) && watcher.Contains("if(data is { } store && !saveBlocked && character!=0)",StringComparison.Ordinal)
                && !watcher.Contains("File.ReadAllText",StringComparison.Ordinal),"watcher");
            var security=File.ReadAllText(Path.GetFullPath("SECURITY.md")).Split("\n---",2);
            Check(security[0].Contains("achievement progress",StringComparison.Ordinal) && security[0].Contains("AchievementProgress.json",StringComparison.Ordinal)
                && security[1].Contains("progression des succès",StringComparison.Ordinal),"SECURITY, both halves");
        }),
    ];
}
