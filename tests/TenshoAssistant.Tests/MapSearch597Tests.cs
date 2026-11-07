using System.Globalization;
using System.Numerics;
using TenshoAssistant.Maps;

// 0.597 (P10): the search of the Maps page.
internal static class MapSearch597Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static MapSearchHit Hit(string name,string zone="Limsa",MapSearchKind kind=MapSearchKind.Place,uint map=1,float x=100,float y=100)=>new(kind,name,zone,map,new Vector2(x,y));
    private static readonly CultureInfo French=CultureInfo.GetCultureInfo("fr-FR");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map search: every word, in the name or the zone, accents and capitals ignored",() =>
        {
            MapSearchHit[] hits=[Hit("Éthérite de Limsa"),Hit("Marchand","Ul'dah"),Hit("Forgeron","Gridania")];
            Check(MapSearch.Find(hits,"etherite",French).Shown is [{Name:"Éthérite de Limsa"}],"accent ignored");
            Check(MapSearch.Find(hits,"MARCHAND uldah",French).Total==0,"each word must be there");
            Check(MapSearch.Find(hits,"marchand ul'dah",French).Total==1,"name and zone");
            Check(MapSearch.Find(hits,"gridania",French).Shown is [{Name:"Forgeron"}],"by zone");
        }),
        ("Map search: nothing under two letters; names starting with it first; a limit that is said",() =>
        {
            MapSearchHit[] hits=[Hit("Grand Arbre"),Hit("Arbre Vieux")];
            Check(MapSearch.Find(hits,"a",French).Total==0 && MapSearch.Find(hits,"  ",French).Shown.Length==0 && MapSearch.Find(hits,null,French).Total==0,"too short");
            Check(MapSearch.Find(hits,"arbre",French).Shown.Select(hit=>hit.Name).SequenceEqual(["Arbre Vieux","Grand Arbre"]),"prefix first");
            var many=Enumerable.Range(0,MapSearch.MaximumShown+5).Select(index=>Hit($"Point {index}")).ToArray();
            var found=MapSearch.Find(many,"point",French);
            Check(found.Shown.Length==MapSearch.MaximumShown && found.Total==many.Length,"capped, with the real count");
        }),
        ("Map search: one result per spot, nothing without a name, a map or a position",() =>
        {
            var hits=MapSearch.Distinct([Hit("Ore",x:100),Hit("Ore",x:101),Hit("Ore",x:400),Hit("Ore",map:2),Hit("Ore",kind:MapSearchKind.Resource),Hit(""),Hit("None",map:0),Hit("Bad",x:float.NaN)]);
            Check(hits.Length==4,"same spot merged; another spot, map or kind kept; invalid rows out: "+hits.Length);
        }),
        ("Map search: off the frame, from the Maps page, nothing moved in the game",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapSearch.cs"));
            Check(view.Contains("mapFindTask=Task.Run(") && view.Contains("markers?.Read(group.Key)"),"built off the frame, without the frame's marker cache");
            Check(view.Contains("OpenMapView(hit.MapId);") && !view.Contains("PlaceFlagFromMapView"),"opens the Tensho map, no flag");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Maps.cs")).Contains("DrawMapFind(catalog);"),"in the plugin's Maps section");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Lifetime.cs")).Contains("mapFindCancel"),"stopped with the window");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs")).Contains("public IReadOnlyList<MapIconMarker> Read(uint range)"),"a read that touches no shared state");
        }),
    ];
}
