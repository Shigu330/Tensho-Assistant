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
        ("Map labels (0.623): zoomed out, only the map links; none on top of another, links first",() =>
        {
            Check(MapLabels.Shown(true,0.3f) && !MapLabels.Shown(false,0.3f) && MapLabels.Shown(false,MapLabels.DetailZoom),"by zoom");
            (Vector2,Vector2,bool)[] names=[(new(0,0),new(50,10),false),(new(10,0),new(50,10),true),(new(100,0),new(50,10),false),(new(0,10),new(50,10),false)];
            Check(MapLabels.Fitting(names).Order().SequenceEqual([1,2,3]),"the link wins over the name it touches; touching edges do not count");
            Check(MapLabels.Fitting([]).Length==0,"none");
        }),
        ("Map labels (0.626): the name of a map link opens that map, through the same click as its icon",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("if(overLink)linkUnderMouse=label.Link;"),"over the name");
            Check(view.IndexOf("if(overLink)linkUnderMouse=label.Link;")<view.IndexOf("if(linkUnderMouse is { } link && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !mapDragged)NavigateMap(link);"),"before the click is read");
        }),
        ("Map labels (0.631): a link's whole-zone picture is not a small icon",() =>
        {
            Check(TenshoAssistant.Maps.MapLabels.IsZoneImage(855,1024),"Tuliyollal on Yok Tural");
            Check(!TenshoAssistant.Maps.MapLabels.IsZoneImage(855,64),"a small link icon stays one");
            Check(!TenshoAssistant.Maps.MapLabels.IsZoneImage(0,1024),"not a link");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.IndexOf("0.631: the zones of a region map")<view.IndexOf("var icons=new List<MapIconDraw>();"),"under every icon");
        }),
        ("Map labels (0.632): zones and crystals on a region map, the world's own choice",() =>
        {
            // Wachunpelo on Urqopacha: pixel 1356, 607, link at 479, 799; the game's crystal is at about 618, 624 of Yok Tural.
            var crystal=TenshoAssistant.Maps.MapLabels.ZonePixel(new System.Numerics.Vector2(479,799),new System.Numerics.Vector2(1356,607));
            Check(Math.Abs(crystal.X-618)<4 && Math.Abs(crystal.Y-624)<4,"measured on the capture");
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs"));
            Check(source.Contains("if(worldMap==0 || row.PlaceName.RowId==20)worldMap=row.RowId;"),"the latest Hydaelyn");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("HeaderEntry($\"ZoneWorld{choice.MapId}\"") && view.Contains("##ZoneWorld{choice.MapId}"),"in both menus");
        }),
        ("Map labels: from the sheet, only for a drawn icon, with their setting",() =>
        {
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs"));
            Check(source.Contains("row.SubtextOrientation"),"the game's value");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs")).Replace("\r\n","\n");
            Check(view.Contains("settings.ShowIconNames && settings.NamesShown(mapZoom) && item.Fixed is {Label.Length:>0} named && MapLabels.Shown(named.LinkMapId!=0,mapZoom)"),"on its setting and the zoom");
            Check(new TenshoAssistant.MapViewSettings().ShowIconNames,"on by default");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.596-map-label-orientation.md")),"the evidence");
        }),
    ];
}
