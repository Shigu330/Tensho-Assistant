using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Maps;

// Fixed map icon in texture pixels (MapMarker X/Y), with its game icon and optional label. Icon 0 = a place name drawn as text.
// LinkMapId: on region and world maps, DataType 1 markers open that map (probe: docs/evidence/0.103.0-maps-probe.log).
// AetheryteId: DataType 3 markers are aetherytes (DataKey = Aetheryte row; probe docs/evidence/0.107.0-aetheryte-probe.log).
// Orientation: where the game writes the label (MapMarker SubtextOrientation, Maps/MapLabels.cs).
public sealed record MapIconMarker(float X,float Y,uint Icon,string Label,uint LinkMapId=0,uint AetheryteId=0,byte Orientation=0);

/// <summary>
/// Game data for the map window, copied once per key: fixed markers per range, and the overview maps (regions, world)
/// that the zone catalogue leaves out because they have no territory.
/// </summary>
internal sealed class MapMarkerSource
{
    private readonly IDataManager data;private readonly IPluginLog log;
    private readonly Dictionary<uint,IReadOnlyList<MapIconMarker>> cache=[];
    // 0.328: ranges whose read threw, kept apart from the cache so a failure is « unavailable », never « no icons »;
    // not retried (For runs from Draw, a retry would throw every frame).
    private readonly HashSet<uint> failed=[];
    private Dictionary<uint,MapDefinition>? overviews;
    private Dictionary<uint,uint>? regionMaps;
    private List<uint>? regionOrder;
    private uint worldMap;
    private uint firstWorldMap;
    public MapMarkerSource(IDataManager data,IPluginLog log){this.data=data;this.log=log;}
    /// <summary>True when the fixed markers of this range could not be read (For then returns an empty list).</summary>
    public bool Failed(uint range)=>failed.Contains(range);
    public IReadOnlyList<MapIconMarker> For(uint range)
    {
        if(range==0 || failed.Contains(range))return [];
        if(cache.TryGetValue(range,out var known))return known;
        try{return cache[range]=Copy(range);}
        catch(Exception error){log.Warning(error,"Map markers unavailable for range {0}.",range);failed.Add(range);return [];}
    }
    /// <summary>0.597 (P10): the markers with a name, for work off the frame: touches neither the frame's cache nor its failures.</summary>
    public IReadOnlyList<MapIconMarker> Read(uint range)
    {
        try{return range==0 ? []:Copy(range).Where(marker=>marker.Label.Length>0).ToArray();}
        catch(Exception error){log.Warning(error,"Map markers unavailable for range {0}.",range);return [];}
    }
    private (Dictionary<uint,(uint Map,uint Range)> ByPlace,Dictionary<string,(uint Map,uint Range)> ByName)? namedMaps;
    private readonly object namedMapsLock=new();
    // 0.639: the map such a marker names: by its place's row, then by the same text (Vallée des Pampas: rows 4918 and 4926). Also read off the frame: Read.
    private uint NamedMap(uint icon,uint dataType,uint place,string label,uint range)
    {
        if(label.Length==0 || !MapLabels.OpensNamedMap(icon,dataType))return 0;
        lock(namedMapsLock)
        {
            if(namedMaps is null)
            {
                var byPlace=new Dictionary<uint,(uint Map,uint Range)>();var byName=new Dictionary<string,(uint Map,uint Range)>(StringComparer.OrdinalIgnoreCase);
                foreach(var row in data.GetExcelSheet<Map>())
                {
                    if(row.TerritoryType.RowId==0 || row.SizeFactor==0 || row.PlaceName.RowId==0)continue;
                    byPlace.TryAdd(row.PlaceName.RowId,(row.RowId,row.MapMarkerRange));
                    byName.TryAdd(row.PlaceName.Value.Name.ExtractText(),(row.RowId,row.MapMarkerRange));
                }
                namedMaps=(byPlace,byName);
            }
            // A marker named like its own map leads nowhere.
            return (namedMaps.Value.ByPlace.TryGetValue(place,out var found) || namedMaps.Value.ByName.TryGetValue(label,out found)) && found.Range!=range ? found.Map:0;
        }
    }
    private IReadOnlyList<MapIconMarker> Copy(uint range)
    {
        var markers=new List<MapIconMarker>();
        if(data.GetSubrowExcelSheet<MapMarker>().TryGetRow(range,out var rows))
            foreach(var row in rows)
            {
                var label=row.PlaceNameSubtext.RowId!=0 ? row.PlaceNameSubtext.Value.Name.ExtractText():"";
                if(row.Icon!=0 || label.Length>0)markers.Add(new(row.X,row.Y,row.Icon,label,row.DataType==1 ? row.DataKey.RowId:NamedMap(row.Icon,row.DataType,row.PlaceNameSubtext.RowId,label,range),row.DataType==3 ? row.DataKey.RowId:0,row.SubtextOrientation));
            }
        return markers.AsReadOnly();
    }

