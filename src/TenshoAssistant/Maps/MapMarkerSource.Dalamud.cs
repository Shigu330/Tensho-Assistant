using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Maps;

// Fixed map icon in texture pixels (MapMarker X/Y), with its game icon and optional label. Icon 0 = a place name drawn as text.
// LinkMapId: on region and world maps, DataType 1 markers open that map (probe: checkpoints/0.103.0-maps-probe.log).
// AetheryteId: DataType 3 markers are aetherytes (DataKey = Aetheryte row; probe checkpoints/0.107.0-aetheryte-probe.log).
public sealed record MapIconMarker(float X,float Y,uint Icon,string Label,uint LinkMapId=0,uint AetheryteId=0);

/// <summary>
/// Game data for the map window, copied once per key: fixed markers per range, and the overview maps (regions, world)
/// that the zone catalogue leaves out because they have no territory.
/// </summary>
internal sealed class MapMarkerSource
{
    private readonly IDataManager data;private readonly IPluginLog log;
    private readonly Dictionary<uint,IReadOnlyList<MapIconMarker>> cache=[];
    private Dictionary<uint,MapDefinition>? overviews;
    private Dictionary<uint,uint>? regionMaps;
    private List<uint>? regionOrder;
    private uint worldMap;
    public MapMarkerSource(IDataManager data,IPluginLog log){this.data=data;this.log=log;}
    public IReadOnlyList<MapIconMarker> For(uint range)
    {
        if(range==0)return [];
        if(cache.TryGetValue(range,out var known))return known;
        var markers=new List<MapIconMarker>();
        try
        {
            if(data.GetSubrowExcelSheet<MapMarker>().TryGetRow(range,out var rows))
                foreach(var row in rows)
                {
                    var label=row.PlaceNameSubtext.RowId!=0 ? row.PlaceNameSubtext.Value.Name.ExtractText():"";
                    if(row.Icon!=0 || label.Length>0)markers.Add(new(row.X,row.Y,row.Icon,label,row.DataType==1 ? row.DataKey.RowId:0,row.DataType==3 ? row.DataKey.RowId:0));
                }
        }
        catch(Exception error){log.Warning(error,"Map markers unavailable for range {0}.",range);}
        return cache[range]=markers.AsReadOnly();
    }

    private void LoadOverviews()
    {
        if(overviews is not null)return;
        overviews=[];regionMaps=[];regionOrder=[];
        var ordered=new List<(int Index,uint MapId)>();
        try
        {
            foreach(var row in data.GetExcelSheet<Map>())
            {
                var id=row.Id.ExtractText();
                if(!(id.StartsWith("region/",StringComparison.Ordinal) || id.StartsWith("world/",StringComparison.Ordinal)) || row.SizeFactor==0)continue;
                var name=row.PlaceName.RowId!=0 ? row.PlaceName.Value.Name.ExtractText():"";
                overviews[row.RowId]=new(row.RowId,0,name,name,"",row.MapIndex,row.SizeFactor,row.PlaceName.RowId,MapProjection.TexturePath(id),row.OffsetX,row.OffsetY,row.MapMarkerRange,row.PlaceNameRegion.RowId,
                    row.PriorityCategoryUI,row.PriorityUI);
                if(id.StartsWith("world/",StringComparison.Ordinal)){if(worldMap==0)worldMap=row.RowId;}
                else
                {
                    if(regionMaps.TryAdd(row.PlaceNameRegion.RowId,row.RowId) && int.TryParse(id["region/".Length..].Split('/')[0],out var index))
                        ordered.Add((index,row.RowId));
                }
            }
        }
        catch(Exception error){log.Warning(error,"Overview maps unavailable.");}
        // The game's region menu follows the region map numbers (region/00 Noscea, region/01 forest, …).
        regionOrder.AddRange(ordered.OrderBy(entry=>entry.Index).Select(entry=>entry.MapId));
    }
    public IReadOnlyList<uint> Regions{get{LoadOverviews();return regionOrder!;}}
    /// <summary>Region map shown for a map: a region map itself, the zone's region, or 0 for the world map.</summary>
    public uint RegionOf(MapDefinition map)
    {
        LoadOverviews();
        if(map.MapId==worldMap)return 0;
        if(overviews!.ContainsKey(map.MapId))return map.MapId;
        return regionMaps!.GetValueOrDefault(map.RegionPlaceId);
    }
    public MapDefinition? Overview(uint mapId){LoadOverviews();return overviews!.GetValueOrDefault(mapId);}
    /// <summary>One level up: zone → its region map, region → world map, world → none.</summary>
    public uint Parent(MapDefinition map)
    {
        LoadOverviews();
        if(map.MapId==worldMap)return 0;
        if(overviews!.ContainsKey(map.MapId))return worldMap;
        return regionMaps!.GetValueOrDefault(map.RegionPlaceId,worldMap);
    }
    public uint World{get{LoadOverviews();return worldMap;}}
    private Dictionary<uint,string>? symbols;
    /// <summary>Game name of a map icon kind (MapSymbol sheet: aetheryte, merchant…), or "" when the game gives none.</summary>
    public string SymbolName(uint icon)
    {
        if(symbols is null)
        {
            symbols=[];
            try
            {
                foreach(var row in data.GetExcelSheet<MapSymbol>())
                    if(row.Icon>0 && row.PlaceName.RowId!=0)symbols.TryAdd((uint)row.Icon,row.PlaceName.Value.Name.ExtractText());
            }
            catch(Exception error){log.Warning(error,"Map symbol names unavailable.");}
        }
        return symbols.GetValueOrDefault(icon,"");
    }
}
