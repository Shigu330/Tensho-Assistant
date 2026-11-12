using System.Numerics;
using Microsoft.Data.Sqlite;
using TenshoAssistant.Discoveries;

// 0.1136 (X-172, user's choice of 12/11, trial docs/evidence/x172-sqlite-trial.md): the discoveries in Discoveries.db (SQLite), written row by row;
// the files of before converted once, kept aside. Behaviour tests (R28): real databases in a temporary folder, a write that fails on a read-only
// file, a save left uncommitted, a reader on another connection while the game writes, the layout of a newer version refused.
internal static class DiscoveriesSqlite1136Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,10,8,0,0,TimeSpan.Zero);
    private static readonly Vector3 Place=new(120,0,-40);

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-sqlite-1136-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{SqliteConnection.ClearAllPools();try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    private static DiscoveryFact Shared(uint subject)=>new(){Kind=DiscoveryKind.FishCatch,Subject=subject,Detail=1,Territory=135,Map=16,
        CellX=DiscoveryStore.Cell(Place.X,Place.Z).X,CellZ=DiscoveryStore.Cell(Place.X,Place.Z).Z,X=Place.X,Z=Place.Z,LevelMin=1,LevelMax=1,Seen=1,First=At.AddDays(-3),Last=At.AddDays(-2)};
    private static DiscoveryStore Restart(DiscoveryDatabase database)
    {
        var store=new DiscoveryStore();
        store.Load(database.ReadMine());
        store.UseSharedSource(kind=>database.ReadShared(kind),database.SharedKinds());
        store.WantAll();
        for(var wait=0;store.SharedKindsWaiting>0 && wait<500;wait++)Thread.Sleep(10);
        return store;
    }
    private static void Save(DiscoveryStore store,DiscoveryDatabase database)
    {
        var changes=store.ChangesToSave(out var stamp);
        try{database.Write(changes.Write,changes.Remove,changes.All);}
        catch{store.SaveFailed(changes);throw;}
        store.MarkSaved(stamp);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries 1136: the files of before are converted once into Discoveries.db and kept aside; the same facts read back",() => InTemp(root =>
        {
            var main=Path.Combine(root,"Discoveries.json");
            var store=new DiscoveryStore();
            store.Merge([Shared(4869),Shared(4870)]);
            store.SeeSource(DiscoveryKind.FishCatch,4869,1,3,At,135,16,Place);
            store.SeeMonster(10,20,135,16,new Vector3(5,0,5),12,At);
            DiscoveryFiles.WriteSplit(main,store.Snapshot(At),[]);
            var aside=DiscoveryDatabase.ConvertFiles(root,At,(_,error)=>throw error);
            Check(aside is not null && File.Exists(Path.Combine(aside,"Discoveries.json")) && File.Exists(DiscoveryFiles.SharedPath(Path.Combine(aside,"Shared"),DiscoveryKind.FishCatch)),"the old files kept aside, whole");
            Check(!File.Exists(main) && !Directory.Exists(DiscoveryFiles.SharedFolder(main)),"nothing left in their place");
            var database=new DiscoveryDatabase(Path.Combine(root,DiscoveryDatabase.FileName));
            Check(database.Count()==(2,1),"two of yours (the fish became yours), one shared");
            var back=Restart(database);
            Check(back.Find(DiscoveryKind.FishCatch,4869u,5).Single() is { Seen:1, Imported:1, Own:{ LevelMin:3 } } && back.Find(DiscoveryKind.FishCatch,4870u,5).Single().Seen==0,"read back with its provenance");
            Check(!back.Dirty,"nothing to save after reading");
            Check(DiscoveryDatabase.ConvertFiles(root,At.AddMinutes(1),(_,error)=>throw error) is null && database.Count()==(2,1),"never converted twice");
        })),
        ("Discoveries 1136: a save writes the changed rows only, in one transaction; a write that fails is taken again; the base cleared on purpose empties it",() => InTemp(root =>
        {
            var path=Path.Combine(root,DiscoveryDatabase.FileName);
            var database=new DiscoveryDatabase(path);
            var store=new DiscoveryStore();
            for(uint subject=1;subject<=50;subject++)store.SeeSource(DiscoveryKind.Desynthesis,subject,1,1,At);
            Check(store.ChangesToSave(out _).Write.Count==50,"the first save writes every new fact");
            store=new DiscoveryStore();
            for(uint subject=1;subject<=50;subject++)store.SeeSource(DiscoveryKind.Desynthesis,subject,1,1,At);
            Save(store,database);
            Check(!store.Dirty && database.Count()==(50,0),"saved");
            store.SeeSource(DiscoveryKind.Desynthesis,7,1,2,At.AddHours(1));
            var one=store.ChangesToSave(out var stamp);
            Check(one.Write.Count==1 && one.Remove.Count==0 && !one.All,"one change, one row");
            // The file read-only: the write fails and the change is taken again.
            File.SetAttributes(path,FileAttributes.ReadOnly);
            var failed=false;
            try{database.Write(one.Write,one.Remove,one.All);}catch(SqliteException){failed=true;store.SaveFailed(one);}
            File.SetAttributes(path,FileAttributes.Normal);
            var again=store.ChangesToSave(out stamp);
            Check(failed && store.Dirty && again.Write.Single().Subject==7,"failed: taken again by the next save");
            store.SaveFailed(again);
            Save(store,database);
            Check(Restart(database).Find(DiscoveryKind.Desynthesis,7u,1).Single().Seen==2,"the change is in the file");
            // A save left uncommitted (a stop before the end of the transaction) leaves the last save whole.
            using(var open=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Pooling=false}.ToString()))
            {
                open.Open();using var transaction=open.BeginTransaction();using var command=open.CreateCommand();command.Transaction=transaction;
                command.CommandText="DELETE FROM facts;";command.ExecuteNonQuery();
            }
            Check(database.Count()==(50,0),"a stop before the commit changes nothing");
            store.Clear();store.SeeSource(DiscoveryKind.Venture,9,1,1,At);
            Save(store,database);
            Check(database.Count()==(1,0),"cleared on purpose: only what came after");
        })),
        ("Discoveries 1136: a change in a shared kind not read yet waits for it and is joined first; an import marked once its save is in the file",() => InTemp(root =>
        {
            var database=new DiscoveryDatabase(Path.Combine(root,DiscoveryDatabase.FileName));
            var first=new DiscoveryStore();first.Merge([Shared(4869)]);Save(first,database);
            var second=new DiscoveryStore();second.Load(database.ReadMine());
            second.UseSharedSource(kind=>database.ReadShared(kind),database.SharedKinds());
            second.SeeSource(DiscoveryKind.FishCatch,4869,1,5,At,135,16,Place);
            var early=second.ChangesToSave(out _);
            Check(early.Write.Count==0 && second.Dirty,"waits for its kind (the row holds the shared copy)");
            for(var wait=0;second.SharedKindsWaiting>0 && wait<500;wait++)Thread.Sleep(10);
            Save(second,database);
            Check(Restart(database).Find(DiscoveryKind.FishCatch,4869u,5).Single() is { Seen:1, Imported:1 },"joined then written: the import is kept");
            var marks=0;
            second.Merge([Shared(4870)]);second.WhenImportSaved("common:x",()=>marks++);
            Check(marks==0,"not before the save");
            Save(second,database);
            Check(marks==1 && database.Count().Shared==1,"marked once in the file");
        })),
        ("Discoveries 1136: another reader (the desktop application) reads while the game writes; a newer layout is refused",() => InTemp(root =>
        {
            var path=Path.Combine(root,DiscoveryDatabase.FileName);
            var writer=new DiscoveryDatabase(path);var reader=new DiscoveryDatabase(path);
            var store=new DiscoveryStore();var errors=0;var reads=0;
            var stop=DateTime.UtcNow.AddSeconds(2);
            var reading=Task.Run(()=>{while(DateTime.UtcNow<stop){try{_=reader.Count();reads++;}catch(SqliteException){errors++;}}});
            for(uint round=0;DateTime.UtcNow<stop;round++){for(uint n=0;n<50;n++)store.SeeSource(DiscoveryKind.Desynthesis,round*50+n+1,1,1,At);Save(store,writer);}
            reading.Wait();
            Check(reads>10 && errors==0,$"reads {reads}, errors {errors}");
            using(var open=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Pooling=false}.ToString()))
            {open.Open();using var command=open.CreateCommand();command.CommandText="PRAGMA user_version=2;";command.ExecuteNonQuery();}
            var refused=false;
            try{_=new DiscoveryDatabase(path);}catch(InvalidDataException){refused=true;}
            Check(refused,"a newer layout is refused, never written over");
        })),
    ];
}
