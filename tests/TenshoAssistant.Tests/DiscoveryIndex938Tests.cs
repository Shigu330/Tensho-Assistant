using System.Numerics;
using TenshoAssistant.Discoveries;

// X-93 (0.938): the discoveries base searches through an index by kind and subject; results are the same as before, cleared with the base.
internal static class DiscoveryIndex938Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery index 938: by subject and by predicate, most seen first, cleared",() =>
        {
            var store=new DiscoveryStore();var at=new DateTimeOffset(2026,11,8,0,0,0,TimeSpan.Zero);
            store.SeeSource(DiscoveryKind.FishCatch,10,1,1,at,1,1,new Vector3(5,0,5));
            store.SeeSource(DiscoveryKind.FishCatch,10,2,1,at,1,1,new Vector3(5,0,5));
            store.SeeSource(DiscoveryKind.FishCatch,10,2,1,at.AddHours(1),1,1,new Vector3(5,0,5));
            store.SeeSource(DiscoveryKind.FishCatch,11,1,1,at,1,1,new Vector3(5,0,5));
            store.SeeSource(DiscoveryKind.Desynthesis,10,3,1,at);
            var ten=store.Find(DiscoveryKind.FishCatch,10u,8);
            Check(ten.Count==2 && ten.All(fact=>fact.Subject==10),"one subject: "+ten.Count);
            Check(store.Find(DiscoveryKind.FishCatch,subject=>subject>=10,8).Count==3 && store.Find(DiscoveryKind.FishCatch,99u,8).Count==0,"predicate, unknown");
            Check(store.Find(DiscoveryKind.FishCatch,10u,1).Count==1,"max");
            store.Clear();
            Check(store.Find(DiscoveryKind.FishCatch,10u,8).Count==0 && store.Find(DiscoveryKind.Desynthesis,_=>true,8).Count==0,"cleared");
            Check(File.Exists(Path.GetFullPath("tools/DiscoveryLoadProbe/Program.cs")),"load test tool kept");
        }),
    ];
}
