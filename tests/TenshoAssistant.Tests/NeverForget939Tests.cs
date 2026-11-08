using System.Numerics;
using TenshoAssistant.Discoveries;

// X-93 (0.939, user: never forget a discovery): own sightings never refused, the bound only on shared facts, a shared fact left out is counted.
internal static class NeverForget939Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Never forget 939: yours kept past the bound, shared left out and counted, nothing removed",() =>
        {
            var at=new DateTimeOffset(2026,11,8,0,0,0,TimeSpan.Zero);
            DiscoveryFact Shared(uint subject)=>new(){Kind=DiscoveryKind.Desynthesis,Subject=subject,Detail=1,LevelMin=1,LevelMax=1,Seen=1,First=at,Last=at};
            var store=new DiscoveryStore();
            var full=Enumerable.Range(1,DiscoveryStore.MaxFacts).Select(index=>Shared((uint)index)).ToArray();
            var (added,_,_)=store.Merge(full);
            Check(added==DiscoveryStore.MaxFacts && store.LeftOut==0,"shared part filled");
            var (more,_,_)=store.Merge([Shared(999_999_000)]);
            Check(more==0 && store.LeftOut==1,"shared left out, counted");
            Check(store.SeeSource(DiscoveryKind.FishCatch,5,1,1,at,1,1,new Vector3(1,0,1)),"yours kept past the bound");
            Check(store.Find(DiscoveryKind.Desynthesis,1u,1).Count==1 && store.Find(DiscoveryKind.FishCatch,5u,1).Count==1,"nothing removed");
            var reloaded=new DiscoveryStore();reloaded.Load(store.Snapshot(at));
            Check(reloaded.LeftOut==0 && reloaded.Find(DiscoveryKind.FishCatch,5u,1).Count==1,"reload keeps all");
        }),
    ];
}
