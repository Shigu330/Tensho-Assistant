using System.Collections.Frozen;
using TenshoAssistant.Caching;
using TenshoAssistant.Gathering;
using TenshoAssistant.Quests;

namespace TenshoAssistant.Maps;

public enum MapLocalKind { QuestIssuer, GatheringPoint, FishingSpot, Sightseeing, AetherCurrent }

/// <summary>
/// World X/Z position of a quest giver (Quest.IssuerLocation → Level) or a gathering point (ExportedGatheringPoint, keyed by
/// GatheringPointBase). MapId is 0 when the row gives no map: gathering points take the map of their catalogue source.
/// Proof of the links: docs/evidence/0.114.0-local-probe.log (Fate.Location is not a Level row, so FATEs have no static position).
/// Fishing spots (0.126): FishingSpot.X/Z are map texture pixels (docs/evidence/0.126.0-fishing-probe.log, equal to Garland's
/// coordinates for spots 2 and 35), converted to world X/Z; their radius unit is unproven, so Radius is 0.
/// </summary>
// Sightseeing (0.127): Id = Adventure row, Data = its discovered picture icon, Name = vista name (Adventure → Level).
// AetherCurrent (0.127): Id = AetherCurrent row; field currents placed by the Level whose Object is the EObj carrying it
// (152/152), quest currents at the quest giver with Data = quest id (docs/evidence/0.127.0-sightseeing-probe.log).
// Vistas (0.129): MinTime/MaxTime = Eorzea hhmm window (e.g. 800–1159, 1800–459 wraps past midnight), Extra = emote name;
// weather is not in the Adventure sheet. Positions checked against the sightseeing log (#001 Barracuda Piers 9.5/7.8).
public sealed record MapLocalPosition(MapLocalKind Kind,uint Id,uint MapId,float X,float Z,float Radius,uint Data=0,string Name="",int MinTime=0,int MaxTime=0,string Extra="")
{
    /// <summary>True when an Eorzea time (minutes of the day) falls in the vista window; a window ending before it starts wraps.</summary>
    public bool InWindow(int eorzeaMinutes)
    {
        var start=MinTime/100*60+MinTime%100;var end=MaxTime/100*60+MaxTime%100;
        return start<=end ? eorzeaMinutes>=start && eorzeaMinutes<=end:eorzeaMinutes>=start || eorzeaMinutes<=end;
    }
}

public sealed class MapLocalIndex
{
    private readonly FrozenDictionary<uint,MapLocalPosition> quests;
    private readonly FrozenDictionary<uint,MapLocalPosition> gathering;
    private readonly FrozenDictionary<uint,MapLocalPosition> fishing;
    private readonly FrozenDictionary<(MapLocalKind,uint),IReadOnlyList<MapLocalPosition>> onMap;
    public int Count{get;}
    public MapLocalIndex(IEnumerable<MapLocalPosition> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var questRows=new Dictionary<uint,MapLocalPosition>();var gatheringRows=new Dictionary<uint,MapLocalPosition>();var fishingRows=new Dictionary<uint,MapLocalPosition>();
        var mapRows=new Dictionary<(MapLocalKind,uint),List<MapLocalPosition>>();var seen=new HashSet<(MapLocalKind,uint)>();
        foreach(var row in rows)
        {
            if(row is null || row.Id==0 || !float.IsFinite(row.X) || !float.IsFinite(row.Z) || !float.IsFinite(row.Radius))continue;
            // First row wins: a later duplicate never moves a known position.
            if(row.Kind is MapLocalKind.Sightseeing or MapLocalKind.AetherCurrent)
            {
                // Listed per map; one position per entry.
                if(row.MapId==0 || row.Name is null || !seen.Add((row.Kind,row.Id)))continue;
                if(!mapRows.TryGetValue((row.Kind,row.MapId),out var list))mapRows[(row.Kind,row.MapId)]=list=[];
                list.Add(row);continue;
            }
            (row.Kind switch{MapLocalKind.QuestIssuer=>questRows,MapLocalKind.FishingSpot=>fishingRows,_=>gatheringRows}).TryAdd(row.Id,row);
        }
        quests=questRows.ToFrozenDictionary();gathering=gatheringRows.ToFrozenDictionary();fishing=fishingRows.ToFrozenDictionary();
        onMap=mapRows.ToFrozenDictionary(pair=>pair.Key,pair=>(IReadOnlyList<MapLocalPosition>)pair.Value.AsReadOnly());
        Count=quests.Count+gathering.Count+fishing.Count+seen.Count;
    }
    public MapLocalPosition? Quest(uint questId)=>quests.GetValueOrDefault(questId);
    public MapLocalPosition? Gathering(uint baseId)=>gathering.GetValueOrDefault(baseId);
    public MapLocalPosition? Fishing(uint spotId)=>fishing.GetValueOrDefault(spotId);
    /// <summary>Sightseeing vistas or aether currents placed on a map.</summary>
    public IReadOnlyList<MapLocalPosition> OnMap(MapLocalKind kind,uint mapId)=>onMap.GetValueOrDefault((kind,mapId)) ?? [];
    /// <summary>Every vista or aether current of the index, all maps.</summary>
    public IEnumerable<MapLocalPosition> All(MapLocalKind kind)=>onMap.Where(pair=>pair.Key.Item1==kind).SelectMany(pair=>pair.Value);
}

