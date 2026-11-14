using TenshoAssistant.Maps;
using TenshoAssistant.Quests;

// 0.1346 (X-358, review of 0.1345): the list of the quests the game offers counted every type 3 marker, and the accepted quests' markers
// (Map.QuestMarkers) are type 3 too. Only the quests of the game's list of quests not accepted yet are offers now.
internal static class QuestOffered1346Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private sealed record Marker(uint MapId,uint ObjectiveId,byte Type,float X,float Z,bool Offered=false);
    private static (bool,uint) Key(Marker marker)=>(marker.Type==3,marker.ObjectiveId & 0xFFFF);
    private static IReadOnlyList<Marker> Tag(IReadOnlyList<Marker> markers,IReadOnlyList<Marker> unaccepted,IReadOnlyList<Marker> accepted)=>
        MapOfferedQuests.Tag(markers,unaccepted,accepted,Key,marker=>marker with { Offered=true });
    private static IReadOnlySet<uint>? Offered(IReadOnlyList<Marker> markers,uint map)=>MapLocalLists.Offered(markers.Select(marker=>(marker.MapId,marker.ObjectiveId,marker.Offered)),map);
    private static QuestEntry Quest(uint id,QuestProgress progress)=>new(id,"q"+id,"",0,progress,null){PreviousQuestIds=[],PreviousQuestJoin=1,Level=50};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Quest offered 1346: an accepted quest and an offered one at the same place: only the offered one is an offer",() =>
        {
            var accepted=new[]{new Marker(3,0x0006,3,12f,22f)};          // a step of an accepted quest
            var unaccepted=new[]{new Marker(3,0x0003,3,12f,22f)};        // a quest offered by the same giver
            var merged=MapGameLeves.Merge<Marker>([..accepted],unaccepted,marker=>(marker.MapId,0u,marker.X,marker.Z));
            Check(merged.Count==1 && merged[0].ObjectiveId==0x0006,"same place and icon: the merge keeps one row (the accepted step)");
            var tagged=Tag([..accepted,..unaccepted],unaccepted,accepted);
            var offered=Offered(tagged,3);
            Check(offered is not null && offered.SetEquals([0x10003u]),"only the quest not accepted is offered (before: both)");
            var index=new MapLocalIndex([new MapLocalPosition(MapLocalKind.QuestIssuer,0x10003,3,12f,22f,0),new MapLocalPosition(MapLocalKind.QuestIssuer,0x10006,3,12f,22f,0),
                new MapLocalPosition(MapLocalKind.QuestIssuer,0x10009,3,12f,22f,0)]);
            var entries=new[]{Quest(0x10003,QuestProgress.Unconfirmed),Quest(0x10006,QuestProgress.Active),Quest(0x10009,QuestProgress.Unconfirmed)};
            var ids=MapLocalLists.Quests(entries,index,3,false,includeLocked:false,offered:offered).SelectMany(group=>group.Quests).Select(item=>item.Quest.Id).Order().ToArray();
            Check(ids.SequenceEqual([0x10003u,0x10006u]),"the offered quest and the one under way stay; the one the game does not offer leaves");
        }),
        ("Quest offered 1346: accepted quests alone do not make the map « covered »; the mark goes by quest; never on an accepted quest",() =>
        {
            var accepted=new[]{new Marker(3,0x0006,3,12f,22f)};
            Check(Offered(Tag(accepted,[],accepted),3) is null,"only accepted steps on the map: no offer list, so no restriction (before: every quest not started left)");
            var window=new Marker(3,0x0003,3,40f,50f);                  // the map window's marker of the offered quest, kept by the merge
            var tagged=Tag([window,..accepted],[new Marker(3,0x0003,3,40.5f,50f)],accepted);
            Check(tagged[0].Offered && !tagged[1].Offered,"the window's marker of an offered quest is marked; the accepted step is not");
            Check(!Tag([..accepted],[new Marker(3,0x10006,3,1f,1f)],accepted)[0].Offered,"a quest in both lists is never an offer");
            Check(!Tag([new Marker(3,0x0003,1,1f,1f)],[new Marker(3,0x0003,1,1f,1f)],[])[0].Offered,"a marker that is not a quest is never an offer");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs")).Replace("\r\n","\n");
            Check(reader.Contains("current=MapOfferedQuests.Tag(current,unaccepted,accepted,"),"the reader marks the offers from both game lists");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapLocalLists.cs"));
            Check(page.Contains("(marker.MapId,marker.ObjectiveId,marker.Offered)"),"the tab reads the mark, not the marker type");
        }),
    ];
}
