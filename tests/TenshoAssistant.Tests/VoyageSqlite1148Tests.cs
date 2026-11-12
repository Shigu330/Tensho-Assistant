using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.FreeCompany;

// 0.1148 (X-173, user's choice of 12/11): the voyage history in the player's data file, TenshoData.db, beside the gil journal and the achievement
// progress (D-141). Behaviour tests (R28): real files in a temporary folder; the file of before converted whole in one transaction or not at all; a
// stop inside the conversion; a stop between the conversion and the move of the old file; an unreadable old file; a data file that blocks (newer
// layout); a data file that cannot be created (JSON as before); two writers at once; the desktop application's copy reading it.
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
    private static TenshoDataFile Data(string database,List<string>? warnings=null)=>new(database,(error,message)=>warnings?.Add(message+" "+error.Message));
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
        ("Voyages SQLite 1148: the file of before converted whole, kept aside byte for byte, the same after a restart; a save replaces the history",() => InTemp((root,file,database) =>
        {
            VoyageHistory.Write(file,Sample());var bytes=File.ReadAllBytes(file);
            var data=new VoyageHistoryData(file,Data(database));
            Check(data.Mode=="sqlite","SQLite in use: "+data.Mode);
            Check(Same(data.Load(At),Sample()),"same voyages after the conversion (ids above long.MaxValue, no return time, quotes in names)");
            Check(!File.Exists(file) && Aside(root) is [var kept] && File.ReadAllBytes(kept).SequenceEqual(bytes),"the file of before kept aside, unchanged");
            Check(Same(new VoyageHistoryData(file,Data(database)).Load(At),Sample()),"after a restart");
            var newer=Sample().Skip(1).Append(Voyage(17,3,At.AddHours(3),At.AddHours(30),(5,5))).ToList();
            data.Save(newer);
            Check(Same(new VoyageHistoryData(file,Data(database)).Load(At),newer),"a save replaces the whole history");
            using var open=new TenshoDataDatabase(database).Open();
            Check(TenshoDataDatabase.Converted(open,VoyageHistoryData.Family),"marked converted");
        })),
        ("Voyages SQLite 1148: a stop inside the conversion leaves the file of before in place and nothing marked; the next start converts it",() => InTemp((root,file,database) =>
        {
            VoyageHistory.Write(file,Sample());
            var data=new VoyageHistoryData(file,Data(database)){DuringSave=()=>throw new IOException("stop inside the transaction")};
            Check(Throws<IOException>(()=>data.Load(At)),"the stop is seen");
            Check(File.Exists(file) && Aside(root).Length==0,"the file of before stays");
            using(var open=new TenshoDataDatabase(database).Open())
            {
                Check(!TenshoDataDatabase.Converted(open,VoyageHistoryData.Family) && Convert.ToInt64(TenshoDataDatabase.Scalar(open,"SELECT COUNT(*) FROM voyage_records;"))==0,"no row, no mark");
            }
            Check(Throws<InvalidOperationException>(()=>new VoyageHistoryData(file,Data(database)).Save(Sample().Take(1).ToList())),"a save never writes over a file of before not converted");
            Check(Same(new VoyageHistoryData(file,Data(database)).Load(At),Sample()),"converted at the next start");
        })),
        ("Voyages SQLite 1148: a stop between the conversion and the move never brings the old history back over newer",() => InTemp((root,file,database) =>
        {
            VoyageHistory.Write(file,Sample());
            var data=new VoyageHistoryData(file,Data(database));data.Load(At);
            var newer=Sample().Take(1).ToList();data.Save(newer);
            File.Copy(Aside(root)[0],file);
            Check(Same(new VoyageHistoryData(file,Data(database)).Load(At),newer),"newer history kept");
            Check(!File.Exists(file) && Aside(root).Length==2,"the old file set aside again, both copies kept");
        })),
        ("Voyages SQLite 1148: an unreadable file of before, a blocked data file, no data file: never written over",() => InTemp((root,file,database) =>
        {
            File.WriteAllText(file,"{ not json");
            var warnings=new List<string>();
            var store=new VoyageHistoryStore(file,(_,message)=>warnings.Add(message),Data(database));
            Check(store.Unavailable && store.Records.Count==0 && warnings.Count==1,"unreadable: unavailable, said once");
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
            Check(Same(saved,first) || Same(saved,second),"one whole save, never a mix: "+saved.Count);
            var mirror=Path.Combine(root,"mirror");
            var copy=TenshoDataDatabase.CopyForDesktop(database,Path.Combine(mirror,TenshoDataDatabase.FileName));
            Check(copy.Copied==1,"copied: "+string.Join("; ",copy.Problems));
            var app=new VoyageHistoryStore(Path.Combine(mirror,"VoyageHistory.json"),(_,_)=>{},Data(Path.Combine(mirror,TenshoDataDatabase.FileName)));
            Check(!app.Unavailable && Same(app.Records,VoyageHistory.Prune(saved,DateTimeOffset.UtcNow)),"the application shows the same voyages");
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.DesktopData.cs"));
            Check(window.Contains("new FreeCompany.VoyageHistoryStore(json,warn,!File.Exists(json) && File.Exists(file) ? new TenshoDataFile(file,warn):null)")
                && window.Contains("UseDesktopPlayerData(folder,warn);\n                UseDesktopVoyages(folder,warn);".Replace("\n",Environment.NewLine)) || window.Contains("UseDesktopPlayerData(folder,warn);\n                UseDesktopVoyages(folder,warn);"),
                "the app reads its copy of the data file once converted, the JSON copy before, never converting it itself");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.IndexOf("var tenshoData=new TenshoDataFile(",StringComparison.Ordinal) is var made and >0
                && made<plugin.IndexOf("new FreeCompany.VoyageHistoryStore(Path.Combine(pluginInterface.GetPluginConfigDirectory(),\"VoyageHistory.json\"),(error,message)=>log.Warning(error,message),tenshoData)",StringComparison.Ordinal),"the plugin's store on the data file");
        })),
    ];
}
