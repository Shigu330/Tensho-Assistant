using System.Numerics;
using TenshoAssistant.Maps;

// 0.596 (P12): the names the game writes beside its icons.
internal static class MapLabels596Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map labels: left, right, below, above the icon, clear of it",() =>
        {
            var text=new Vector2(60,10);const float icon=20;
            Check(MapLabels.Offset(1,icon,text)==new Vector2(-71,-5),"left: ends before the icon");
            Check(MapLabels.Offset(2,icon,text)==new Vector2(11,-5),"right: starts after it");
            Check(MapLabels.Offset(3,icon,text)==new Vector2(-30,11),"below, centred");
            Check(MapLabels.Offset(4,icon,text)==new Vector2(-30,-21),"above, centred");
        }),
        ("Map labels: a value not established goes to the right",() =>
        {
            foreach(var value in new byte[]{0,5,8,11})Check(MapLabels.Offset(value,20,new(60,10))==MapLabels.Offset(2,20,new(60,10)),$"value {value}");
        }),
        ("Map labels: from the sheet, only for a drawn icon, with their setting",() =>
        {
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs"));
            Check(source.Contains("row.SubtextOrientation"),"the game's value");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs")).Replace("\r\n","\n");
            Check(view.Contains("settings.ShowIconNames && settings.NamesShown(mapZoom) && item.Fixed is {Label.Length:>0}"),"after the icon, on its setting and the names' zoom");
            Check(new TenshoAssistant.MapViewSettings().ShowIconNames,"on by default");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.596-map-label-orientation.md")),"the evidence");
        }),
    ];
}
