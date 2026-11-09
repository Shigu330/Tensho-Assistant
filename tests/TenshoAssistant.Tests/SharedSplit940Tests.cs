using System.Numerics;
using TenshoAssistant.Discoveries;

// X-93 (0.940): yours in the main file, the shared facts apart by kind (gzip); read back whole; unchanged kinds not rewritten; emptied kinds removed.
internal static class SharedSplit940Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
    ];
}
