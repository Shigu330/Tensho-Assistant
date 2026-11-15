using Dalamud.Plugin.Services;

namespace TenshoAssistant.Maps;

/// <summary>X-462 (0.1464): the MapRange boxes of a territory's planmap.lgb that name a map (docs/evidence/0.1464-map-floors.md), read off the frame by
/// the loaders that place layout objects; none when the file is missing or unreadable (the territory's map is kept then).</summary>
internal static class MapFloorReader
{
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
