using TenshoAssistant.Gathering;
using TenshoAssistant.Maps;
using TenshoAssistant.Quests;

internal static class MapLocalIndexTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static QuestEntry Quest(uint id,string name,string category,ushort level,QuestProgress progress)=>new(id,name,category,0,progress,null){Level=level};
    // Rows from checkpoints/0.114.0-local-probe.log (quest 65539 on map 3, gathering base 153).
    private static MapLocalIndex Index()=>new([
        new(MapLocalKind.QuestIssuer,65539,3,-238f,-142.9f,0),new(MapLocalKind.QuestIssuer,65540,3,-233.9f,-171f,0),new(MapLocalKind.QuestIssuer,65537,2,49.8f,51.8f,0),
        new(MapLocalKind.QuestIssuer,65541,3,-1f,-1f,0),new(MapLocalKind.GatheringPoint,153,0,-115.9f,209.2f,53),
        new(MapLocalKind.QuestIssuer,65539,9,0,0,0),new(MapLocalKind.QuestIssuer,0,3,1,1,0),new(MapLocalKind.GatheringPoint,154,0,float.NaN,1,0)]);
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Map local index keeps first valid position per quest and gathering base",()=>
        {
            var index=Index();
            Check(index.Quest(65539) is { MapId:3 } first && first.X==-238f,"first quest row wins");
            Check(index.Quest(0) is null && index.Gathering(154) is null,"zero id and non-finite rows dropped");
            Check(index.Gathering(153) is { Radius:53 } && index.Quest(153) is null,"kinds kept apart");
            Check(index.Count==5,"count");
        }),
        ("Map local quests group by category, hide completed by default and stay on the map",()=>
        {
            var entries=new[]{Quest(65539,"B","Botanique",1,QuestProgress.Unconfirmed),Quest(65540,"A","Quêtes annexes",5,QuestProgress.Active),
                Quest(65541,"C","Quêtes annexes",3,QuestProgress.Complete),Quest(65537,"D","Botanique",1,QuestProgress.Unconfirmed),Quest(99,"E","X",1,QuestProgress.Active)};
            var open=MapLocalLists.Quests(entries,Index(),3,false);
            Check(open.Count==2 && open[0].Category=="Botanique" && open[0].Quests.Count==1 && open[1].Quests.Single().Quest.Id==65540,"open quests of map 3");
            var all=MapLocalLists.Quests(entries,Index(),3,true);
            Check(all[1].Quests.Select(item=>item.Quest.Id).SequenceEqual(new uint[]{65541,65540}),"completed shown on request, sorted by level");
            Check(MapLocalLists.Quests(entries,Index(),42,true).Count==0,"another map has none");
        }),
        ("Map local gathering merges items per point base and leaves fishing without position",()=>
        {
            var sources=new[]{new GatheringSource(5,30001,153,0,"Minage",1,10,1,"Zone","Lieu A",MapId:3),new GatheringSource(6,30001,153,0,"Minage",1,10,1,"Zone","Lieu A",MapId:3,IsHidden:true),
                new GatheringSource(7,1,90,4,"Pêche",1,20,1,"Zone","Rive",MapId:3,IsFishing:true),new GatheringSource(8,30002,154,0,"Minage",1,15,1,"Zone","Lieu B",MapId:4)};
            var groups=MapLocalLists.Gathering(sources,Index(),3);
            Check(groups.Count==2 && groups[0].Type=="Minage" && groups[1].Type=="Pêche","types of map 3");
            var point=groups[0].Points.Single();
            Check(point.Items.SequenceEqual(new uint[]{5,6}) && point.Hidden && point.Position is { Radius:53 },"items merged with position");
            Check(groups[1].Points.Single().Position is null,"fishing has no position");
        }),
        ("Map local cache key follows the shape and rejects broken snapshots",()=>
        {
            var rows=new[]{new MapLocalPosition(MapLocalKind.QuestIssuer,1,3,1,2,0)};
            Check(MapLocalIndexCache.Valid(new("v",rows),"v") && !MapLocalIndexCache.Valid(new("w",rows),"v"),"data version");
            Check(!MapLocalIndexCache.Valid(new("v",[]),"v") && !MapLocalIndexCache.Valid(new("v",[new MapLocalPosition((MapLocalKind)9,1,3,1,2,0)]),"v")
                && !MapLocalIndexCache.Valid(new("v",[new MapLocalPosition(MapLocalKind.GatheringPoint,1,0,float.NaN,2,0)]),"v"),"empty, unknown kind, NaN");
            Check(MapLocalIndexCache.Version("v")!=MapLocalIndexCache.Version("w") && MapLocalIndexCache.Version("v").Contains("MapLocalPosition("),"version holds data and shape");
            var copy=System.Text.Json.JsonSerializer.Deserialize<MapLocalIndexCacheData>(System.Text.Json.JsonSerializer.Serialize(new MapLocalIndexCacheData("v",rows)))!;
            Check(MapLocalIndexCache.Valid(copy,"v") && copy.Positions[0]==rows[0],"JSON roundtrip");
        }),
    };
}
