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
    public static IReadOnlyList<IReadOnlyList<MapDefinition>> Zones(IEnumerable<MapDefinition> maps,MapDefinition? region)
        =>region is not { MenuCategory:>0 } ? []:maps.Where(map=>map.MenuCategory==region.MenuCategory && map.MenuPriority>region.MenuPriority && map.TerritoryId!=0)
            .GroupBy(map=>map.MenuPriority).OrderBy(group=>group.Key)
            .Select(group=>(IReadOnlyList<MapDefinition>)group.OrderBy(map=>map.MapIndex).ThenBy(map=>map.MapId).ToArray()).ToArray();
}

public sealed class MapCatalog
{
    private readonly FrozenDictionary<uint, MapDefinition> byId;
    private readonly FrozenDictionary<uint, IReadOnlyList<MapDefinition>> byPlace;
    public IReadOnlyList<MapDefinition> Entries { get; }
    public MapCatalog(IEnumerable<MapDefinition> source, CancellationToken cancellation = default)
    {
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
    System.Numerics.Vector2? Target = null, uint TargetIcon = 0)
{
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
        // 0.151: a game target (journal) lives on the requested map, which may differ from the selected zone when the quest
        // is behind a zone-change NPC (Solution Nine → Nine Sentinel, user capture).
        if (Target is not null && Requested != 0 && catalog.Find(Requested) is not null) return Requested;
        if (catalog.Find(Selected) is not null) return Selected == Current ? 0 : Selected;
        if (catalog.Find(Requested) is not null) return Requested;
        if (Territory != 0 && catalog.Entries.Where(map => map.TerritoryId == Territory).OrderBy(map => map.MapIndex).FirstOrDefault() is { } zone) return zone.MapId;
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
/// Game map icon a vendor NPC stands by (MAP-017, docs/evidence/0.155.0-vendor-markers-probe.log): shop icons (Échoppe, Échangeur,
/// Marchand) within 16 texture pixels, as since 0.140; service icons seen beside vendors (Réparations, Officier du logement, Mage bleu,
/// Guide, Commise aux objets de classe/job, Commandes spéciales, Dresseur) within 8 pixels and only when the marker has no place label.
/// </summary>
public static class MapVendorIcons
{
    public static readonly FrozenSet<uint> Shops = new uint[] { 60412, 60935, 63851 }.ToFrozenSet();
    public static readonly FrozenSet<uint> Services = new uint[] { 60434, 60768, 60983, 63921, 63919, 60927, 63997 }.ToFrozenSet();
    public const float ShopRadius = 16, ServiceRadius = 8;

    /// <summary>Index of the nearest marker the vendor at <paramref name="pixel"/> belongs to, or -1.</summary>
    public static int Nearest(IReadOnlyList<(float X, float Y, uint Icon, bool Labelled)> markers, System.Numerics.Vector2 pixel)
    {
        var best = -1; var bestDistance = float.MaxValue;
        for (var index = 0; index < markers.Count; index++)
        {
            var marker = markers[index];
            var radius = Shops.Contains(marker.Icon) ? ShopRadius : Services.Contains(marker.Icon) && !marker.Labelled ? ServiceRadius : 0;
            var distance = System.Numerics.Vector2.Distance(new(marker.X, marker.Y), pixel);
            if (radius > 0 && distance <= radius && distance < bestDistance) { best = index; bestDistance = distance; }
        }
        return best;
    }
}
