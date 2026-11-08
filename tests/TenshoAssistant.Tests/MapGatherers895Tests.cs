// 0.895 (X-80): the map's gathering points split by gatherer (miner, botanist, fisher), under the « Gathering points » switch.
using TenshoAssistant;
using TenshoAssistant.Maps;
internal static class MapGatherers895Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map gatherers: types 0–1 miner, 2–3 botanist, 4–5 and fishing spots fisher; each switch hides only its own",() =>
        {
            MapLocalGatheringPoint Point(uint type)=>new(1,"",1,"",[],false,null,TypeId:type);
            Check(Point(0).Job==16 && Point(1).Job==16 && Point(2).Job==17 && Point(3).Job==17 && Point(4).Job==18 && Point(5).Job==18 && Point(100).Job==18,"jobs");
            var settings=new MapViewSettings{ShowBotanyPoints=false};
            Check(settings.ShowsGatherer(16) && !settings.ShowsGatherer(17) && settings.ShowsGatherer(18),"switches");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("MapGatheringPoints(map).Where(point=>settings.ShowsGatherer(point.Job))"),"filter");
        }),
    ];
}
