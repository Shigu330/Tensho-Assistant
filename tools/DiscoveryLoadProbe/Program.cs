using System.Diagnostics;
using System.Numerics;
using TenshoAssistant.Discoveries;

// X-93 (0.938): how the discoveries base behaves with many facts. Invented MonsterPlace / FishCatch / MonsterDrop facts are loaded, written, read
// again and searched; times, memory and file size are printed. Usage: dotnet run -c Release -- 100000 200000 1000000
var sizes=args.Length>0 ? args.Select(int.Parse).ToArray():[100_000,200_000,1_000_000];
var folder=Path.Combine(Path.GetTempPath(),"tensho-load-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
try
{
    foreach(var size in sizes)
    {
        var at=new DateTimeOffset(2026,11,1,0,0,0,TimeSpan.Zero);
        var file=new DiscoveryFile{Written=at,Facts=new List<DiscoveryFact>(size)};
        for(var index=0;index<size;index++)
        {
            var kind=(index%3) switch{0=>DiscoveryKind.MonsterPlace,1=>DiscoveryKind.FishCatch,_=>DiscoveryKind.MonsterDrop};
            var x=(index%400)*50f+10;var z=(index/400%400)*50f-9_000;
            var placed=DiscoveryStore.HasPlace(kind);
            file.Facts.Add(new DiscoveryFact{Kind=kind,Subject=(uint)(index%5000+1),Detail=(uint)(index%97+1),Territory=(uint)(index%300+1),Map=placed ? (uint)(index%300+1):0,
                CellX=placed ? DiscoveryStore.Cell(x,z).X:0,CellZ=placed ? DiscoveryStore.Cell(x,z).Z:0,X=placed ? x:0,Z=placed ? z:0,LevelMin=10,LevelMax=12,Seen=1,First=at,Last=at});
        }
        GC.Collect();var before=GC.GetTotalMemory(true);
        var watch=Stopwatch.StartNew();
        var store=new DiscoveryStore();store.Load(file);var load=watch.ElapsedMilliseconds;
        var after=GC.GetTotalMemory(true);
        watch.Restart();var snapshot=store.Snapshot(at);var snap=watch.ElapsedMilliseconds;
        var path=Path.Combine(folder,$"Discoveries-{size}.json");
        watch.Restart();DiscoveryFiles.Write(path,snapshot);var write=watch.ElapsedMilliseconds;
        watch.Restart();var read=DiscoveryFiles.Read(path);var readMs=watch.ElapsedMilliseconds;
        watch.Restart();for(var query=0;query<100;query++)store.Find(DiscoveryKind.MonsterPlace,subject=>subject==(uint)(query*7+1),8);var find=watch.ElapsedMilliseconds/100.0;
        watch.Restart();var summary=store.Summary();var sum=watch.ElapsedMilliseconds;
        Console.WriteLine($"SIZE {size}: kept {summary.Sum(row=>row.Total)} (cap {DiscoveryStore.MaxFacts}), load {load} ms, memory +{(after-before)/1_048_576.0:0.0} MB, snapshot {snap} ms, write {write} ms, file {new FileInfo(path).Length/1_048_576.0:0.0} MB, read {readMs} ms ({read?.Facts.Count} facts), one Find {find:0.00} ms, Summary {sum} ms");
    }
}
finally{Directory.Delete(folder,true);}
