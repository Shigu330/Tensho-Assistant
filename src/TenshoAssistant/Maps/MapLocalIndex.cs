using System.Collections.Frozen;
using TenshoAssistant.Caching;
using TenshoAssistant.Gathering;
using TenshoAssistant.Quests;

namespace TenshoAssistant.Maps;

public enum MapLocalKind { QuestIssuer, GatheringPoint }

/// <summary>
/// World X/Z position of a quest giver (Quest.IssuerLocation → Level) or a gathering point (ExportedGatheringPoint, keyed by
/// GatheringPointBase). MapId is 0 when the row gives no map: gathering points take the map of their catalogue source.
/// Proof of the links: checkpoints/0.114.0-local-probe.log (Fate.Location is not a Level row, so FATEs have no static position).
/// </summary>
public sealed record MapLocalPosition(MapLocalKind Kind,uint Id,uint MapId,float X,float Z,float Radius);

public sealed class MapLocalIndex
{
    private readonly FrozenDictionary<uint,MapLocalPosition> quests;
    private readonly FrozenDictionary<uint,MapLocalPosition> gathering;
    public int Count{get;}
    public MapLocalIndex(IEnumerable<MapLocalPosition> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var questRows=new Dictionary<uint,MapLocalPosition>();var gatheringRows=new Dictionary<uint,MapLocalPosition>();
        foreach(var row in rows)
        {
            if(row is null || row.Id==0 || !float.IsFinite(row.X) || !float.IsFinite(row.Z) || !float.IsFinite(row.Radius))continue;
            // First row wins: a later duplicate never moves a known position.
            (row.Kind==MapLocalKind.QuestIssuer ? questRows:gatheringRows).TryAdd(row.Id,row);
        }
        quests=questRows.ToFrozenDictionary();gathering=gatheringRows.ToFrozenDictionary();Count=quests.Count+gathering.Count;
    }
    public MapLocalPosition? Quest(uint questId)=>quests.GetValueOrDefault(questId);
    public MapLocalPosition? Gathering(uint baseId)=>gathering.GetValueOrDefault(baseId);
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
    private const int CopyRules=1;
    public static readonly string Shape=CatalogCacheKeys.Shape(typeof(MapLocalPosition),typeof(MapLocalKind));
    public static string Version(string dataVersion)=>CatalogCacheKeys.Version(dataVersion,CopyRules,Shape);
    public static bool Valid(MapLocalIndexCacheData? data,string dataVersion)
        =>data is not null && data.DataVersion==dataVersion && data.Positions is { Count:>0 and <=MaximumRows }
          && data.Positions.All(row=>row is not null && Enum.IsDefined(row.Kind) && row.Id!=0 && float.IsFinite(row.X) && float.IsFinite(row.Z) && float.IsFinite(row.Radius));
}

public sealed record MapLocalQuest(QuestEntry Quest,MapLocalPosition Position);
public sealed record MapLocalGatheringPoint(uint BaseId,string Type,int Level,string Place,IReadOnlyList<uint> Items,bool Hidden,MapLocalPosition? Position);

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

    /// <summary>Gathering points of the map by gathering type, one row per point base (items merged); fishing spots have no position here.</summary>
    public static IReadOnlyList<(string Type,IReadOnlyList<MapLocalGatheringPoint> Points)> Gathering(IEnumerable<GatheringSource> sources,MapLocalIndex index,uint mapId)
    {
        ArgumentNullException.ThrowIfNull(sources);ArgumentNullException.ThrowIfNull(index);
        return sources.Where(source=>source.MapId==mapId && source.BaseId!=0)
            .GroupBy(source=>(source.TypeName,source.BaseId))
            .Select(group=>
            {
                var first=group.First();
                return new MapLocalGatheringPoint(first.BaseId,first.TypeName,first.Level,first.Place,group.Select(source=>source.ItemId).Distinct().Order().ToArray(),
                    group.Any(source=>source.IsHidden),first.IsFishing ? null:index.Gathering(first.BaseId));
            })
            .GroupBy(point=>point.Type.Length>0 ? point.Type:"—")
            .OrderBy(group=>group.Key,StringComparer.CurrentCultureIgnoreCase)
            .Select(group=>(group.Key,(IReadOnlyList<MapLocalGatheringPoint>)group.OrderBy(point=>point.Level).ThenBy(point=>point.Place,StringComparer.CurrentCultureIgnoreCase).ToArray()))
            .ToArray();
    }
}
