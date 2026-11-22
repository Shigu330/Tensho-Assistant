using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.FreeCompany;

// 0.1148 (X-173, user's choice of 12/11): the voyage history in the player's data file, TenshoData.db, beside the gil journal and the achievement
// progress (D-141). Behaviour tests (R28): real files in a temporary folder; the file of before converted whole in one transaction or not at all; a
// stop inside the conversion; a stop between the conversion and the move of the old file; an unreadable old file; a data file that blocks (newer
// layout); a data file that cannot be created (JSON as before); two writers at once; the desktop application's copy reading it. X-574 (0.1793,
// the user's choice of 17/11): the file of before is no longer converted: left aside, never read, moved nor removed (the cases say that now).
internal static class VoyageSqlite1148Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=DateTimeOffset.UtcNow.AddDays(-2);

    private static void InTemp(Action<string,string,string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-voy-1148-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root,Path.Combine(root,"VoyageHistory.json"),Path.Combine(root,TenshoDataDatabase.FileName));}
        finally{SqliteConnection.ClearAllPools();try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    private static TenshoDataFile Data(string database,List<string>? warnings=null,List<string>? said=null)=>new(database,(error,message)=>warnings?.Add(message+" "+error.Message),said is null ? null:line=>{if(line.EndsWith("(X-574).",StringComparison.Ordinal))said.Add(line);});
    private static VoyageRecord Voyage(ulong house,int slot,DateTimeOffset registered,DateTimeOffset? back,params (uint Item,int Count)[] loot)
        =>new(house,slot%2==0 ? VesselKind.Submarine:VesselKind.Airship,slot,"Vessel \""+slot+"\" ×",50+slot,registered,back,[1u,5u,9u],["Deep-sea Site 1","The Ivory Shoals"],
            loot.Select(entry=>new VoyageLoot(entry.Item,entry.Count)).ToArray());
    private static List<VoyageRecord> Sample()=>
    [
        Voyage(0x8000_0000_0000_0003UL,0,At,At.AddHours(18),(22500,3),(22501,1)),
        Voyage(17,1,At.AddHours(1),null,(10,2)),
        Voyage(17,2,At.AddHours(2),At.AddHours(20)),
    ];
    private static bool Same(IReadOnlyList<VoyageRecord> left,IReadOnlyList<VoyageRecord> right)
        =>left.Count==right.Count && left.Zip(right).All(pair=>pair.First.HouseId==pair.Second.HouseId && pair.First.Kind==pair.Second.Kind && pair.First.Slot==pair.Second.Slot
            && pair.First.Vessel==pair.Second.Vessel && pair.First.Rank==pair.Second.Rank && pair.First.RegisteredAt==pair.Second.RegisteredAt && pair.First.ReturnAt==pair.Second.ReturnAt
            && pair.First.Points.SequenceEqual(pair.Second.Points) && pair.First.Route.SequenceEqual(pair.Second.Route) && pair.First.Loot.SequenceEqual(pair.Second.Loot));
    private static string[] Aside(string root)=>Directory.GetDirectories(root,"before-sqlite-*").SelectMany(directory=>Directory.GetFiles(directory)).ToArray();
    private static bool Throws<T>(Action run) where T:Exception
    {
        try{run();return false;}catch(T){return true;}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Voyages SQLite 1148: a file of before is left aside, never read nor moved (X-574); the history saved, the same after a restart; a save writes by key",() => InTemp((root,file,database) =>
        {
            VoyageHistory.Write(file,Sample());var bytes=File.ReadAllBytes(file);
            var said=new List<string>();
            var data=new VoyageHistoryData(file,Data(database,said:said));
            Check(data.Mode=="sqlite","SQLite in use: "+data.Mode);
            Check(data.Load(At).Count==0,"nothing read from the file of before");
            Check(File.ReadAllBytes(file).SequenceEqual(bytes) && Aside(root).Length==0,"the file of before in place, unchanged, nothing set aside");
            Check(said.SequenceEqual(["Voyage history: a file of an old format (VoyageHistory.json) was left aside, not read (X-574)."]),"said once: "+string.Join(" | ",said));
            data.Save(Sample());
            Check(Same(new VoyageHistoryData(file,Data(database)).Load(At),Sample()),"saved (ids above long.MaxValue, no return time, quotes in names), the same after a restart");
            var added=Voyage(17,3,At.AddHours(3),At.AddHours(30),(5,5));
            data.Save(Sample().Skip(1).Append(added).ToList());
            Check(Same(new VoyageHistoryData(file,Data(database)).Load(At),Sample().Append(added).ToList()),"a save writes its voyages by key, the others kept (X-182)");
            Check(said.Count==1 && File.ReadAllBytes(file).SequenceEqual(bytes),"said once; the file of before never written");
            using var open=new TenshoDataDatabase(database).Open();
            Check(Convert.ToInt64(TenshoDataDatabase.Scalar(open,"SELECT COUNT(*) FROM converted;"))==0,"no « converted » mark written");
        })),
        ("Voyages SQLite 1148: a stop inside a save leaves the last save whole; the file of before stays as it is",() => InTemp((root,file,database) =>
        {
            VoyageHistory.Write(file,Sample().Take(1).ToList());var bytes=File.ReadAllBytes(file);
            new VoyageHistoryData(file,Data(database)).Save(Sample());
            var data=new VoyageHistoryData(file,Data(database)){DuringSave=()=>throw new IOException("stop inside the transaction")};
            var changed=Sample();changed[1]=changed[1] with {Loot=[new VoyageLoot(8,8)]};
            Check(Throws<IOException>(()=>data.Save(changed)),"the stop is seen");
            Check(Same(new VoyageHistoryData(file,Data(database)).Load(At),Sample()),"the save of before whole");
            Check(File.ReadAllBytes(file).SequenceEqual(bytes) && Aside(root).Length==0,"the file of before stays, unchanged");
        })),
        ("Voyages SQLite 1148: an old file found again never brings the old history back over newer",() => InTemp((root,file,database) =>
        {
            var data=new VoyageHistoryData(file,Data(database));
            var newer=Sample();newer[0]=newer[0] with {Loot=[new VoyageLoot(7,7)]};data.Save(newer);
            VoyageHistory.Write(file,Sample());var bytes=File.ReadAllBytes(file);
            Check(Same(new VoyageHistoryData(file,Data(database)).Load(At),newer),"newer history kept");
            Check(File.ReadAllBytes(file).SequenceEqual(bytes) && Aside(root).Length==0,"the old file left where it is, unchanged");
        })),
        ("Voyages SQLite 1148: an unreadable file of before, a blocked data file, no data file: never written over",() => InTemp((root,file,database) =>
        {
            File.WriteAllText(file,"{ not json");
            var warnings=new List<string>();
            var store=new VoyageHistoryStore(file,(_,message)=>warnings.Add(message),Data(database));
            // X-574 (0.1793): the file of before is not read: the history (the data file's) stays usable.
            Check(!store.Unavailable && store.Records.Count==0 && warnings.Count==0,"unreadable file of before: not read, the history usable");
            Check(File.ReadAllText(file)=="{ not json" && Aside(root).Length==0,"left where it is");
            File.Delete(file);
            using(var open=new TenshoDataDatabase(database).Open())TenshoDataDatabase.Run(open,"PRAGMA user_version=9;");
            SqliteConnection.ClearAllPools();
            var blockedBytes=File.ReadAllBytes(database);
            var blocked=new VoyageHistoryStore(file,(_,message)=>warnings.Add(message),Data(database));
            Check(blocked.Unavailable && !File.Exists(file),"a data file of a newer layout: unavailable, no JSON file made instead");
            Check(File.ReadAllBytes(database).SequenceEqual(blockedBytes),"the newer data file never written");
            var nowhere=Path.Combine(root,"missing-folder","sub",TenshoDataDatabase.FileName);
            var json=new VoyageHistoryData(file,new TenshoDataFile(nowhere,(_,_)=>{}));
            Check(json.Mode=="json","no data file can be made: JSON as before");
            json.Save(Sample());
            Check(File.Exists(file) && Same(VoyageHistory.Read(file),Sample()),"written to the JSON file");
            Check(new VoyageHistoryData(file,null).Mode=="json","no data file given: JSON");
        })),
        ("Voyages SQLite 1148: two writers at once, each save whole; the desktop application's copy reads it",() => InTemp((root,file,database) =>
        {
            var first=Sample();var second=Sample().Skip(1).Append(Voyage(99,4,At.AddHours(4),null,(1,1))).ToList();
            var a=new VoyageHistoryData(file,Data(database));var b=new VoyageHistoryData(file,Data(database));
            a.Load(At);
            Parallel.For(0,40,index=>{if(index%2==0)a.Save(first);else b.Save(second);});
            var saved=new VoyageHistoryData(file,Data(database)).Load(At);
            Check(Same(saved,Sample().Append(second[^1]).ToList()),"neither erases the other's voyages (X-182): "+saved.Count);
            var mirror=Path.Combine(root,"mirror");
            var copy=TenshoDataDatabase.CopyForDesktop(database,Path.Combine(mirror,TenshoDataDatabase.FileName));
            Check(copy.Copied==1,"copied: "+string.Join("; ",copy.Problems));
            var app=new VoyageHistoryStore(Path.Combine(mirror,"VoyageHistory.json"),(_,_)=>{},Data(Path.Combine(mirror,TenshoDataDatabase.FileName)));
            Check(!app.Unavailable && Same(app.Records,VoyageHistory.Prune(saved,DateTimeOffset.UtcNow)),"the application shows the same voyages");
            // X-413 (0.1385): line ends normalised first; « A && CRLF || LF » never checked A on an LF tree.
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.DesktopData.cs")).Replace("\r\n","\n");
            // X-574 (0.1793): the copy of the data file whenever there is one (a VoyageHistory.json of before left in the plugin's folder is no
            // longer moved: it must never win); the JSON copy only without it (the plugin's JSON mode).
            Check(window.Contains("new FreeCompany.VoyageHistoryStore(json,warn,File.Exists(file) ? new TenshoDataFile(file,warn):null)")
                && window.Contains("UseDesktopPlayerData(folder,warn);\n                UseDesktopVoyages(folder,warn);"),
                "the app reads its copy of the data file when there is one, the JSON copy otherwise, never converting it");
            // X-153 (0.1403): the store is created in the voyages family (Plugin.Voyages.cs), called after the persistence family.
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            var voyages=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Voyages.cs"));
            Check(plugin.IndexOf("var (tenshoData,bank,bankBridge)=RegisterPersistence(",StringComparison.Ordinal) is var made and >0
                && made<plugin.IndexOf("RegisterVoyages(pluginInterface,configuration,data,framework,client,tenshoData,",StringComparison.Ordinal)
                && voyages.Contains("new FreeCompany.VoyageHistoryStore(Path.Combine(pluginInterface.GetPluginConfigDirectory(),\"VoyageHistory.json\"),(error,message)=>log.Warning(error,message),tenshoData)",StringComparison.Ordinal),"the plugin's store on the data file");
        })),
    ];
}
