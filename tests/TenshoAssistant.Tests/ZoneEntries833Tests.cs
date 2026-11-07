// 0.833 (X-63, user request): « Go there » to a zone with no aetheryte.
using System.Numerics;
using TenshoAssistant.Maps;
internal static class ZoneEntries833Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static Vector2 Flat(Vector3 world,MapDefinition map)=>new(world.X,world.Z);
    private static readonly MapDefinition Hinterlands=new(213,399,"","The Dravanian Hinterlands","",0,95);
    private static readonly MapDefinition Idyllshire=new(257,478,"","Idyllshire","",0,400);
    private static readonly AetheryteRow[] Rows=
    [
        new(75,true,6,478,"Idyllshire",new(0,0,0)),new(90,false,6,478,"West Idyllshire",new(-300,0,0)),
        new(91,false,6,399,"Prologue Gate",new(100,0,100)),new(92,false,6,399,"Epilogue Gate",new(900,0,100)),
    ];
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Zone entries: a shard placed in the zone, nearest the point, through its town's aetheryte",() =>
        {
            var entry=ZoneEntries.Plan(Hinterlands,new(850,120),Rows,[],new Dictionary<uint,uint>{[75]=600},Flat);
            Check(entry is {Aetheryte:75,Shard:92,NoAethernet:false},"Epilogue Gate through Idyllshire");
            Check(ZoneEntries.Plan(Hinterlands,new(850,120),Rows,[],new Dictionary<uint,uint>(),Flat) is null,"Idyllshire not attuned: nothing");
        }),
        ("Zone entries: else the cheapest neighbour, its shard nearest the way in, or « no aethernet » said",() =>
        {
            var target=new MapDefinition(500,900,"","Somewhere","",0,100);
            var town=new ZoneEntryLink(Idyllshire,[new(-290,0)],[(75u,new Vector2(0,0))]);
            var wild=new ZoneEntryLink(new(214,400,"","The Churning Mists","",0,95),[new(10,10)],[(78u,new Vector2(0,0))]);
            var rows=Rows.Append(new(78,true,0,400,"Moghome",null)).ToArray();
            var cheap=ZoneEntries.Plan(target,new(0,0),rows,[town,wild],new Dictionary<uint,uint>{[75]=300,[78]=500},Flat);
            Check(cheap is {Aetheryte:75,Shard:90,NoAethernet:false,ViaMapId:257},"Idyllshire, then West Idyllshire");
            var walk=ZoneEntries.Plan(target,new(0,0),rows,[town,wild],new Dictionary<uint,uint>{[75]=900,[78]=500},Flat);
            Check(walk is {Aetheryte:78,Shard:null,NoAethernet:true},"Moghome, no aethernet");
        }),
    ];
}
