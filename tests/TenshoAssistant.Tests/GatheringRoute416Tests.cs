using TenshoAssistant.Gathering;

// R-14 (0.416): the chain's raw materials by zone — the zone gathering the most items first, untimed points first, the rest apart.
internal static class GatheringRoute416Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static GatheringSource Source(uint item,uint point,uint territory,string zone,int level=50,GatheringSchedule? schedule=null)
        =>new(item,point,point,0,"Mining",0,level,territory,zone,$"Place {point}",1,false,false,schedule);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gathering route: greedy zones, one zone per item, amounts summed, the rest apart",() =>
        {
            var sources=new Dictionary<uint,GatheringSource[]>
            {
                [1]=[Source(1,10,100,"Alpha"),Source(1,11,200,"Beta")],
                [2]=[Source(2,20,200,"Beta")],
                [3]=[Source(3,30,200,"Beta"),Source(3,31,300,"Gamma")],
                [4]=[Source(4,40,300,"Gamma")],
            };
            var route=GatheringRoutes.Build([(1,5),(2,3),(3,2),(4,1),(9,7),(1,1),(5,0)],item=>sources.GetValueOrDefault(item) ?? []);
            Check(route.Zones.Select(zone=>zone.Zone).SequenceEqual(["Beta","Gamma"]),"Beta gathers three items, then Gamma the last");
            Check(route.Zones[0].Rows.Select(row=>row.ItemId).Order().SequenceEqual([1u,2u,3u]) && route.Zones[1].Rows.Single().ItemId==4,"one zone per item");
            Check(route.Zones[0].Rows.Single(row=>row.ItemId==1).Amount==6,"amounts summed");
            Check(route.Elsewhere.Single()==(9u,7L),"no source: apart; zero amounts dropped");
        }),
        ("Gathering route: in a zone, the untimed point first, then the lowest level",() =>
        {
            var timed=GatheringSchedule.FromEphemeral(800,1000);
            var sources=new[]{Source(1,10,100,"Alpha",10,timed),Source(1,11,100,"Alpha",60),Source(1,12,100,"Alpha",20)};
            var route=GatheringRoutes.Build([(1,1)],_=>sources);
            Check(route.Zones.Single().Rows.Single().Source.PointId==12,"untimed first, then the lowest level");
            Check(GatheringRoutes.Build([(1,1)],_=>[sources[0]]).Zones.Single().Rows.Single().Source.PointId==10,"a timed point when it is the only one");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftChain.cs")).Contains("DrawChainGatheringRoute(result);"),"shown under the chain");
        }),
    ];
}
