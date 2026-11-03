namespace TenshoAssistant.Gear;

/// <summary>A chocobo barding (BuddyEquip row): name, head / body / legs icons, Grand Company (0 = any), order, and the item that teaches it.</summary>
public sealed record BardingSet(uint Id,string Name,uint IconHead,uint IconBody,uint IconLegs,uint GrandCompany,int Order,uint ItemId);

/// <summary>
/// The companion chocobo as the game holds it (CompanionInfo, copied on the framework thread): name, rank, colour (Stain row), the
/// barding worn on head, body and legs (BuddyEquip rows, 0 = none) and the bardings unlocked (Dalamud IUnlockState). Null parts = not read.
/// </summary>
public sealed record ChocoboState(string Name,int Rank,uint StainId,uint Head,uint Body,uint Legs,IReadOnlySet<uint>? Unlocked,DateTimeOffset ReadAt);

/// <summary>
/// R-11 step 4 (0.340, DEC-023 point 5): chocobo barding. Appearance only, no stats (BuddyEquip has names, models and icons): the page
/// shows what is worn, what is unlocked and which item teaches the missing ones (ItemAction 1013, Data[0] = BuddyEquip row, checked on
/// the 101 items that teach one, docs/evidence/0.340-chocobo-barding.md).
/// </summary>
public static class ChocoboBarding
{
    public const uint TeachAction=1013;

    /// <summary>Bardings to show: by name or item name (case-insensitive), optionally only the ones not unlocked (unknown counts as not shown).</summary>
    public static IReadOnlyList<BardingSet> Filter(IEnumerable<BardingSet> sets,string search,bool missingOnly,IReadOnlySet<uint>? unlocked,Func<uint,string?> itemName)
    {
        var text=search.Trim();
        return sets.Where(set=>!missingOnly || unlocked is not null && !unlocked.Contains(set.Id))
            .Where(set=>text.Length==0 || set.Name.Contains(text,StringComparison.CurrentCultureIgnoreCase)
                || set.ItemId!=0 && itemName(set.ItemId)?.Contains(text,StringComparison.CurrentCultureIgnoreCase)==true)
            .OrderBy(set=>set.Order).ThenBy(set=>set.Id).ToArray();
    }

    /// <summary>Unlocked and total (bardings with an item only: the default harness is not « collected »); null when unlocks are not read.</summary>
    public static (int Unlocked,int Total)? Progress(IEnumerable<BardingSet> sets,IReadOnlySet<uint>? unlocked)
    {
        if(unlocked is null)return null;
        var counted=sets.Where(set=>set.ItemId!=0).ToArray();
        return (counted.Count(set=>unlocked.Contains(set.Id)),counted.Length);
    }
}
