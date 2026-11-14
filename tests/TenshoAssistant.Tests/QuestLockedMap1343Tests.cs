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
    ];
}
