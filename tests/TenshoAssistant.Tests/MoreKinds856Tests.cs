// 0.856 (X-72 C, user choice): hunt marks' spawn places and FATEs seen in the discoveries base.
using System.Numerics;
using TenshoAssistant.Discoveries;
internal static class MoreKinds856Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,8,1,0,0,TimeSpan.Zero);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("More kinds: a hunt mark keeps its rank; a FATE counts once an hour; kinds kept apart",() =>
        {
            var store=new DiscoveryStore();
            Check(store.SeeHuntMark(2953,6002,397,211,new Vector3(100,0,100),1,At),"hunt mark new");
            Check(store.SeeMonster(2953,6002,397,211,new Vector3(100,0,100),50,At),"same monster as a monster place: its own fact");
            Check(store.SeeFate(845,397,211,new Vector3(10,0,10),55,At),"FATE new");
            Check(!store.SeeFate(845,397,211,new Vector3(10,0,10),55,At.AddMinutes(30)),"still running");
            store.SeeFate(845,397,211,new Vector3(10,0,10),55,At.AddHours(2));
            var fate=store.Find(DiscoveryKind.FateSeen,_=>true,5).Single();
            Check(fate.Seen==2 && fate.LevelMax==55,"twice, two hours apart");
            Check(store.Find(DiscoveryKind.HuntMarkPlace,_=>true,5).Single().LevelMax==1,"rank B");
            var settings=new DiscoverySettings();
            Check(settings.HuntMarks && settings.Fates,"on by default");
        }),
    ];
}
