// 0.860 (X-74, the night's parsers): where the items got come from.
using System.Numerics;
using TenshoAssistant.Discoveries;
internal static class ItemSources860Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,8,3,0,0,TimeSpan.Zero);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Item gains: gains, losses, one item spent",() =>
        {
            var before=new Dictionary<uint,long>{[1]=5,[2]=1};var after=new Dictionary<uint,long>{[1]=5,[3]=2};
            Check(ItemGains.Gains(before,after).Single().Key==3 && ItemGains.Losses(before,after).Single().Key==2,"gain 3, loss 2");
            Check(ItemGains.SingleSpent(ItemGains.Losses(before,after))==2,"one unit spent");
            Check(ItemGains.SingleSpent(new Dictionary<uint,long>{[2]=3}) is null,"three spent: not an opening");
        }),
        ("Item gains: attribution order: fishing, then busy windows stop, FATE reward, monster drop outside duties, opened item",() =>
        {
            Check(ItemGains.Attribute(true,true,1000,0,0,-100_000,false,null,false,false)==ItemGains.Cause.Fishing,"fishing");
            Check(ItemGains.Attribute(false,true,1000,900,7,-100_000,false,null,false,false)==ItemGains.Cause.None,"a shop open");
            Check(ItemGains.Attribute(false,false,10_000,1000,7,9000,false,null,false,false)==ItemGains.Cause.FateReward,"FATE first");
            Check(ItemGains.Attribute(false,false,10_000,-100_000,0,9000,false,null,false,false)==ItemGains.Cause.MonsterDrop,"target died 1 s ago");
            Check(ItemGains.Attribute(false,false,10_000,-100_000,0,9000,true,null,false,false)==ItemGains.Cause.None,"not in a duty");
            Check(ItemGains.Attribute(false,false,10_000,-100_000,0,-100_000,false,5,true,false)==ItemGains.Cause.Opened,"a coffer opened");
            Check(ItemGains.Attribute(false,false,10_000,-100_000,0,-100_000,false,5,false,false)==ItemGains.Cause.None,"not a usable item");
        }),
        ("Store: item sources without a place, quantities widened, each event counted; places only where they belong",() =>
        {
            var store=new DiscoveryStore();
            Check(store.SeeSource(DiscoveryKind.Desynthesis,5,100,2,At),"new");
            Check(!store.SeeSource(DiscoveryKind.Desynthesis,5,100,4,At.AddMinutes(1)),"again");
            var fact=store.Find(DiscoveryKind.Desynthesis,_=>true,5).Single();
            Check(fact.Seen==2 && fact.LevelMin==2 && fact.LevelMax==4 && fact.Map==0,"widened, twice, no place");
            store.SeeSource(DiscoveryKind.MonsterDrop,6,113,1,At,399,213,new Vector3(120,0,40));
            Check(store.Find(DiscoveryKind.MonsterDrop,_=>true,5).Single().CellX==2,"a drop keeps its place");
            Check(DiscoveryStore.Valid(fact) && !DiscoveryStore.Valid(new DiscoveryFact{Kind=DiscoveryKind.MonsterDrop,Subject=6}),"a drop needs a map");
            Check(new DiscoverySettings().ItemSources && new DiscoverySettings().Fishing,"on by default");
        }),
    ];
}
