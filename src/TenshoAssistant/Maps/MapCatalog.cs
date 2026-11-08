using System.Collections.Frozen;
using System.Globalization;

namespace TenshoAssistant.Maps;

public sealed record MapDefinition(uint MapId, uint TerritoryId, string Region, string Zone, string Subzone,
    int MapIndex, int SizeFactor, uint PlaceNameId = 0, string TexturePath = "", int OffsetX = 0, int OffsetY = 0, uint MarkerRange = 0, uint RegionPlaceId = 0,
    int MenuCategory = 0, int MenuPriority = 0);

/// <summary>
/// Zone menu of the game map (probe docs/evidence/0.105.0-zones-probe.log): maps sharing the region map's PriorityCategoryUI,
/// ordered by PriorityUI, one entry per priority; maps of one priority are the floors of that entry, ordered by MapIndex.
/// </summary>
public static class MapMenus
{
    /// <summary>
    /// 0.621 (user report, compared in game): what the game's map writes under the zone: where the player stands (TerritoryInfo), then the sub-place when there is one (user's choice: no « -- »). Only for the
    /// map the player is on. Nothing when the place is unknown.
    /// </summary>
    public static IReadOnlyList<string> PlaceLines(uint playerMap,uint shownMap,string? area,string? subArea)
        =>playerMap==0 || playerMap!=shownMap || string.IsNullOrEmpty(area) ? []:string.IsNullOrEmpty(subArea) ? ["> "+area]:["> "+area,"> "+subArea];

    /// <summary>
    /// 0.641 (user capture, docs/evidence/0.641-zone-menu.md): the game's zone list also holds maps with no menu category, seen for the shown map and the quest's map: added after the others, once, in the same region.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<MapDefinition>> WithExtras(IReadOnlyList<IReadOnlyList<MapDefinition>> zones,MapDefinition? region,params MapDefinition?[] extras)
    {
        if(region is null || region.RegionPlaceId==0)return zones;
        var result=zones.ToList();
        foreach(var extra in extras)
            if(extra is { TerritoryId:not 0,MenuCategory:0 } && extra.RegionPlaceId==region.RegionPlaceId && !result.Any(entry=>entry.Contains(extra)))result.Add([extra]);
        return result;
    }
    /// <summary>
    /// 0.619 (user report: « 0 » in the menu): the name of a floor. The game gives a name, or a number for the floors of a dungeon; its main map has neither (Limsa's
    /// upper decks: map 11, index 0, no subzone, beside its Airship Landing), so it takes the zone's name.
    /// </summary>
    public static string FloorLabel(MapDefinition map)=>map.Subzone.Length>0 ? map.Subzone:map.MapIndex>0 ? map.MapIndex.ToString():map.Zone;
    public static IReadOnlyList<IReadOnlyList<MapDefinition>> Zones(IEnumerable<MapDefinition> maps,MapDefinition? region)
        =>region is not { MenuCategory:>0 } ? []:maps.Where(map=>map.MenuCategory==region.MenuCategory && map.MenuPriority>region.MenuPriority && map.TerritoryId!=0)
            .GroupBy(map=>map.MenuPriority).OrderBy(group=>group.Key)
            .Select(group=>(IReadOnlyList<MapDefinition>)group.OrderBy(map=>map.MapIndex).ThenBy(map=>map.MapId).ToArray()).ToArray();
}

