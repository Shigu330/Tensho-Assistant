using System.Numerics;
using TenshoAssistant.Discoveries;

// 0.1133 (X-166, GPT audit of 12/11, R01): an imported discovery that becomes yours is never lost by a stop between the two writes of a save
// (your file is written first now); an import is marked done (file renamed, shared file's hash noted) only once a save holds it. Behaviour
// tests (R28): real files in a temporary folder, a stop simulated between the two writes, then a restart read from disk.
internal static class DiscoveriesCrash1133Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,12,4,0,0,TimeSpan.Zero);
    private sealed class Stop : Exception { }

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-crash-1133-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    // A restart: the main file read, the shared kinds read from disk the way the plugin does (UseSharedSource), all of them asked for.
    private static DiscoveryStore Restart(string main)
    {
        var store=new DiscoveryStore();
        if(DiscoveryFiles.Read(main) is { } file)store.Load(file);
        store.UseSharedSource(kind=>DiscoveryFiles.ReadSharedKind(main,kind,(_,error)=>throw error),DiscoveryFiles.SharedKindsOnDisk(main));
        store.WantAll();
        for(var wait=0;store.SharedKindsWaiting>0 && wait<500;wait++)Thread.Sleep(10);
        Check(store.SharedKindsWaiting==0,"the shared kinds are read");
        return store;
    }
    private static void Save(DiscoveryStore store,string main,Dictionary<DiscoveryKind,string> written,Action? between=null)
    {
        var file=store.SnapshotToSave(At,out var stamp);
        DiscoveryFiles.WriteSplit(main,file,written,store.SharedKindsHeld(),between);
        store.MarkSaved(stamp);
    }
    private static readonly Vector3 Place=new(120,0,-40);
    private static DiscoveryFact Shared(uint subject)=>new(){Kind=DiscoveryKind.FishCatch,Subject=subject,Detail=1,Territory=135,Map=16,
        CellX=DiscoveryStore.Cell(Place.X,Place.Z).X,CellZ=DiscoveryStore.Cell(Place.X,Place.Z).Z,X=Place.X,Z=Place.Z,LevelMin=1,LevelMax=1,Seen=1,First=At.AddDays(-3),Last=At.AddDays(-2)};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries 1133: a stop between the two writes of a save never loses an imported fact that became yours; read back once, imports not doubled",() => InTemp(root =>
        {
            var main=Path.Combine(root,"Discoveries.json");var written=new Dictionary<DiscoveryKind,string>();
            var first=new DiscoveryStore();
            first.Merge([Shared(4869),Shared(4870)]);
            Save(first,main,written);
            Check(DiscoveryFiles.Read(main)!.Facts.Count==0 && DiscoveryFiles.ReadSharedKind(main,DiscoveryKind.FishCatch,(_,e)=>throw e)!.Facts.Count==2,"both imported, in the shared file");
            // The next session: the fish is caught by the player, so the imported fact becomes yours; the save stops between its two writes.
            var second=Restart(main);
            second.SeeSource(DiscoveryKind.FishCatch,4869,1,1,At,135,16,Place);
            var stopped=false;
            try{Save(second,main,[],()=>throw new Stop());}catch(Stop){stopped=true;}
            Check(stopped && second.Dirty,"the save stopped between the two files and is still to do");
            // Restart from disk: the fact is there (yours), once; its import counted once, not twice; the other shared fact is kept.
            var third=Restart(main);
            var mine=third.Find(DiscoveryKind.FishCatch,4869u,5);
            Check(mine.Count==1 && mine[0].Seen==1 && mine[0].Imported==1,$"one readable copy, yours, its import not doubled (count {mine.Count}, seen {mine.FirstOrDefault()?.Seen}, imported {mine.FirstOrDefault()?.Imported})");
            Check(third.Find(DiscoveryKind.FishCatch,4870u,5).Single().Seen==0,"the other shared fact kept");
            Check(third.Dirty,"the old shared copy left out is to be removed at the next save");
            // The next full save writes the shared file without the old copy.
            Save(third,main,[]);
            var shared=DiscoveryFiles.ReadSharedKind(main,DiscoveryKind.FishCatch,(_,e)=>throw e)!;
            Check(shared.Facts.Count==1 && shared.Facts[0].Subject==4870 && DiscoveryFiles.Read(main)!.Facts.Single().Subject==4869,"each fact in one file after a full save");
            var fourth=Restart(main);
            Check(fourth.Find(DiscoveryKind.FishCatch,4869u,5).Single() is { Seen:1, Imported:1 } && !fourth.Dirty,"read back the same, nothing to save");
        })),
        ("Discoveries 1133: a write of your file that fails leaves the shared file as it was: the fact still readable there",() => InTemp(root =>
        {
            var main=Path.Combine(root,"Discoveries.json");var written=new Dictionary<DiscoveryKind,string>();
            var first=new DiscoveryStore();first.Merge([Shared(4869)]);Save(first,main,written);
            var second=Restart(main);
            second.SeeSource(DiscoveryKind.FishCatch,4869,1,1,At,135,16,Place);
            File.Delete(main);Directory.CreateDirectory(main);// your file cannot be written: a folder stands where it goes
            var failed=false;
            try{Save(second,main,[]);}catch(Exception error) when(error is IOException or UnauthorizedAccessException){failed=true;}
            Directory.Delete(main);
            Check(failed && second.Dirty,"the save failed and is still to do");
            Check(DiscoveryFiles.ReadSharedKind(main,DiscoveryKind.FishCatch,(_,e)=>throw e)!.Facts.Single().Subject==4869,"the shared file was not touched");
            Check(Restart(main).Find(DiscoveryKind.FishCatch,4869u,5).Count==1,"the fact is read back after a restart");
        })),
        ("Discoveries 1133: an import is marked done only once a save holds it; a stop before the save leaves it to take again",() => InTemp(root =>
        {
            var main=Path.Combine(root,"Discoveries.json");var import=Path.Combine(root,"Import");
            var source=new DiscoveryStore();source.SeeSource(DiscoveryKind.FishCatch,4869,1,1,At,135,16,Place);
            DiscoveryFiles.Write(Path.Combine(import,"a.json"),source.Snapshot(At));
            // A session imports the file, then stops before any save: nothing on disk, the file is not renamed.
            var stopped=new DiscoveryStore();
            Check(DiscoveryFiles.Import(import,stopped).Files==1 && DiscoveryFiles.Import(import,stopped).Files==0,"read once in the session");
            Check(File.Exists(Path.Combine(import,"a.json")) && !File.Exists(Path.Combine(import,"a.json.imported")),"not renamed before a save");
            // The next session takes it again, saves, and only then the file is renamed.
            var next=Restart(main);
            Check(DiscoveryFiles.Import(import,next).Files==1,"taken again after the stop");
            Save(next,main,[]);
            Check(File.Exists(Path.Combine(import,"a.json.imported")) && !File.Exists(Path.Combine(import,"a.json")),"renamed once saved");
            Check(Restart(main).Find(DiscoveryKind.FishCatch,4869u,5).Single() is { Seen:0, Imported:1 },"the import is on disk, once");
            // The shared file's hash (GitHub exchange): the same rule, by key.
            var marks=0;var store=new DiscoveryStore();
            store.Merge([Shared(4870)]);
            store.WhenImportSaved("common:abc",()=>marks++);
            Check(marks==0 && store.ImportWaiting("common:abc"),"the hash waits for the save");
            var copy=store.SnapshotToSave(At,out var stamp);
            store.SeeSource(DiscoveryKind.FishCatch,1,1,1,At,135,16,Place);
            store.MarkSaved(stamp-1);
            Check(marks==0,"an older save does not hold it");
            store.MarkSaved(stamp);
            Check(marks==1 && !store.ImportWaiting("common:abc"),"noted once the save that holds it is written");
            var clean=new DiscoveryStore();clean.WhenImportSaved("x",()=>marks++);
            Check(marks==2,"nothing to save: done at once");
            // A base cleared on purpose and saved marks the waiting import done: a deleted import is not brought back (audit 0.949).
            var cleared=new DiscoveryStore();cleared.Merge([Shared(4871)]);cleared.WhenImportSaved("common:def",()=>marks++);
            cleared.Clear();var empty=cleared.SnapshotToSave(At,out var clearedStamp);cleared.MarkSaved(clearedStamp);
            Check(marks==3,"cleared and saved: marked done");
        })),
        ("Discoveries 1133: the wiring: your file before the shared ones, the GitHub hash and the import file marked after the save",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
            var model=Read("Discoveries/DiscoveriesModel.cs");
            var split=model[model.IndexOf("public static void WriteSplit(",StringComparison.Ordinal)..];
            Check(split.IndexOf("Write(mainPath,",StringComparison.Ordinal)<split.IndexOf("foreach(var (kind,facts) in byKind)",StringComparison.Ordinal),"your file first");
            var exchange=Read("Discoveries/GitHubExchange.cs");
            Check(exchange.Contains("store.WhenImportSaved(\"common:\"+hash,") && !exchange.Contains("File.WriteAllText(CommonMarker,hash)"),"the hash noted after the save");
            Check(model.Contains("store.WhenImportSaved(key,()=>File.Move(path,path+\".imported\",true));"),"the import file renamed after the save");
        }),
    ];
}