public sealed record MapLocalIndexState(MapLocalIndex? Index,int Copied,string? Error);
public sealed class MapLocalIndexService
{
    private MapLocalIndexState state=new(null,0,null);
    public MapLocalIndexState State=>Volatile.Read(ref state);
    internal void Progress(int copied)=>Interlocked.Exchange(ref state,new(null,copied,null));
    internal void Publish(MapLocalIndex index)=>Interlocked.Exchange(ref state,new(index,index.Count,null));
    internal void Fail(Exception error)=>Interlocked.Exchange(ref state,new(null,0,error.Message));
    internal void Clear()=>Interlocked.Exchange(ref state,new(null,0,null));
}

public sealed record MapLocalIndexCacheData(string DataVersion,IReadOnlyList<MapLocalPosition> Positions);
/// <summary>Disk copy of the positions, reused until the client data or the copy shape changes (same rules as the other catalogues).</summary>
public static class MapLocalIndexCache
{
    public const string Family="catalog-map-positions";
    public const int MaximumRows=200_000;
    // Bump when the Lumina → MapLocalPosition copy rules change without a change of record shape.
    // 2: quest giver rows carry the quest kind's map icon in Data (0.151).
    private const int CopyRules=2;
    public static readonly string Shape=CatalogCacheKeys.Shape(typeof(MapLocalPosition),typeof(MapLocalKind));
    public static string Version(string dataVersion)=>CatalogCacheKeys.Version(dataVersion,CopyRules,Shape);
    public static bool Valid(MapLocalIndexCacheData? data,string dataVersion)
        =>data is not null && data.DataVersion==dataVersion && data.Positions is { Count:>0 and <=MaximumRows }
          && data.Positions.All(row=>row is not null && Enum.IsDefined(row.Kind) && row.Id!=0 && float.IsFinite(row.X) && float.IsFinite(row.Z) && float.IsFinite(row.Radius) && row.Name is not null && row.Extra is not null);
}

public sealed record MapLocalQuest(QuestEntry Quest,MapLocalPosition Position);
// Icon: the gathering type's map icon (GatheringType.IconMain, copied into GatheringSource.IconId).
// Schedule (0.130): the point's Eorzea windows when limited (unspoiled, ephemeral, legendary), else null.
public sealed record MapLocalGatheringPoint(uint BaseId,string Type,int Level,string Place,IReadOnlyList<uint> Items,bool Hidden,MapLocalPosition? Position,uint Icon=0,
    GatheringSchedule? Schedule=null)
{
    public bool Timed=>Schedule is { Kind:GatheringScheduleKind.Limited, Windows.Count:>0 };
    /// <summary>True when an Eorzea minute of the day falls in one of the windows (windows may cross midnight).</summary>
    public bool ActiveAt(int eorzeaMinutes)=>Timed && Schedule!.Windows.Any(window=>
    {
        var end=window.EndMinute<=window.StartMinute ? window.EndMinute+1440:window.EndMinute;
        return eorzeaMinutes>=window.StartMinute && eorzeaMinutes<end || eorzeaMinutes+1440>=window.StartMinute && eorzeaMinutes+1440<end;
    });
}

/// <summary>Lists of the right-click menu, by tab: quests whose giver stands on the map, gathering points of the map.</summary>
public static class MapLocalLists
{
    /// <summary>Quests by journal category (sorted by name), each by level then name; completed ones only on request.</summary>
    public static IReadOnlyList<(string Category,IReadOnlyList<MapLocalQuest> Quests)> Quests(IEnumerable<QuestEntry> entries,MapLocalIndex index,uint mapId,bool includeComplete)
    {
        ArgumentNullException.ThrowIfNull(entries);ArgumentNullException.ThrowIfNull(index);
        return entries.Where(entry=>includeComplete || entry.Progress!=QuestProgress.Complete)
            .Select(entry=>index.Quest(entry.Id) is { } position && position.MapId==mapId ? new MapLocalQuest(entry,position):null).OfType<MapLocalQuest>()
            .GroupBy(item=>item.Quest.Category.Length>0 ? item.Quest.Category:"—")
            .OrderBy(group=>group.Key,StringComparer.CurrentCultureIgnoreCase)
            .Select(group=>(group.Key,(IReadOnlyList<MapLocalQuest>)group.OrderBy(item=>item.Quest.Level).ThenBy(item=>item.Quest.Name,StringComparer.CurrentCultureIgnoreCase).ToArray()))
            .ToArray();
    }

    /// <summary>Gathering points of the map by gathering type, one row per point base or fishing spot (items merged).</summary>
    public static IReadOnlyList<(string Type,IReadOnlyList<MapLocalGatheringPoint> Points)> Gathering(IEnumerable<GatheringSource> sources,MapLocalIndex index,uint mapId)
    {
        ArgumentNullException.ThrowIfNull(sources);ArgumentNullException.ThrowIfNull(index);
        return sources.Where(source=>source.MapId==mapId && source.BaseId!=0)
            .GroupBy(source=>(source.TypeName,source.BaseId))
            .Select(group=>
            {
                var first=group.First();
                return new MapLocalGatheringPoint(first.BaseId,first.TypeName,first.Level,first.Place,group.Select(source=>source.ItemId).Distinct().Order().ToArray(),
                    group.Any(source=>source.IsHidden),first.IsFishing ? index.Fishing(first.BaseId):index.Gathering(first.BaseId),first.IconId,
                    group.Select(source=>source.Schedule).FirstOrDefault(schedule=>schedule is { Kind:GatheringScheduleKind.Limited }));
            })
            .GroupBy(point=>point.Type.Length>0 ? point.Type:"—")
            .OrderBy(group=>group.Key,StringComparer.CurrentCultureIgnoreCase)
            .Select(group=>(group.Key,(IReadOnlyList<MapLocalGatheringPoint>)group.OrderBy(point=>point.Level).ThenBy(point=>point.Place,StringComparer.CurrentCultureIgnoreCase).ToArray()))
            .ToArray();
    }
}
