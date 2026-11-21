using System.Numerics;
using TenshoAssistant.Maps;

// 0.1672 (X-684, third part: places): one place tooltip (MainWindow.PlaceTooltip.cs) for the map bookmarks, the chat's map links and their
// list, the map's favourites and the place directory: zone and sub-zone, region, coordinates, nearest aetheryte with its cost, weather now;
// worked out once for the place under the mouse.
internal static class PlaceTips1672Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly MapDefinition Map=new(12,135,"La Noscea","Basse-Noscea","Moraby",0,100);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Place tips 1672: zone and sub-zone, region, coordinates, the nearest aetheryte with its cost or not attuned, the weather",()=>
        {
            Check(PlaceTips.Area(Map)=="Basse-Noscea · Moraby" && PlaceTips.Area(Map with { Subzone="" })=="Basse-Noscea" && PlaceTips.Area(Map with { Subzone="Basse-Noscea" })=="Basse-Noscea","area");
            var point=new Vector2(20,20);var pixel=MapProjection.PixelFromCoordinate(point,Map);
            (uint,float,float,string)[] aetherytes=[(8,pixel.X+400,pixel.Y,"Far"),(9,pixel.X+10,pixel.Y,"Near"),(0,pixel.X,pixel.Y,"not an aetheryte")];
            var facts=PlaceTips.Build(Map,point,aetherytes,new Dictionary<uint,uint>{[9]=107},("Clair",60202));
            Check(facts is { Area:"Basse-Noscea · Moraby",Region:"La Noscea",Aetheryte:"Near",AetheryteCost:107,Attuned:true,Weather:"Clair",WeatherIcon:60202 } && facts.Coordinates==MapProjection.CoordinateText(point),"the facts");
            var notAttuned=PlaceTips.Build(Map,point,aetherytes,new Dictionary<uint,uint>{[8]=200},null);
            Check(notAttuned is { Aetheryte:"Near",AetheryteCost:null,Attuned:false,Weather:null },"not in the teleport list: not attuned; no weather known");
            var unread=PlaceTips.Build(Map,point,aetherytes,null,null);
            Check(unread is { Aetheryte:"Near",AetheryteCost:null,Attuned:null },"teleport list not read: neither cost nor « not attuned »");
            var zone=PlaceTips.Build(Map,null,aetherytes,null,("Pluie",1));
            Check(zone is { Coordinates:null,Aetheryte:null,Weather:"Pluie" } && PlaceTips.Build(Map,new Vector2(float.NaN,1),aetherytes,null,null).Coordinates is null,"a whole zone, or coordinates not valid: no point, no aetheryte");
        }),
        ("Place tips 1672: one body, kept for the place under the mouse; the bookmarks, chat links, favourites and directory use it",()=>
        {
            var body=Read("src/TenshoAssistant/MainWindow.PlaceTooltip.cs");
            Check(body.Contains("seen.Map==map.MapId && seen.Coordinate==coordinate && seen.Minute==minute && ReferenceEquals(seen.Costs,costs))return seen.Facts;")
                  && body.Split("PlaceTips.Build(").Length==2,"worked out once, then kept");
            Check(Read("src/TenshoAssistant/MainWindow.MapBookmarks.cs").Contains("DrawPlaceTooltip(map!, new Vector2(point.X, point.Y),")
                  && Read("src/TenshoAssistant/MainWindow.ChatWindow.cs").Contains("DrawPlaceTooltip(map,coordinate,link.Place.Length>0 ? link.Place:null,localization.Format(\"ChatWindow.MapLinkTip\",\"\").Trim());")
                  && Read("src/TenshoAssistant/MainWindow.ChatMapLinks.cs").Contains("DrawPlaceTooltip(map,new Vector2(link.X,link.Y),")
                  && Read("src/TenshoAssistant/MainWindow.MapLocalLists.cs").Contains("DrawPlaceTooltip(here ? map:target!,new Vector2(point.X,point.Y),name,point.Note);")
                  && Read("src/TenshoAssistant/MainWindow.PlaceDirectory.cs").Contains("DrawPlaceTooltip(districtMap,null,"),"the five places use it");
            Check(!Read("src/TenshoAssistant/MainWindow.MapBookmarks.cs").Contains("{map!.Zone} · {map.Subzone}\\nX"),"the bookmark's hand-written tooltip is gone");
        }),
    ];
}
