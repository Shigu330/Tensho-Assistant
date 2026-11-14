using TenshoAssistant.Maps;
using TenshoAssistant.Quests;

// 0.1343 (X-355, user report of 2026-11-14 at the Steps of Thal, with captures: « Quêtes pas encore disponibles affiche ou masque les quêtes
// déjà débloquées … il prend aussi les quêtes où on n'a pas le lvl, le job »): the game draws a quest marker locked (red) for a quest not
// unlocked yet, and also for one the current level or job cannot take. The box « Quests not available yet », off, now hides only the quests
// not unlocked (previous quests not done), the rule of the map's quest list.
internal static class QuestLockedMap1343Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static QuestEntry Quest(uint id,QuestProgress progress,params uint[] previous)=>new(id,"q"+id,"",0,progress,null){PreviousQuestIds=previous,PreviousQuestJoin=1,Level=50};
    private static readonly QuestLinks Links=new(
    [
        Quest(0x10001,QuestProgress.Complete),
        Quest(0x10002,QuestProgress.Unconfirmed,0x10001),   // unlocked: drawn red by the game for the level or the job
        Quest(0x10003,QuestProgress.Unconfirmed),            // no previous quest: unlocked
        Quest(0x10004,QuestProgress.Unconfirmed),
        Quest(0x10005,QuestProgress.Unconfirmed,0x10004),   // its previous quest is not done: not unlocked
        Quest(0x10006,QuestProgress.Active,0x10004),        // under way (a locked step marker)
    ]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Quest locked map 1343: box off, a locked marker is hidden only when its quest is not unlocked",() =>
        {
            Check(!MapQuestIcons.HiddenAsLocked(false,true,0x0002,Links),"unlocked quest drawn red (level or job): kept (the report)");
            Check(!MapQuestIcons.HiddenAsLocked(false,true,0x10003,Links),"no previous quest: kept");
            Check(MapQuestIcons.HiddenAsLocked(false,true,0x0005,Links),"previous quest not done: hidden");
            Check(!MapQuestIcons.HiddenAsLocked(false,true,0x0006,Links),"a quest under way: kept");
            Check(MapQuestIcons.HiddenAsLocked(false,true,0x0099,Links) && MapQuestIcons.HiddenAsLocked(false,true,0,Links) && MapQuestIcons.HiddenAsLocked(false,true,0x0002,null),
                "a quest Tensho cannot find (or no list yet): the game's word, hidden");
        }),
        ("Quest locked map 1343: box on, or a marker the game does not draw locked: always shown; the same rule as the map's quest list",() =>
        {
            Check(!MapQuestIcons.HiddenAsLocked(true,true,0x0005,Links),"box on: shown");
            Check(!MapQuestIcons.HiddenAsLocked(false,false,0x0005,Links),"not drawn locked: shown");
            Check(MapQuestIcons.QuestId(0x0002)==0x10002 && MapQuestIcons.QuestId(0x10002)==0x10002,"objective to quest row");
            foreach(var id in new uint[]{0x10002,0x10003,0x10005})
                Check(MapQuestIcons.HiddenAsLocked(false,true,id,Links)==!QuestList.Available(Links,Links.Find(id)!),"same answer as the list for "+id);
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs")).Replace("\r\n","\n");
            Check(view.Contains("if(!MapQuestIcons.HiddenAsLocked(settings.ShowLockedQuests,mapMarkers?.QuestIcons.TryGetValue(marker.Icon,out var questIcon)==true && questIcon.Locked,marker.ObjectiveId,questLinks))"),"the map uses it");
            Check(view.Contains("if(mapQuestLinks is { } known && ReferenceEquals(known.Entries,entries))return known.Links;"),"links built again only when the list changes");
        }),
        ("Quest locked map 1344: a seasonal event's quest outside its event is not available in the Quests tab; during it, it stays",() =>
        {
            // 0.1344 (user question: « tu as aussi pris en compte les quêtes event ? »). Festival 100 runs, festival 200 does not.
            var index=new MapLocalIndex([new MapLocalPosition(MapLocalKind.QuestIssuer,0x10003,3,10f,20f,0),new MapLocalPosition(MapLocalKind.QuestIssuer,0x10007,3,11f,21f,0),
                new MapLocalPosition(MapLocalKind.QuestIssuer,0x10008,3,12f,22f,0)]);
            var entries=new[]{Quest(0x10003,QuestProgress.Unconfirmed),Quest(0x10007,QuestProgress.Unconfirmed),Quest(0x10008,QuestProgress.Unconfirmed)};
            var questsOf=new Dictionary<uint,IReadOnlyList<uint>>{[100]=[0x10007],[200]=[0x10008]};
            static uint[] Ids(IReadOnlyList<(string Category,IReadOnlyList<MapLocalQuest> Quests)> groups)=>groups.SelectMany(group=>group.Quests).Select(item=>item.Quest.Id).Order().ToArray();
            var off=MapLocalLists.OutOfSeason(questsOf,[100u]);
            Check(off is not null && off.SetEquals([0x10008u]),"festival 200 not running: its quest out of season");
            Check(Ids(MapLocalLists.Quests(entries,index,3,false,includeLocked:false,outOfSeason:off)).SequenceEqual([0x10003u,0x10007u]),"box off: the out-of-season event quest leaves the list, the running event's stays");
            Check(Ids(MapLocalLists.Quests(entries,index,3,false,includeLocked:true,outOfSeason:off)).SequenceEqual([0x10003u,0x10007u,0x10008u]),"box on: every quest");
            Check(MapLocalLists.OutOfSeason(null,[100u]) is null && MapLocalLists.OutOfSeason(questsOf,null) is null,"festivals or their quests unknown: nothing taken for out of season");
            Check(Ids(MapLocalLists.Quests(entries,index,3,false,includeLocked:false,outOfSeason:null)).SequenceEqual([0x10003u,0x10007u,0x10008u]),"unknown: as before");
        }),
        ("Quest locked map 1345: where the game's list covers the map, a quest not started is available only when the game offers it",() =>
        {
            // 0.1345 (user report at the Gold Saucer: « Toutes les cartes en main » listed, absent from the map, « la carte a raison »).
            var index=new MapLocalIndex([new MapLocalPosition(MapLocalKind.QuestIssuer,0x10003,3,10f,20f,0),new MapLocalPosition(MapLocalKind.QuestIssuer,0x10009,3,11f,21f,0),
                new MapLocalPosition(MapLocalKind.QuestIssuer,0x10006,3,12f,22f,0)]);
            var entries=new[]{Quest(0x10003,QuestProgress.Unconfirmed),Quest(0x10009,QuestProgress.Unconfirmed),Quest(0x10006,QuestProgress.Active)};
            static uint[] Ids(IReadOnlyList<(string Category,IReadOnlyList<MapLocalQuest> Quests)> groups)=>groups.SelectMany(group=>group.Quests).Select(item=>item.Quest.Id).Order().ToArray();
            var offered=MapLocalLists.Offered([(3u,0x0003u,(byte)3),(4u,0x0009u,(byte)3),(3u,0x0009u,(byte)1)],3);
            Check(offered is not null && offered.SetEquals([0x10003u]),"the game offers 0x10003 on map 3 (another map's marker and a quest step are not offers)");
            Check(Ids(MapLocalLists.Quests(entries,index,3,false,includeLocked:false,offered:offered)).SequenceEqual([0x10003u,0x10006u]),"box off: the quest the game does not offer leaves the list; one under way stays");
            Check(Ids(MapLocalLists.Quests(entries,index,3,false,includeLocked:true,offered:offered)).SequenceEqual([0x10003u,0x10006u,0x10009u]),"box on: every quest");
            Check(MapLocalLists.Offered([(4u,0x0009u,(byte)3)],3) is null && MapLocalLists.Offered(null,3) is null,"no offer of the game on this map: no restriction (not covered and none offered are not told apart)");
        }),
    ];
}
