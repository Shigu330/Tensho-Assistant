using Dalamud.Plugin.Services;

namespace TenshoAssistant.Maps;

/// <summary>X-462 (0.1464): the MapRange boxes of a territory's planmap.lgb that name a map (docs/evidence/0.1464-map-floors.md), read off the frame by
/// the loaders that place layout objects; none when the file is missing or unreadable (the territory's map is kept then).
/// X-480 (0.1469): the loaders take the boxes of the territory's floors only (Floors: MapFloors.Floors with the frames of the Map sheet), once per
/// layout and map (FloorCache): planmap.lgb was read again for each NPC found by the Triple Triad loader.</summary>
internal static class MapFloorReader
{
    /// <summary>X-480 (0.1469): the boxes of the floors of a territory's map (MapFloors.Floors): none when it has no floors.</summary>
    public static IReadOnlyList<MapFloorRange> Floors(IDataManager data,string bg,Lumina.Excel.Sheets.Map home)
    {
        var ranges=Read(data,bg);
        if(ranges.Count==0)return ranges;
        var maps=data.GetExcelSheet<Lumina.Excel.Sheets.Map>();
        return MapFloors.Floors(ranges,Frame(home),id=>maps.TryGetRow(id,out var map) ? Frame(map):null);
    }

    private static MapFloorFrame Frame(Lumina.Excel.Sheets.Map map)=>new(map.RowId,map.TerritoryType.RowId,map.SizeFactor,map.OffsetX,map.OffsetY,map.MapMarkerRange);

    /// <summary>X-480 (0.1469): Floors read once per layout and territory map, for one loader's run (not thread-safe: one per run).</summary>
    public sealed class FloorCache(IDataManager data)
    {
        private readonly Dictionary<(string Bg,uint Map),IReadOnlyList<MapFloorRange>> floors=[];
        public IReadOnlyList<MapFloorRange> Of(string bg,Lumina.Excel.Sheets.Map home)
            =>floors.TryGetValue((bg,home.RowId),out var known) ? known:floors[(bg,home.RowId)]=Floors(data,bg,home);
    }

    public static IReadOnlyList<MapFloorRange> Read(IDataManager data,string bg)
    {
        var cut=bg.IndexOf("/level/",StringComparison.Ordinal);
        if(cut<0)return [];
        var path=$"bg/{bg[..(cut+1)]}level/planmap.lgb";
        try
        {
            if(!data.FileExists(path) || data.GetFile(path)?.Data is not { } bytes || !Npcs.NpcShopLinks.ValidLayout(bytes))return [];
            if(data.GetFile<Lumina.Data.Files.LgbFile>(path) is not { } file)return [];
            var ranges=new List<MapFloorRange>();
            foreach(var layer in file.Layers)
                foreach(var instance in layer.InstanceObjects)
                    if(instance.AssetType==Lumina.Data.Parsing.Layer.LayerEntryType.MapRange
                       && instance.Object is Lumina.Data.Parsing.Layer.LayerCommon.MapRangeInstanceObject range && range.Map!=0)
                    {
                        var at=instance.Transform.Translation;var half=instance.Transform.Scale;
                        ranges.Add(new(range.Map,at.X,at.Y,at.Z,half.X,half.Y,half.Z,instance.Transform.Rotation.Y));
                    }
            return ranges;
        }
        catch(Exception){return [];}
    }
}
