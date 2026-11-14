// 0.1107 (X-128, user capture of 2026-11-11): the « Quests not available yet » box sits in the map's Quests tab and also filters its list.
using TenshoAssistant.Maps;
using TenshoAssistant.Quests;

internal static class MapLockedQuests1107Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map locked quests: unticked, the quests not available yet leave the Quests tab's list; started and available ones stay",() =>
        {
            var index=new MapLocalIndex([new MapLocalPosition(MapLocalKind.QuestIssuer,1,3,10f,20f,0),new MapLocalPosition(MapLocalKind.QuestIssuer,2,3,11f,21f,0),
                new MapLocalPosition(MapLocalKind.QuestIssuer,3,3,12f,22f,0),new MapLocalPosition(MapLocalKind.QuestIssuer,4,3,13f,23f,0)]);
            QuestEntry Quest(uint id,QuestProgress progress,params uint[] previous)=>new(id,"Q"+id,"Side",0,progress,null){PreviousQuestIds=previous,PreviousQuestJoin=1,Level=1};
            var entries=new[]{Quest(1,QuestProgress.Complete),Quest(2,QuestProgress.Unconfirmed,1),Quest(3,QuestProgress.Unconfirmed,4),Quest(4,QuestProgress.Active)};
            static uint[] Ids(IReadOnlyList<(string Category,IReadOnlyList<MapLocalQuest> Quests)> groups)=>groups.SelectMany(group=>group.Quests).Select(item=>item.Quest.Id).Order().ToArray();
            Check(Ids(MapLocalLists.Quests(entries,index,3,false)).SequenceEqual([2u,3u,4u]),"ticked (the default): every open quest");
            Check(Ids(MapLocalLists.Quests(entries,index,3,false,includeLocked:false)).SequenceEqual([2u,4u]),"unticked: 3 waits for 4, not done yet");
            Check(Ids(MapLocalLists.Quests(entries,index,3,true,includeLocked:false)).SequenceEqual([1u,2u,4u]),"completed ones still on request");
        }),
        ("Map locked quests: the box is in the Quests tab, no longer in the right-click menu; the map settings keep theirs",() =>
        {
            var lists=Read("MainWindow.MapLocalLists.cs");
            var tab=lists.IndexOf("private void DrawLocalQuests(",StringComparison.Ordinal);
            var box=lists.IndexOf("if(SettingBox(\"MapView.ShowLockedQuests\",settings.ShowLockedQuests,value=>settings.ShowLockedQuests=value))SaveMapSettings();",StringComparison.Ordinal);
            Check(tab>0 && box>tab && box<lists.IndexOf("private void DrawLocalFates(",StringComparison.Ordinal),"in the Quests tab");
            Check(lists.Contains("MapLocalLists.Quests(state.Entries,index,map.MapId,mapLocalShowComplete,locked,MapLocalLists.OutOfSeason(festivals?.Quests,running))") && lists.Contains("cache.Locked!=locked"),"the list follows the box");
            var view=Read("MainWindow.MapView.cs");
            Check(!view.Contains("contextSettings.ShowLockedQuests"),"not in the right-click menu");
            Check(view.Contains("changed|=SettingBox(\"MapView.ShowLockedQuests\",settings.ShowLockedQuests"),"still in the map settings");
        }),
    ];
}
