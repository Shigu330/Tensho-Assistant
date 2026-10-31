using System.Collections.Frozen;

namespace TenshoAssistant.Gathering;

public sealed record GatheringSource(uint ItemId, uint PointId, uint BaseId, uint TypeId, string TypeName,
    uint IconId, int Level, uint TerritoryId, string Zone, string Place, uint MapId = 0, bool IsFishing = false, bool IsHidden = false,
    GatheringSchedule? Schedule = null)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public GatheringSchedule Timing => Schedule ?? GatheringSchedule.Unknown;
}

public sealed class GatheringCatalog
{
    private readonly FrozenDictionary<uint, IReadOnlyList<GatheringSource>> byItem;
    private readonly FrozenDictionary<uint,GatheringSource> fishingSpots;
    public string Source { get; }
    public int Count { get; }
    public IReadOnlyList<GatheringSource> Sources { get; }
    public FishingNotebookIndex Fishing {get;}
    public GatheringCatalog(IEnumerable<GatheringSource> rows, string source, CancellationToken cancellation = default,IEnumerable<FishingNotebookEntry>? fishing=null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        var groups = new Dictionary<uint, List<GatheringSource>>();
        var seen = new HashSet<(uint Item, uint Base, uint Territory, string Place, bool Fishing, bool Hidden, string Schedule)>();
        foreach (var row in rows)
        {
            cancellation.ThrowIfCancellationRequested();
            if (row.ItemId == 0 || row.PointId == 0 || row.BaseId == 0 || row.TerritoryId == 0
                || string.IsNullOrWhiteSpace(row.Zone)) continue;
            if (!seen.Add((row.ItemId, row.BaseId, row.TerritoryId, row.Place, row.IsFishing, row.IsHidden, row.Timing.Key))) continue;
            if (!groups.TryGetValue(row.ItemId, out var list)) groups[row.ItemId] = list = [];
            list.Add(row);
        }
        byItem = groups.ToFrozenDictionary(pair => pair.Key,
            pair => (IReadOnlyList<GatheringSource>)Array.AsReadOnly(pair.Value.OrderBy(row => row.TypeId)
                .ThenBy(row => row.Level).ThenBy(row => row.Zone, StringComparer.Ordinal).ThenBy(row => row.BaseId).ToArray()));
        Count = byItem.Values.Sum(rows => rows.Count);
        Sources = Array.AsReadOnly(byItem.Values.SelectMany(rows => rows).ToArray());
        fishingSpots=Sources.Where(row=>row.IsFishing).GroupBy(row=>row.PointId).ToFrozenDictionary(group=>group.Key,group=>group.First());
        Source = source;
        Fishing=new(fishing ?? [],cancellation);
    }
    public IReadOnlyList<GatheringSource> ForItem(uint itemId) => byItem.GetValueOrDefault(itemId) ?? [];
    public GatheringSource? ForFishingSpot(uint pointId)=>fishingSpots.GetValueOrDefault(pointId);
}

public sealed record GatheringCatalogState(GatheringCatalog? Catalog, int Copied, int Total, string? Error);

public sealed class GatheringCatalogService
{
    private GatheringCatalogState state = new(null, 0, 0, null);
    public GatheringCatalogState State => Volatile.Read(ref state);
    internal void Progress(int copied, int total) => Interlocked.Exchange(ref state, new(null, copied, total, null));
    internal void Publish(GatheringCatalog catalog) => Interlocked.Exchange(ref state, new(catalog, 0, 0, null));
    internal void Fail(Exception error) => Interlocked.Exchange(ref state, new(null, 0, 0, error.Message));
    internal void Clear() => Interlocked.Exchange(ref state, new(null, 0, 0, null));
}
