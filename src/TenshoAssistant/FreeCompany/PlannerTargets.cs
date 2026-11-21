namespace TenshoAssistant.FreeCompany;

/// <summary>
/// X-604 (0.1707): the targets of the loot and unlock goals for submarines, as the airships' (AirshipSectors.DropSectors, Unlocker): the items
/// that may drop on a map (the community records), the sectors of the map where one may drop, and the sector a voyage visits to move towards
/// unlocking a sector (the first of the sectors to explore, SubmarineUnlocks.Route). Pure.
/// </summary>
public static class PlannerTargets
{
    /// <summary>The items recorded in these sectors' loot, once each, by id.</summary>
    public static IReadOnlyList<uint> LootItems(IReadOnlyDictionary<uint,SectorLoot> loot,IEnumerable<uint> sectors)
        =>[.. sectors.SelectMany(id=>loot.GetValueOrDefault(id)?.Items.Where(item=>item.Total>0).Select(item=>item.ItemId) ?? []).Distinct().Order()];

    /// <summary>The sectors, among these, whose recorded loot has this item.</summary>
    public static IReadOnlyList<uint> DropSectors(IReadOnlyDictionary<uint,SectorLoot> loot,IEnumerable<uint> sectors,uint item)
        =>[.. sectors.Where(id=>loot.GetValueOrDefault(id)?.Items.Any(entry=>entry.ItemId==item && entry.Total>0)==true)];

    /// <summary>The sector to visit now to unlock the target: the first of the sectors to explore; none when unknown, open or already unlocked.</summary>
    public static IReadOnlyList<uint> UnlockVisit(SubmarineUnlockState state,IReadOnlyList<uint> explore)
        =>state==SubmarineUnlockState.Path && explore.Count>0 ? [explore[0]]:[];

    /// <summary>
    /// X-712 (review M2): the sector one vessel visits to unlock the target, from its own company's unlocked sectors (null when not read: the
    /// whole chain), with the state that says why there is none.
    /// </summary>
    public static (SubmarineUnlockState State,IReadOnlyList<uint> Visit) UnlockVisitFor(IReadOnlyDictionary<uint,SubmarineSector> sectors,
        IReadOnlyDictionary<uint,SubmarineUnlock> unlocks,uint target,IReadOnlyCollection<uint>? unlocked)
    {
        var (state,explore)=SubmarineUnlocks.Route(sectors,unlocks,target,unlocked);
        return (state,UnlockVisit(state,explore));
    }
}
