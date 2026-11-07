// 0.848 (X-70, user report): the quest markers from the game's current list replace the window's stale ones.
using TenshoAssistant.Maps;
internal static class QuestEvents848Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Quest events: the game's list replaces the same quests; other markers stay; nothing read keeps the window's",() =>
        {
            (bool Quest,uint Key,string Where)[] window=[(true,2607,"inn"),(false,0,"fate"),(true,1309,"other quest")];
            (bool Quest,uint Key,string Where)[] game=[(true,2607,"ruby sea")];
            var merged=MapQuestEvents.Merge(window,game,marker=>(marker.Quest,marker.Key));
            Check(merged.Count==3 && merged.Any(marker=>marker.Where=="ruby sea") && merged.All(marker=>marker.Where!="inn"),"step replaced");
            Check(merged.Any(marker=>marker.Where=="fate") && merged.Any(marker=>marker.Where=="other quest"),"others kept");
            Check(ReferenceEquals(MapQuestEvents.Merge(window,[],marker=>(marker.Quest,marker.Key)),window),"nothing read");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs"));
            Check(reader.Contains("if(info->ObjectiveId==0 || !info->ShouldRender)continue;"),"rendered quests only");
        }),
    ];
}
