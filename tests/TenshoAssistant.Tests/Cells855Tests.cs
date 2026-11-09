// 0.855 (X-72 B, user choice): 50-yalm cells; older facts moved and joined.
using TenshoAssistant.Discoveries;
internal static class Cells855Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static DiscoveryFact Old(float x,int seen,int day)=>new(){Kind=DiscoveryKind.MonsterPlace,Subject=113,Detail=5099,Territory=399,Map=213,X=x,Z=5,
        CellX=(int)MathF.Floor(x/10),CellZ=0,LevelMin=58,LevelMax=58,Seen=seen,First=new DateTimeOffset(2026,11,day,0,0,0,TimeSpan.Zero),Last=new DateTimeOffset(2026,11,day,1,0,0,TimeSpan.Zero)};
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Cells: older 10-yalm facts load into 50-yalm cells, joined, and the base is marked to write again",() =>
        {
            var store=new DiscoveryStore();
            store.Load(new DiscoveryFile{Facts=[Old(12,2,7),Old(31,1,6),Old(71,1,7)]});
            var all=store.Snapshot(DateTimeOffset.UtcNow,saving:false).Facts;
            Check(all.Count==2,"12 and 31 share the 0-50 cell; 71 in the next");
            var joined=all.Single(fact=>fact.CellX==0);
            Check(joined.Seen==3 && joined.X==31 && joined.First.Day==6 && joined.Last.Day==7,"sightings added, first place kept");
            Check(store.Dirty,"written again");
        }),
        ("Cells: an imported older file is moved too, not refused",() =>
        {
            var store=new DiscoveryStore();
            var result=store.Merge([Old(12,2,7)]);
            Check(result.Added==1 && result.Refused==0,"imported");
        }),
    ];
}
