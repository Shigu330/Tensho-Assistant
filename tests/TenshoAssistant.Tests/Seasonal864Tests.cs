// 0.864 (X-73.12): festivals seen (first and last days) and vistas registered (weather, Eorzea minute).
using TenshoAssistant.Discoveries;
internal static class Seasonal864Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Seasonal: a festival seen on two days keeps both ends; day numbers stay in range",() =>
        {
            var store=new DiscoveryStore();
            var first=new DateTimeOffset(2026,12,15,0,0,0,TimeSpan.Zero);
            store.SeeSource(DiscoveryKind.FestivalSeen,90,1,ItemGains.DayNumber(first),first);
            store.SeeSource(DiscoveryKind.FestivalSeen,90,1,ItemGains.DayNumber(first.AddDays(10)),first.AddDays(10));
            var fact=store.Find(DiscoveryKind.FestivalSeen,_=>true,5).Single();
            Check(fact.LevelMax-fact.LevelMin==10 && fact.Seen==2,"ten days apart");
            Check(ItemGains.DayNumber(new DateTimeOffset(2000,1,1,0,0,0,TimeSpan.Zero))==0,"before: 0");
        }),
    ];
}