/// <summary>0.634: where a dungeon is entered, in map pixels (docs/evidence/0.634-dungeon-entrances.md).</summary>
public sealed record MapEntrance(uint MapId,float X,float Y);
public static class MapEntrances
{
    /// <summary>The game's dungeon entrance icon on a map (MapMarker.Icon); its name is the dungeon's.</summary>
    public const uint Icon=60414;
    /// <summary>Each dungeon's marker, by its place name's row, then by the same text. A dungeon without one is simply absent.</summary>
    public static Dictionary<uint,MapEntrance> Build(IEnumerable<(uint Territory,uint Place,string Name)> dungeons,IEnumerable<(uint Range,uint Place,string Name,float X,float Y)> markers,IEnumerable<MapDefinition> maps)
    {
        var mapOfRange=new Dictionary<uint,uint>();
        foreach(var map in maps.Where(map=>map.MarkerRange!=0 && map.TerritoryId!=0).OrderBy(map=>map.MapIndex).ThenBy(map=>map.MapId))mapOfRange.TryAdd(map.MarkerRange,map.MapId);
        var byPlace=new Dictionary<uint,MapEntrance>();var byName=new Dictionary<string,MapEntrance>(StringComparer.OrdinalIgnoreCase);
        foreach(var marker in markers)
            if(mapOfRange.TryGetValue(marker.Range,out var mapId))
            {
                var entrance=new MapEntrance(mapId,marker.X,marker.Y);
                byPlace.TryAdd(marker.Place,entrance);
                if(marker.Name.Length>0)byName.TryAdd(marker.Name,entrance);
            }
        var result=new Dictionary<uint,MapEntrance>();
        foreach(var dungeon in dungeons)
            if(byPlace.TryGetValue(dungeon.Place,out var found) || (dungeon.Name.Length>0 && byName.TryGetValue(dungeon.Name,out found)))result[dungeon.Territory]=found;
        return result;
    }
}
public sealed class MapCatalog
{
    private readonly FrozenDictionary<uint, MapDefinition> byId;
    private readonly FrozenDictionary<uint, IReadOnlyList<MapDefinition>> byPlace;
    public IReadOnlyList<MapDefinition> Entries { get; }
    private readonly IReadOnlyDictionary<uint,uint> territoryMaps;
    /// <summary>0.628: the map a territory names in the game's TerritoryType sheet (0 if none). It can differ from the Map sheet's way back (docs/evidence/0.628-quest-target-on-world-map.md).</summary>
    public uint MapOf(uint territory)=>territoryMaps.GetValueOrDefault(territory);
    private readonly IReadOnlyDictionary<uint,MapEntrance> entrances;
    public MapEntrance? EntranceOf(uint territory)=>entrances.GetValueOrDefault(territory);
    public MapCatalog(IEnumerable<MapDefinition> source, CancellationToken cancellation = default, IReadOnlyDictionary<uint,uint>? territoryMaps = null, IReadOnlyDictionary<uint,MapEntrance>? entrances = null)
    {
        this.entrances=entrances ?? new Dictionary<uint,MapEntrance>();
        this.territoryMaps=territoryMaps ?? new Dictionary<uint,uint>();
        ArgumentNullException.ThrowIfNull(source);
        var copied = new Dictionary<uint, MapDefinition>();
        cancellation.ThrowIfCancellationRequested();
        foreach (var map in source)
        {
            cancellation.ThrowIfCancellationRequested();
            if (map.MapId == 0 || map.TerritoryId == 0 || map.SizeFactor <= 0 || string.IsNullOrWhiteSpace(map.Zone)) continue;
            if (!copied.TryAdd(map.MapId, map)) throw new ArgumentException("Duplicate map ID.", nameof(source));
        }
        byId = copied.ToFrozenDictionary();
        Entries = Array.AsReadOnly(copied.Values.OrderBy(map => map.Region, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(map => map.Zone, StringComparer.CurrentCultureIgnoreCase).ThenBy(map => map.MapIndex).ThenBy(map => map.MapId).ToArray());
        byPlace = Entries.Where(map => map.PlaceNameId != 0).GroupBy(map => map.PlaceNameId)
            .ToFrozenDictionary(group => group.Key,group => (IReadOnlyList<MapDefinition>)Array.AsReadOnly(group.ToArray()));
        cancellation.ThrowIfCancellationRequested();
    }
    public MapDefinition? Find(uint id) => byId.GetValueOrDefault(id);
    /// <summary>
    /// Maps showing the same ground as <paramref name="map"/> (0.175): same territory, scale and offset, so world positions land on the same
    /// pixel. Auxesia (territory 1319) has 22 such variants, c1w4/01–22, that change with cosmic exploration progress, while NPC positions
    /// are recorded on the territory's default map (docs/evidence/0.175.0-auxesia-maps-probe.log).
    /// </summary>
    public IReadOnlyList<uint> Equivalent(MapDefinition map) => Entries
        .Where(other => other.MapId == map.MapId || other.TerritoryId == map.TerritoryId && other.SizeFactor == map.SizeFactor
            && other.OffsetX == map.OffsetX && other.OffsetY == map.OffsetY)
        .Select(other => other.MapId).ToArray();
    /// <summary>
    /// 0.679 (user report): the versions of one drawing of the land, such as the Island Sanctuary's as it develops (maps 772, 773, 774, 780): the game may place a marker on any of them.
    /// Stricter than Equivalent: the floors of a zone have the same frame but their own marker range, and a marker of another floor must not appear on this one.
    /// </summary>
    public IReadOnlyList<uint> SameLand(MapDefinition map) => Entries
        .Where(other => other.MapId == map.MapId || other.MarkerRange != 0 && other.MarkerRange == map.MarkerRange && other.TerritoryId == map.TerritoryId
            && other.SizeFactor == map.SizeFactor && other.OffsetX == map.OffsetX && other.OffsetY == map.OffsetY)
        .Select(other => other.MapId).ToArray();
    public IReadOnlyList<MapDefinition> ForPlace(uint placeNameId) => byPlace.GetValueOrDefault(placeNameId) ?? Array.Empty<MapDefinition>();
    public IReadOnlyList<MapDefinition> Search(string query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0) return Entries;
        return Array.AsReadOnly(Entries.Where(map => tokens.All(token =>
            CultureInfo.CurrentCulture.CompareInfo.IndexOf($"{map.Region} {map.Zone} {map.Subzone} {map.MapId}", token,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)).ToArray());
    }
    // A generous input bound protects the native coordinate encoder; this is not a map availability check.
    public static bool CoordinatesValid(float x, float y) => float.IsFinite(x) && float.IsFinite(y)
        && x is >= -10000 and <= 10000 && y is >= -10000 and <= 10000;
}

public sealed record MapCatalogState(MapCatalog? Catalog, int Copied, int Total, string? Error);

/// <summary>
/// What the game asked when its map opened: the selected map, the open request (quest, flag, link) and its territory,
/// and the player's current map. All copied on the framework thread.
/// </summary>
// QuestId/AetheryteId/FateId: what the request points at (CurrentOpenMapInfo), used to centre the Tensho map on it.
// QuestLog: the journal or gathering log opened the map, even when it gives no id.
// Target: world X/Z of the game's first temporary marker (AgentMap.TempMapMarkers[0], X/Y = world × 16), where the game itself
// centres a quest or gathering link; read the way Mappy does (reference only, AGPL, not copied).
public sealed record GameMapRequest(uint Selected, uint Requested, uint Territory, uint Current, uint QuestId = 0, uint AetheryteId = 0, uint FateId = 0, bool QuestLog = false,
    System.Numerics.Vector2? Target = null, uint TargetIcon = 0, uint RouteMap = 0, System.Numerics.Vector2? Route = null, uint RouteIcon = 0, string Title = "", int TargetScale = 0)
{
    /// <summary>
    /// 0.627 (user report, log: the same « QuestLog » request at each press of the map key): the game keeps its last quest request after the map closes. Read again unchanged,
    /// it asks for nothing: the map simply opens on the player.
    /// </summary>
    public GameMapRequest Plain()=>this with {Selected=Current,Requested=Current,Territory=0,QuestId=0,AetheryteId=0,FateId=0,QuestLog=false,Target=null,TargetIcon=0,RouteMap=0,Route=null,RouteIcon=0,Title="",TargetScale=0};
    /// <summary>0.633: this map shows the way (the zone-change NPC), not the target's own territory.</summary>
    public bool UsesRoute(MapDefinition map)=>Route is not null && RouteMap==map.MapId && Territory!=map.TerritoryId;
    /// <summary>
    /// 0.634 (user request): a quest in a dungeon shows where it is entered, from outside it. The game's own way (a route marker) comes first; inside the dungeon, its own map.
    /// </summary>
    public MapEntrance? EntranceTarget(MapCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if(!QuestLog && QuestId==0 || RouteMap!=0)return null;
        var place=Territory!=0 ? Territory:catalog.Find(Selected)?.TerritoryId ?? 0;
        if(place==0 || catalog.Find(Current)?.TerritoryId==place)return null;
        return catalog.EntranceOf(place) is { } entrance && catalog.Find(entrance.MapId) is not null ? entrance:null;
    }
    /// <summary>A quest, aetheryte or FATE was asked: never the M toggle.</summary>
    public bool Pointed => QuestId != 0 || AetheryteId != 0 || FateId != 0 || QuestLog || Target is not null;
    /// <summary>
    /// Map to show in the Tensho window, 0 = follow the player (M on the current zone, which toggles the window).
    /// A zone selection wins; when the selection is not a zone (world or region map, as seen in 0.111 for a quest), the
    /// request is used, then the first map of the requested territory, then the selection itself.
    /// </summary>
    public uint Resolve(MapCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (EntranceTarget(catalog) is { } entrance) return entrance.MapId;
        // 0.151: a game target (journal) lives on the requested map, which may differ from the selected zone when the quest
        // is behind a zone-change NPC (Solution Nine → Nine Sentinel, user capture).
        if (Target is not null && Requested != 0 && catalog.Find(Requested) is not null) return Requested;
        if (catalog.Find(Selected) is not null) return Selected == Current ? 0 : Selected;
        if (catalog.Find(Requested) is not null) return Requested;
        // 0.633 (user capture): away from it, the game shows the way in. 0.638 (log, a dungeon's own map opened instead): before looking for a map of that territory.
        var own = catalog.MapOf(Territory);
        if (RouteMap != 0 && own != Current && catalog.Find(Current)?.TerritoryId != Territory && catalog.Find(RouteMap) is not null) return RouteMap;
        if (Territory != 0 && catalog.Entries.Where(map => map.TerritoryId == Territory).OrderBy(map => map.MapIndex).FirstOrDefault() is { } zone) return zone.MapId;
        // 0.628 (user capture): a request on the world map names the target's territory: open that territory's own map.
        if (catalog.Find(own) is not null) return own;
        return Selected;
    }
}
public sealed class MapCatalogService
{
    private MapCatalogState state = new(null, 0, 0, null);
    public MapCatalogState State => Volatile.Read(ref state);
    internal void Progress(int copied, int total) => Interlocked.Exchange(ref state, new(null, copied, total, null));
    internal void Publish(MapCatalog catalog) => Interlocked.Exchange(ref state, new(catalog, 0, 0, null));
    internal void Fail(Exception error) => Interlocked.Exchange(ref state, new(null, 0, 0, error.Message));
    internal void Clear() => Interlocked.Exchange(ref state, new(null, 0, 0, null));
}

/// <summary>
/// Game map icon a vendor NPC stands by (MAP-017, docs/evidence/0.155.0-vendor-markers-probe.log): shop icons (shop, exchange,
/// merchant) within 16 texture pixels, as since 0.140; service icons seen beside vendors (in the probe log under the French client's names:
/// Réparations, Officier du logement, Mage bleu, Guide, Commise aux objets de classe/job, Commandes spéciales, Dresseur) within 8 pixels and only
/// when the marker has no place label.
/// </summary>
public static class MapVendorIcons
{
    public static readonly FrozenSet<uint> Shops = new uint[] { 60412, 60935, 63851 }.ToFrozenSet();
    public static readonly FrozenSet<uint> Services = new uint[] { 60434, 60768, 60983, 63921, 63919, 60927, 63997 }.ToFrozenSet();
    public const float ShopRadius = 16, ServiceRadius = 8;

