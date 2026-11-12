using TenshoAssistant.Discoveries;

// X-93 part 2 (0.1040): Load, Merge and Snapshot work by slices of DiscoveryStore.Slice facts and let go of the lock between them (built off the lock,
// put in by slices), and the save copies the base off the frame: results unchanged, sightings during a copy never lost.
internal static class DiscoveriesSlices1040Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,1,12,0,0,TimeSpan.Zero);
    private static string Source(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    private static DiscoveryFact Fact(uint subject,int seen=1)=>new()
    {
        Kind=DiscoveryKind.FestivalSeen,Subject=subject,Detail=1,LevelMin=5,LevelMax=5,Seen=seen,First=At,Last=At,
    };
    private static int Count(DiscoveryStore store)=>store.Summary().Sum(row=>row.Total);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries slices 1040: a base of more than two slices loads whole, not marked to save",() =>
        {
            var count=DiscoveryStore.Slice*2+7;
            var store=new DiscoveryStore();
            store.Load(new DiscoveryFile{Facts=Enumerable.Range(1,count).Select(subject=>Fact((uint)subject)).ToList()});
            Check(Count(store)==count,$"loaded {Count(store)} of {count}");
            Check(!store.Dirty,"a clean base is not saved again");
        }),
        ("Discoveries slices 1040: Load still joins facts of one key across slices and marks refused ones to save",() =>
        {
            var store=new DiscoveryStore();
            var facts=Enumerable.Range(1,DiscoveryStore.Slice+3).Select(subject=>Fact((uint)subject)).ToList();
            facts.Add(Fact(1));
            store.Load(new DiscoveryFile{Facts=facts});
            Check(Count(store)==DiscoveryStore.Slice+3 && store.Dirty,"joined across slices, written again smaller");
            Check(store.Find(DiscoveryKind.FestivalSeen,1u,5).Single().Seen==2,"the two sightings added");
            var refused=new DiscoveryStore();
            refused.Load(new DiscoveryFile{Facts=[Fact(1),Fact(0)]});
            Check(Count(refused)==1 && refused.Dirty,"a refused fact is gone at the next save");
        }),
        ("Discoveries slices 1040: Merge across slices counts added, joined and refused as before",() =>
        {
            var store=new DiscoveryStore();
            store.Load(new DiscoveryFile{Facts=[Fact(1)]});
            var imported=Enumerable.Range(2,DiscoveryStore.Slice*2).Select(subject=>Fact((uint)subject)).ToList();
            imported.Add(Fact(1));imported.Add(Fact(0));imported.Add(Fact(2));
            var (added,joined,refused)=store.Merge(imported);
            Check(added==DiscoveryStore.Slice*2 && joined==2 && refused==1,$"added {added}, joined {joined}, refused {refused}");
            Check(store.Dirty && Count(store)==DiscoveryStore.Slice*2+1,"merged");
            var two=store.Find(DiscoveryKind.FestivalSeen,2u,5).Single();
            Check(two.Seen==0 && two.Imported==2,"an imported fact twice in the file is added once, then joined");
        }),
        ("Discoveries slices 1040: Snapshot copies every fact by slices, copies apart from the base",() =>
        {
            var store=new DiscoveryStore();
            store.Load(new DiscoveryFile{Facts=Enumerable.Range(1,DiscoveryStore.Slice*2+1).Select(subject=>Fact((uint)subject)).ToList()});
            store.SeeSource(DiscoveryKind.FestivalSeen,1,1,6,At.AddDays(1));
            Check(store.Dirty,"changed");
            var file=store.SnapshotToSave(At.AddDays(2),out var stamp);
            Check(file.Facts.Count==DiscoveryStore.Slice*2+1 && store.Dirty,"whole copy, not marked before it is written (X-142)");
            store.MarkSaved(stamp);
            Check(!store.Dirty,"marked written once saved");
            file.Facts[0].Seen=99;
            Check(store.Find(DiscoveryKind.FestivalSeen,file.Facts[0].Subject,5).Single().Seen!=99,"a copy, not the base's fact");
            store.Snapshot(At.AddDays(2),false);
            store.SeeSource(DiscoveryKind.FestivalSeen,DiscoveryStore.Slice*3,1,6,At.AddDays(1));
            Check(store.Dirty,"an export leaves the mark; a new sighting sets it");
        }),
        ("Discoveries slices 1040: sightings during a copy are never lost (in it, or marked for the next save)",() =>
        {
            var store=new DiscoveryStore();
            store.Load(new DiscoveryFile{Facts=Enumerable.Range(1,DiscoveryStore.Slice*4).Select(subject=>Fact((uint)subject)).ToList()});
            const int seen=3_000;
            var first=(uint)(DiscoveryStore.Slice*4+1);
            var sighting=Task.Run(()=>{for(var n=0;n<seen;n++)store.SeeSource(DiscoveryKind.FestivalSeen,first+(uint)n,1,6,At);});
            var copies=0;
            while(!sighting.IsCompleted){store.Snapshot(At);copies++;}
            sighting.Wait();
            var last=store.Snapshot(At);
            Check(last.Facts.Count==DiscoveryStore.Slice*4+seen && last.Facts.Select(fact=>fact.Key).Distinct().Count()==last.Facts.Count,$"all {seen} sightings kept, {copies} copies during them");
        }),
        ("Discoveries slices 1040: wiring, the save copies off the frame and nothing holds the lock for a whole file",() =>
        {
            var page=Source("MainWindow.Discoveries.cs");
            var save=page[page.IndexOf("private void SaveDiscoveries(bool now)",StringComparison.Ordinal)..];
            save=save[..save.IndexOf("if(now)Write();else _=Task.Run(Write);",StringComparison.Ordinal)];
            var write=save.IndexOf("void Write()",StringComparison.Ordinal);
            var copy=save.IndexOf("store.SnapshotToSave(DateTimeOffset.UtcNow,",StringComparison.Ordinal);
            Check(write>0 && copy>write,"Snapshot inside Write, off the frame");
            var model=Source("Discoveries/DiscoveriesModel.cs");
            Check(!model.Contains("LoadLocked",StringComparison.Ordinal) && !model.Contains("facts.Values.Select(fact=>fact.Copy())",StringComparison.Ordinal),"no whole-file hold left");
            Check(model.Contains("if(prepared is not null && sharedWaiting.Contains(kind))Apply(prepared,false);",StringComparison.Ordinal),"a shared kind is never loaded twice");
        }),
    ];
}
