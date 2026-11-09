// Test audit of 2026-11-09, batch 2 (0.1021): SharedSplit940Tests, SharedLazy941Tests merged into SharedDiscoveriesTests; every case is kept.
using System.Numerics;
using TenshoAssistant.Discoveries;
internal static class SharedDiscoveriesTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From SharedSplit940Tests: X-93 (0.940): yours in the main file, the shared facts apart by kind (gzip); read back whole; unchanged kinds not rewritten; emptied kinds removed.
        ("Shared split 940: main file yours only, kind files compressed, read back, rewritten only when changed, removed when empty, broken set aside",() =>
        {
            var folder=Path.Combine(Path.GetTempPath(),"tensho-split-"+Guid.NewGuid().ToString("N"));
            try
            {
                var at=new DateTimeOffset(2026,11,8,0,0,0,TimeSpan.Zero);var main=Path.Combine(folder,"Discoveries.json");
                var store=new DiscoveryStore();
                store.SeeSource(DiscoveryKind.FishCatch,5,1,1,at,1,1,new Vector3(1,0,1));
                store.Merge([new DiscoveryFact{Kind=DiscoveryKind.Desynthesis,Subject=7,Detail=1,LevelMin=1,LevelMax=1,Seen=1,First=at,Last=at},
                    new DiscoveryFact{Kind=DiscoveryKind.Venture,Subject=8,Detail=2,LevelMin=1,LevelMax=1,Seen=1,First=at,Last=at}]);
                var written=new Dictionary<DiscoveryKind,string>();
                DiscoveryFiles.WriteSplit(main,store.Snapshot(at),written);
                var shared=DiscoveryFiles.SharedFolder(main);
                Check(DiscoveryFiles.Read(main)!.Facts.Count==1 && File.Exists(DiscoveryFiles.SharedPath(shared,DiscoveryKind.Desynthesis)) && File.Exists(DiscoveryFiles.SharedPath(shared,DiscoveryKind.Venture)),"split");
                Check(File.ReadAllBytes(DiscoveryFiles.SharedPath(shared,DiscoveryKind.Venture))[0]==0x1F,"gzip");
                var back=new DiscoveryStore();back.Load(DiscoveryFiles.Read(main)!);
                foreach(var file in DiscoveryFiles.ReadShared(main,(_,_)=>throw new Exception("broken")))back.Load(file);
                Check(back.Find(DiscoveryKind.FishCatch,5u,1).Count==1 && back.Find(DiscoveryKind.Desynthesis,7u,1).Single().Seen==0 && back.Find(DiscoveryKind.Venture,8u,1).Count==1,"read back");
                var stamp=File.GetLastWriteTimeUtc(DiscoveryFiles.SharedPath(shared,DiscoveryKind.Venture));
                Thread.Sleep(30);
                DiscoveryFiles.WriteSplit(main,store.Snapshot(at.AddMinutes(1)),written);
                Check(File.GetLastWriteTimeUtc(DiscoveryFiles.SharedPath(shared,DiscoveryKind.Venture))==stamp,"unchanged kind not rewritten");
                store.Clear();store.SeeSource(DiscoveryKind.FishCatch,5,1,1,at,1,1,new Vector3(1,0,1));
                DiscoveryFiles.WriteSplit(main,store.Snapshot(at),written);
                Check(!File.Exists(DiscoveryFiles.SharedPath(shared,DiscoveryKind.Venture)),"emptied kind removed");
                File.WriteAllText(DiscoveryFiles.SharedPath(shared,DiscoveryKind.Reduction),"not gzip");
                var broken=0;var read=DiscoveryFiles.ReadShared(main,(_,_)=>broken++).ToArray();
                Check(broken==1 && read.Length==0 && File.Exists(DiscoveryFiles.SharedPath(shared,DiscoveryKind.Reduction)+".broken"),"broken set aside");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }),
        // From SharedLazy941Tests: X-93 (0.941): shared kinds read on demand; a save leaves the kinds not read alone; an import reads its kinds first and joins, never doubles.
        ("Shared lazy 941: read when asked, untouched when not, joined on import, cleared with the base",() =>
        {
            var folder=Path.Combine(Path.GetTempPath(),"tensho-lazy-"+Guid.NewGuid().ToString("N"));
            try
            {
                var at=new DateTimeOffset(2026,11,8,0,0,0,TimeSpan.Zero);var main=Path.Combine(folder,"Discoveries.json");
                DiscoveryFact Shared(DiscoveryKind kind,uint subject)=>new(){Kind=kind,Subject=subject,Detail=1,LevelMin=1,LevelMax=1,Seen=1,First=at,Last=at};
                var first=new DiscoveryStore();
                first.SeeSource(DiscoveryKind.FishCatch,5,1,1,at,1,1,new Vector3(1,0,1));
                first.Merge([Shared(DiscoveryKind.Desynthesis,7),Shared(DiscoveryKind.Venture,8)]);
                DiscoveryFiles.WriteSplit(main,first.Snapshot(at),[]);
                var store=new DiscoveryStore();store.Load(DiscoveryFiles.Read(main)!);
                var reads=new List<DiscoveryKind>();
                store.UseSharedSource(kind=>{lock(reads)reads.Add(kind);return DiscoveryFiles.ReadSharedKind(main,kind,(_,_)=>{});},DiscoveryFiles.SharedKindsOnDisk(main));
                Check(store.SharedKindsWaiting==2 && !store.SharedKindsHeld().Contains(DiscoveryKind.Venture),"nothing read yet");
                store.Find(DiscoveryKind.Desynthesis,7u,1);
                for(var wait=0;wait<200 && store.SharedKindsWaiting>1;wait++)Thread.Sleep(10);
                Check(store.Find(DiscoveryKind.Desynthesis,7u,1).Count==1 && !reads.Contains(DiscoveryKind.Venture),"read when asked only");
                var written=new Dictionary<DiscoveryKind,string>();
                DiscoveryFiles.WriteSplit(main,store.Snapshot(at),written,store.SharedKindsHeld());
                Check(File.Exists(DiscoveryFiles.SharedPath(DiscoveryFiles.SharedFolder(main),DiscoveryKind.Venture)),"kind not read left alone");
                var (added,joined,_)=store.Merge([Shared(DiscoveryKind.Venture,8)]);
                Check(added==0 && joined==1 && store.Find(DiscoveryKind.Venture,8u,1).Single().Imported==2,"import joins the kind read first");
                store.WantAll();
                for(var wait=0;wait<200 && store.SharedKindsWaiting>0;wait++)Thread.Sleep(10);
                Check(store.SharedKindsWaiting==0,"page reads every kind");
                store.Clear();
                DiscoveryFiles.WriteSplit(main,store.Snapshot(at),written,store.SharedKindsHeld());
                Check(DiscoveryFiles.SharedKindsOnDisk(main).Count==0,"cleared base removes the shared files");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }),
    ];
}
