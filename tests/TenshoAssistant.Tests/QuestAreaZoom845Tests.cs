// 0.845 (X-68, user request): from the journal, the map zooms on the quest's area.
using TenshoAssistant.Maps;
internal static class QuestAreaZoom845Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Quest area zoom: the area fills 60 % of the view; none without an area",() =>
        {
            Check(Math.Abs(MapProjection.AreaZoom(600,100)-1.8f)<0.01f,"600 px view, 100 px radius");
            Check(MapProjection.AreaZoom(600,0)==0,"no area");
            Check(MapProjection.AreaZoom(600,1)==MapProjection.ClampZoom(1000),"clamped");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("mapZoom=areaZoom>0 ? areaZoom:MapProjection.ClampZoom(mapZoom);"),"the zoom stays without an area");
        }),
    ];
}
