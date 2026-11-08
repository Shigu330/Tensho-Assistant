using System.Numerics;
using TenshoAssistant.Discoveries;

// X-93 (0.941): shared kinds read on demand; a save leaves the kinds not read alone; an import reads its kinds first and joins, never doubles.
internal static class SharedLazy941Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
                store.Clear();
                DiscoveryFiles.WriteSplit(main,store.Snapshot(at),written,store.SharedKindsHeld());
                Check(DiscoveryFiles.SharedKindsOnDisk(main).Count==0,"cleared base removes the shared files");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }),
    ];
}
