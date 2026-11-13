using System.Text;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Inventory;

// 0.1212 (X-185 part 2, night review of 12/11, N10): « Erase this character » left its gil journal, its achievement progress, the copies of its files
// set aside at the conversion and the freed pages of TenshoData.db and its journal. Behaviour test (R28, privacy): a real profile folder converted,
// a marker name only that character holds, the erase, then no file of that character anywhere and no byte of the marker in the data file, its
// journal or the copies kept; the other character untouched. The same without the data file (files as before).
internal static class CharacterErase1212Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string Marker="Qzxjv Erasemarker";

    private static BankEntry Entry(ulong character,BankStorageKind kind,string name)
    {
        var owner=new InventoryOwnerId(character);
        var containers=BankStorageScope.ContainerIds(kind).Select(id=>new InventoryContainerSnapshot(new(owner,new(id)),InventoryCoverage.Complete,
            id==BankStorageScope.ContainerIds(kind)[0] ? [new InventoryStack(new(new(5057),false),3,0)]:[])).ToArray();
        return new(new(character,kind,0),name,new(DateTimeOffset.UtcNow,"test",containers));
    }
    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-erase-1212-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{SqliteConnection.ClearAllPools();try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    private static string[] FilesOf(string root,ulong character)=>Directory.GetFiles(root,"*",SearchOption.AllDirectories)
        .Where(path=>Path.GetFileName(path) is var name && (name==$"{character}.json" || name.StartsWith($"{character}.json.",StringComparison.Ordinal))).ToArray();
    private static bool Holds(string path,string text)=>File.Exists(path) && Encoding.UTF8.GetString(File.ReadAllBytes(path)).Contains(text,StringComparison.Ordinal);
    private static long Count(string database,string table,ulong character)
    {
        using var open=new TenshoDataDatabase(database).Open();
        using var command=open.CreateCommand();
        command.CommandText=$"SELECT COUNT(*) FROM {table} WHERE character=$character;";command.Parameters.AddWithValue("$character",unchecked((long)character));
        return Convert.ToInt64(command.ExecuteScalar());
    }
    // A gil journal file of before kept aside by the conversion of 0.1139 (GilJournal/before-sqlite-…/<id>.json).
    private static void GilCopy(string root,ulong character)
    {
        var aside=Path.Combine(root,"GilJournal","before-sqlite-20261110-120000");Directory.CreateDirectory(aside);
        File.WriteAllText(Path.Combine(aside,$"{character}.json"),$"[\"{(character==5 ? Marker:"other")}\"]");
    }

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Character erase 1212: behaviour: storages, gil journal, achievements, copies set aside and freed pages; the other character untouched (N10)",() => InTemp(root =>
        {
            var folder=Path.Combine(root,"InventoryBank");var legacy=Path.Combine(root,"InventoryBank.json");var database=Path.Combine(root,TenshoDataDatabase.FileName);
            new PerCharacterBankStore(folder,legacy).Save([Entry(5,BankStorageKind.Saddlebag,Marker+" (World)"),Entry(6,BankStorageKind.Bags,"Other (World)")]);
            File.Copy(Path.Combine(folder,"5.json"),Path.Combine(folder,"5.json.bak"));
            GilCopy(root,5);GilCopy(root,6);
            var store=new PerCharacterBankStore(folder,legacy,10_000,new TenshoDataFile(database,(error,message)=>throw new Exception(message,error)));
            Check(store.Load().Length==2 && !Directory.Exists(folder),"converted, the folder of before set aside");
            Check(FilesOf(root,5).Length==3,"character 5's files kept aside before the erase (bank, its copy, gil journal): "+FilesOf(root,5).Length);
            using(var open=new TenshoDataDatabase(database).Open())
                foreach(var character in new[]{5L,6L})
                {
                    TenshoDataDatabase.Run(open,$"INSERT INTO gil_entries(character,seq,at,delta,balance,source,category) VALUES({character},1,1,10,10,0,0);");
                    TenshoDataDatabase.Run(open,$"INSERT INTO achievement_progress(character,achievement,current,max,seen_at) VALUES({character},7,1,5,1);");
                }
            Check(Holds(database,Marker) || Holds(database+"-wal",Marker),"the marker is in the data file before the erase");
            var left=store.Apply(new BankChange(Erase:5),store.Load());
            Check(left.Length==1 && left[0].Key.CharacterId==6,"only character 6 left in the bank");
            foreach(var table in TenshoDataDatabase.CharacterTables)Check(Count(database,table,5)==0 && Count(database,table,6)==1,$"{table}: 5 erased, 6 kept");
            Check(FilesOf(root,5).Length==0,"no file of character 5 anywhere: "+string.Join(", ",FilesOf(root,5)));
            Check(FilesOf(root,6).Length==2,"character 6's copies kept (bank and gil journal)");
            SqliteConnection.ClearAllPools();
            var everything=Directory.GetFiles(root,"*",SearchOption.AllDirectories);
            Check(!everything.Any(path=>Holds(path,Marker)),"no byte of the marker left in the data file, its journal or a copy: "+string.Join(", ",everything.Where(path=>Holds(path,Marker))));
            Check(new PerCharacterBankStore(folder,legacy,10_000,new TenshoDataFile(database,(error,message)=>throw new Exception(message,error))).Load().Single().Key.CharacterId==6,"after a restart");
        })),
        ("Character erase 1212: behaviour: without the data file, its file, its copy and its copies set aside are removed (N10)",() => InTemp(root =>
        {
            var folder=Path.Combine(root,"InventoryBank");var legacy=Path.Combine(root,"InventoryBank.json");
            var store=new PerCharacterBankStore(folder,legacy);
            store.Save([Entry(5,BankStorageKind.Saddlebag,Marker+" (World)"),Entry(6,BankStorageKind.Bags,"Other (World)")]);
            File.Copy(Path.Combine(folder,"5.json"),Path.Combine(folder,"5.json.bak"));
            GilCopy(root,5);GilCopy(root,6);
            store.Apply(new BankChange(Erase:5),store.Load());
            Check(FilesOf(root,5).Length==0 && FilesOf(root,6).Length==2,"character 5's files gone, 6's kept");
            Check(!Directory.GetFiles(root,"*",SearchOption.AllDirectories).Any(path=>Holds(path,Marker)),"no marker left");
        })),
        ("Character erase 1212: the purge looks only in before-sqlite-* folders and never fails",() => InTemp(root =>
        {
            Directory.CreateDirectory(Path.Combine(root,"Notes"));File.WriteAllText(Path.Combine(root,"Notes","5.json"),"kept");
            Check(TenshoDataDatabase.PurgeSetAside(root,5)==0 && File.Exists(Path.Combine(root,"Notes","5.json")),"a file outside the copies is never removed");
            Check(TenshoDataDatabase.PurgeSetAside(Path.Combine(root,"missing"),5)==0 && TenshoDataDatabase.PurgeSetAside("",5)==0,"a missing folder: nothing");
            var aside=Path.Combine(root,"before-sqlite-20261110-120000","InventoryBank");Directory.CreateDirectory(aside);
            foreach(var name in new[]{"5.json","5.json.bak","55.json","15.json"})File.WriteAllText(Path.Combine(aside,name),"x");
            Check(TenshoDataDatabase.PurgeSetAside(root,5)==2 && File.Exists(Path.Combine(aside,"55.json")) && File.Exists(Path.Combine(aside,"15.json")),"5.json and 5.json.bak only");
        })),
    ];
}