    public bool OverviewsFailed{get;private set;}
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
                if(id.StartsWith("world/",StringComparison.Ordinal))
                {
                    // 0.632 (user capture): the world map is the game's latest Hydaelyn (PlaceName 20: world/03, world/04), not the old world/00 without Tural.
                    if(worldMap==0 || row.PlaceName.RowId==20)worldMap=row.RowId;
                    if(id=="world/02")firstWorldMap=row.RowId;
                }
                else
                {
                    if(regionMaps.TryAdd(row.PlaceNameRegion.RowId,row.RowId) && int.TryParse(id["region/".Length..].Split('/')[0],out var index))
                        ordered.Add((index,row.RowId));
                }
            }
        }
        // R-14 (0.507): all or nothing: a partial list would read as a map without its region.
        catch(Exception error){overviews.Clear();regionMaps.Clear();ordered.Clear();worldMap=0;firstWorldMap=0;OverviewsFailed=true;log.Warning(error,"Overview maps unavailable.");}
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
    /// <summary>0.632 (user captures): the game's choice Hydaelyn / The First, on those two maps only: the world's « floors ».</summary>
    public IReadOnlyList<MapDefinition> WorldChoices(uint mapId)
    {
        LoadOverviews();
        if(firstWorldMap==0 || (mapId!=worldMap && mapId!=firstWorldMap))return [];
        return overviews!.TryGetValue(worldMap,out var hydaelyn) && overviews.TryGetValue(firstWorldMap,out var first) ? [hydaelyn,first]:[];
    }
    private Dictionary<uint,(uint Row,bool Locked,int Variant)>? questIcons;
    /// <summary>
    /// Quest marker icons of the map (0.211): EventIconType rows, MapIconAvailable / MapIconInvalid + 1..IconRange (e.g. row 3 = 71001…
    /// main scenario, row 8 = 71141… feature quests; docs/evidence/0.211-event-icons.md). Not in MapSymbol, so added to the icon settings.
    /// </summary>
    public IReadOnlyDictionary<uint,(uint Row,bool Locked,int Variant)> QuestIcons
    {
        get
        {
            if(questIcons is not null)return questIcons;
            questIcons=[];
            try
            {
                foreach(var row in data.GetExcelSheet<EventIconType>())
                    for(var variant=1;variant<=Math.Clamp((int)row.IconRange,0,8);variant++)
                    {
                        if(row.MapIconAvailable>0)questIcons.TryAdd(row.MapIconAvailable+(uint)variant,(row.RowId,false,variant));
                        if(row.MapIconInvalid>0)questIcons.TryAdd(row.MapIconInvalid+(uint)variant,(row.RowId,true,variant));
                    }
            }
            catch(Exception error){log.Warning(error,"Quest marker icons unavailable.");}
            return questIcons;
        }
    }
    private Dictionary<uint,string>? symbols;
    /// <summary>Every named map icon kind of the game (MapSymbol sheet), for the global icon settings.</summary>
    public IReadOnlyCollection<uint> SymbolIcons{get{SymbolName(0);return symbols!.Keys;}}
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