    /// <summary>
    /// Index of the marker the vendor at <paramref name="pixel"/> belongs to, or -1: the nearest qualifying marker of the vendor's own
    /// kind (<paramref name="preferred"/>: the exchange icon for exchange-only vendors, the shop icon otherwise) when one is in reach, else the nearest
    /// (0.171: in Auxesia the credit exchange stands 2.3 px from a shop icon and 3.7 px from its exchange icon).
    /// </summary>
    public static int Nearest(IReadOnlyList<(float X, float Y, uint Icon, bool Labelled)> markers, System.Numerics.Vector2 pixel, uint preferred = 0)
    {
        int best = -1, bestPreferred = -1; float bestDistance = float.MaxValue, bestPreferredDistance = float.MaxValue;
        for (var index = 0; index < markers.Count; index++)
        {
            var marker = markers[index];
            var radius = Shops.Contains(marker.Icon) ? ShopRadius : Services.Contains(marker.Icon) && !marker.Labelled ? ServiceRadius : 0;
            var distance = System.Numerics.Vector2.Distance(new(marker.X, marker.Y), pixel);
            if (radius <= 0 || distance > radius) continue;
            if (distance < bestDistance) { best = index; bestDistance = distance; }
            if (preferred != 0 && marker.Icon == preferred && distance < bestPreferredDistance) { bestPreferred = index; bestPreferredDistance = distance; }
        }
        return bestPreferred >= 0 ? bestPreferred : best;
    }
}
