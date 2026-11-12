using System.Numerics;
using TenshoAssistant.Discoveries;

// 0.1136 (relais 26, follow-up of X-166): a fact you see while its shared kind is not read yet (shared kinds are read when asked, X-93) is joined
// to the shared copy when the kind is read, as before 0.1133: its imports are kept. Only a fact of yours that already holds an import (it came from
// that copy, the stop between the two writes of a save) leaves the old shared copy out. Behaviour test (R28): real files, a restart, a sighting
// before the kind is read.
internal static class DiscoveriesLazy1136Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,10,6,0,0,TimeSpan.Zero);
    private static readonly Vector3 Place=new(120,0,-40);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries 1136: a sighting made before its shared kind is read is joined to the shared copy, imports kept, after a save and a restart too",() =>
        {
            var root=Path.Combine(Path.GetTempPath(),"tensho-lazy-1136-"+Guid.NewGuid().ToString("N"));
            try
            {
                var main=Path.Combine(root,"Discoveries.json");
                var first=new DiscoveryStore();
                var (cellX,cellZ)=DiscoveryStore.Cell(Place.X,Place.Z);
                first.Merge([new DiscoveryFact{Kind=DiscoveryKind.FishCatch,Subject=4869,Detail=1,Territory=135,Map=16,CellX=cellX,CellZ=cellZ,X=Place.X,Z=Place.Z,
                    LevelMin=2,LevelMax=2,Seen=3,First=At.AddDays(-3),Last=At.AddDays(-2),Values=new(){[2]=3}}]);
                DiscoveryFiles.WriteSplit(main,first.Snapshot(At),[]);
                // Restart: the shared kind waits on disk; a fish is caught before any page asks for it.
                var second=new DiscoveryStore();
                second.UseSharedSource(kind=>DiscoveryFiles.ReadSharedKind(main,kind,(_,error)=>throw error),DiscoveryFiles.SharedKindsOnDisk(main));
                Check(second.SharedKindsWaiting==1,"the shared kind is not read yet");
                second.SeeSource(DiscoveryKind.FishCatch,4869,1,5,At,135,16,Place);
                second.WantAll();
                for(var wait=0;second.SharedKindsWaiting>0 && wait<500;wait++)Thread.Sleep(10);
                var joined=second.Find(DiscoveryKind.FishCatch,4869u,5);
                Check(joined.Count==1 && joined[0] is { Seen:1, Imported:1, LevelMin:2, LevelMax:5 },$"joined, imports kept (seen {joined.FirstOrDefault()?.Seen}, imported {joined.FirstOrDefault()?.Imported})");
                Check(joined[0].Own is { LevelMin:5, LevelMax:5 },"your part kept apart (X-167)");
                var file=second.SnapshotToSave(At,out var stamp);
                DiscoveryFiles.WriteSplit(main,file,[],second.SharedKindsHeld());second.MarkSaved(stamp);
                var third=new DiscoveryStore();third.Load(DiscoveryFiles.Read(main)!);
                third.UseSharedSource(kind=>DiscoveryFiles.ReadSharedKind(main,kind,(_,error)=>throw error),DiscoveryFiles.SharedKindsOnDisk(main));
                third.WantAll();
                for(var wait=0;third.SharedKindsWaiting>0 && wait<500;wait++)Thread.Sleep(10);
                Check(third.Find(DiscoveryKind.FishCatch,4869u,5).Single() is { Seen:1, Imported:1 },"read back the same after a restart");
                Check(CommunityDiscoveries.Mine(third.Snapshot(At)).Facts.Single() is { LevelMin:5, LevelMax:5 },"only your value is sent");
            }
            finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        }),
    ];
}
