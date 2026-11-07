using System.Numerics;
using TenshoAssistant.Maps;

// 0.813 (X-49, user report): a quest target stays on the map after the quest step is done.
internal static class QuestTarget813Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Quest target: tied to the quest whose point stands there, gone when that point moves or the quest goes",() =>
        {
            var at=new Vector2(100,200);
            var before=new[]{((uint)2285,(uint)219,102f,203f),((uint)5233,(uint)219,500f,500f)};
            Check(MapQuestTarget.QuestAt(before,219,at)==2285 && MapQuestTarget.QuestAt(before,11,at)==0,"the quest there, on that map only");
            Check(!MapQuestTarget.Gone(before,2285,219,at),"still there");
            var stepDone=new[]{((uint)2285,(uint)219,400f,50f)};
            Check(MapQuestTarget.Gone(stepDone,2285,219,at) && MapQuestTarget.Gone([],2285,219,at),"point moved, quest done");
            Check(!MapQuestTarget.Gone([],0,219,at),"no quest found: kept (unknown)");
        }),
        ("Quest target: read from the game's Map, used only for journal targets without a quest, reset with each new target",() =>
        {
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("if(mapGameTarget is { QuestId:0 } bare && mapLive?.State?.QuestPoints is { } points)") && map.Contains("mapGameTargetObjective=0;\n                mapGameTarget=focus.UsesRoute(map)") ,"used, reset");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs")).Contains("if(count is <=0 or >32)continue;"),"bounded");
        }),
    ];
}
