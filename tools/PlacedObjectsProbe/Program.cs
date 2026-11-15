using System.Numerics;
using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
// X-386 (0.1402): every summoning bell and company chest placed by the territories' layouts (planlive, planevent, planmap), converted to
// map pixels, against the game's own map markers of the same icon (60425 bell, 60460 company chest; MapSymbol, evidence 0.139).
System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
using var game=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.English,CacheFileResources=false});
uint[] bells=[2000401,2000403,2000441,2006565,2010284,2014985];const uint chest=2000470;
var markers=game.Excel.GetSubrowSheet<MapMarker>();
var seenBg=new HashSet<string>();var matched=new Dictionary<uint,int>();var missing=new List<string>();var festival=0;var outside=0;var worst=0f;
var marksByMap=new Dictionary<uint,List<(uint Icon,Vector2 At)>>();
foreach(var place in game.GetExcelSheet<TerritoryType>()!)
{
    if(place.Map.RowId==0 || !place.Map.IsValid)continue;
    var bg=place.Bg.ExtractText();var cut=bg.IndexOf("/level/",StringComparison.Ordinal);
    if(cut<0 || !seenBg.Add(bg))continue;
    var map=place.Map.Value;if(map.SizeFactor==0)continue;
    if(!marksByMap.TryGetValue(map.RowId,out var marks))
    {
        marks=[];
        if(map.MapMarkerRange!=0 && markers.TryGetRow(map.MapMarkerRange,out var rows))
            foreach(var r in rows)if(r.Icon is 60425 or 60460)marks.Add((r.Icon,new(r.X,r.Y)));
        marksByMap[map.RowId]=marks;
    }
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
                var scale=map.SizeFactor/100f;var t=o.Transform.Translation;
                var pixel=new Vector2((t.X+map.OffsetX)*scale+1024f,(t.Z+map.OffsetY)*scale+1024f);
                var near=marks.Where(m=>m.Icon==icon).Select(m=>Vector2.Distance(m.At,pixel)).DefaultIfEmpty(float.MaxValue).Min();
                if(pixel.X<0 || pixel.Y<0 || pixel.X>2048 || pixel.Y>2048){outside++;continue;}
                if(near<=32){matched[icon]=matched.GetValueOrDefault(icon)+1;worst=Math.Max(worst,near);continue;}
                marks.Add((icon,pixel));
                missing.Add($"MISSING icon {icon} eobj {id} territory {place.RowId} \"{place.PlaceName.ValueNullable?.Name.ExtractText()}\" map {map.RowId} intended {place.TerritoryIntendedUse.RowId} {kind} world {t.X:F1},{t.Z:F1} pixel {pixel.X:F0},{pixel.Y:F0} nearest {(near==float.MaxValue ? "none":near.ToString("F0"))}");
            }
    }
}
Console.WriteLine($"MATCHED (within 32 px of the game's marker of the same icon): "+string.Join(", ",matched.Select(p=>$"{p.Key}x{p.Value}"))+$"; farthest match {worst:F1} px; festival layers left out {festival}; outside the map texture (another map of the place) {outside}");
foreach(var line in missing)Console.WriteLine(line);
Console.WriteLine($"MISSING TOTAL {missing.Count}");
