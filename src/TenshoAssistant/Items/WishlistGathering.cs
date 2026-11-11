using TenshoAssistant.Gathering;

namespace TenshoAssistant.Items;

/// <summary>
/// X-127 (0.1106, user request of 2026-11-11): where a wished item is gathered — one place per job, zone, node and hours (the game's gathering
/// data, Gathering/GatheringCatalog.cs), the timed places first — and the wishes that still want an alarm. No game state and no interface here.
/// </summary>
public static class WishlistGathering
{
    /// <summary>Places shown under an item; the others are counted.</summary>
    public const int Shown=3;

    /// <param name="Node">The node's place name; empty when the game data does not name it.</param>
    public sealed record Place(uint TypeId,string TypeName,string Zone,string Node,bool IsFishing,bool IsHidden,GatheringSchedule Timing,uint TerritoryId);

    /// <summary>The item's places, each once (same job, zone, node and hours), the timed ones first, then in the catalogue's order.</summary>
    public static IReadOnlyList<Place> Places(IEnumerable<GatheringSource> sources)
        =>sources.Select(source=>new Place(source.TypeId,source.TypeName,source.Zone,string.IsNullOrWhiteSpace(source.Place) ? "":source.Place.Trim(),
                source.IsFishing,source.IsHidden,source.Timing,source.TerritoryId))
            .DistinctBy(place=>(place.TypeId,place.Zone,place.Node,place.IsFishing,place.IsHidden,place.Timing.Key))
            .Select((place,index)=>(place,index))
            .OrderBy(pair=>pair.place.Timing.Kind==GatheringScheduleKind.Limited ? 0:1).ThenBy(pair=>pair.index)
            .Select(pair=>pair.place).ToArray();

    /// <summary>The wishes still to get, once each, as (item, 1) lines for the list alarms (MainWindow.ListPanels.cs, ListAlarms: only the items
    /// found at timed points alone, an alarm already there not added again).</summary>
    public static (uint Item,long Needed)[] AlarmLines(IEnumerable<uint>? wishes,Func<uint,bool> obtained)
        =>(wishes ?? []).Where(id=>id!=0).Distinct().Where(id=>!obtained(id)).Select(id=>(id,1L)).ToArray();
}
