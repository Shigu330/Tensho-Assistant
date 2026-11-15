using System.Numerics;
using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
// X-386 (0.1402): every summoning bell and company chest placed by the territories' layouts (planlive, planevent, planmap), converted to
// map pixels, against the game's own map markers of the same icon (60425 bell, 60460 company chest; MapSymbol, evidence 0.139).
// X-424 (0.1405): a placement is compared to the markers of every map of its territory (Map.TerritoryType), each with its own offset and
// scale, not only the territory's main map: a bell the game marks on another floor of the place is not missing.
System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
using var game=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.English,CacheFileResources=false});
uint[] bells=[2000401,2000403,2000441,2006565,2010284,2014985];const uint chest=2000470;
var markers=game.Excel.GetSubrowSheet<MapMarker>();
var seenBg=new HashSet<string>();var matched=new Dictionary<uint,int>();var missing=new List<string>();var festival=0;var outside=0;var worst=0f;
var otherFloor=new List<string>();
var marksByMap=new Dictionary<uint,List<(uint Icon,Vector2 At)>>();
List<(uint Icon,Vector2 At)> MarksOf(Map map)
{
    if(marksByMap.TryGetValue(map.RowId,out var marks))return marks;
    marks=[];
    if(map.MapMarkerRange!=0 && markers.TryGetRow(map.MapMarkerRange,out var rows))
        foreach(var r in rows)if(r.Icon is 60425 or 60460)marks.Add((r.Icon,new(r.X,r.Y)));
    return marksByMap[map.RowId]=marks;
}
var mapsByTerritory=game.GetExcelSheet<Map>()!.Where(m=>m.TerritoryType.RowId!=0 && m.SizeFactor!=0).GroupBy(m=>m.TerritoryType.RowId).ToDictionary(g=>g.Key,g=>g.ToList());
Vector2 Pixel(Lumina.Data.Parsing.Common.Vector3 t,Map map){var scale=map.SizeFactor/100f;return new((t.X+map.OffsetX)*scale+1024f,(t.Z+map.OffsetY)*scale+1024f);}
foreach(var place in game.GetExcelSheet<TerritoryType>()!)
{
    if(place.Map.RowId==0 || !place.Map.IsValid)continue;
    var bg=place.Bg.ExtractText();var cut=bg.IndexOf("/level/",StringComparison.Ordinal);
    if(cut<0 || !seenBg.Add(bg))continue;
    var map=place.Map.Value;if(map.SizeFactor==0)continue;
    var marks=MarksOf(map);
    var others=mapsByTerritory.GetValueOrDefault(place.RowId,[]).Where(m=>m.RowId!=map.RowId).ToList();
    foreach(var kind in new[]{"planlive","planevent","planmap"})
    {
        var path=$"bg/{bg[..(cut+1)]}level/{kind}.lgb";
        if(!game.FileExists(path))continue;
        Lumina.Data.Files.LgbFile? file;try{file=game.GetFile<Lumina.Data.Files.LgbFile>(path);}catch{continue;}
        foreach(var layer in file!.Layers)
            foreach(var o in layer.InstanceObjects)
            {
                if(o.AssetType!=Lumina.Data.Parsing.Layer.LayerEntryType.EventObject || o.Object is not Lumina.Data.Parsing.Layer.LayerCommon.EventInstanceObject e)continue;
                var id=e.ParentData.BaseId;uint icon=id==chest ? 60460u:bells.Contains(id) ? 60425u:0u;
                if(icon==0)continue;
                if(layer.FestivalID!=0){festival++;continue;}
                var t=o.Transform.Translation;
                var pixel=Pixel(t,map);
                var near=marks.Where(m=>m.Icon==icon).Select(m=>Vector2.Distance(m.At,pixel)).DefaultIfEmpty(float.MaxValue).Min();
                if(pixel.X<0 || pixel.Y<0 || pixel.X>2048 || pixel.Y>2048){outside++;continue;}
                if(near<=32){matched[icon]=matched.GetValueOrDefault(icon)+1;worst=Math.Max(worst,near);continue;}
                var elsewhere=others.Select(m=>(Map:m,Near:MarksOf(m).Where(k=>k.Icon==icon).Select(k=>Vector2.Distance(k.At,Pixel(t,m))).DefaultIfEmpty(float.MaxValue).Min()))
                    .Where(p=>p.Near<=32).Select(p=>$"{p.Map.RowId} ({p.Near:F1} px; offset {p.Map.OffsetX},{p.Map.OffsetY}, size {p.Map.SizeFactor})").ToList();
                var line=$"icon {icon} eobj {id} territory {place.RowId} \"{place.PlaceName.ValueNullable?.Name.ExtractText()}\" map {map.RowId} intended {place.TerritoryIntendedUse.RowId} {kind} world {t.X:F1},{t.Z:F1} pixel {pixel.X:F0},{pixel.Y:F0} (offset {map.OffsetX},{map.OffsetY}, size {map.SizeFactor}) nearest {(near==float.MaxValue ? "none":near.ToString("F0"))}; maps of the territory {string.Join(",",mapsByTerritory.GetValueOrDefault(place.RowId,[]).Select(m=>m.RowId))}";
                if(elsewhere.Count>0){otherFloor.Add($"ON ANOTHER MAP {line}; game marker on map {string.Join(", ",elsewhere)}");continue;}
                marks.Add((icon,pixel));
                missing.Add("MISSING "+line);
            }
    }
}
Console.WriteLine($"MATCHED (within 32 px of the game's marker of the same icon): "+string.Join(", ",matched.Select(p=>$"{p.Key}x{p.Value}"))+$"; farthest match {worst:F1} px; festival layers left out {festival}; outside the map texture (another map of the place) {outside}");
foreach(var line in otherFloor)Console.WriteLine(line);
Console.WriteLine($"ON ANOTHER MAP TOTAL {otherFloor.Count}");
foreach(var line in missing)Console.WriteLine(line);
Console.WriteLine($"MISSING TOTAL {missing.Count}");
