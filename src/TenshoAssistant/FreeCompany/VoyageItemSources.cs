namespace TenshoAssistant.FreeCompany;

/// <summary>
/// X-610 (0.1568, the user's report of 19/11: Alchemic Fibre, item 12926, said « no known source » although Tensho carries the airship
/// and submarine loot): the voyages where an item may be found, for the « How to get it » part of the item sheet. Airships: LuminaSupplemental
/// AirshipDrop (embedded, airship-drop.csv); submarines: the community loot records (embedded, submarine-loot.json). The sectors' names are the
/// game's (AirshipExplorationPoint, SubmarineExploration and SubmarineMap sheets, read by VoyageWatcher). Pure.
/// </summary>
public static class VoyageItemSources
{
    /// <summary>The airship sectors where the item may drop, by their name in the game, in id order; empty when none is known.</summary>
    public static IReadOnlyList<string> Airships(AirshipCatalog? catalog,uint item)
        =>catalog is null ? []:[.. AirshipSectors.DropSectors(catalog,item).Select(id=>catalog.Find(id)!.Name).Where(name=>name.Length>0).Distinct()];

    /// <summary>The submarine sectors where the item was recorded, « sector (sea) » with the game's names, in id order; empty when none is known.</summary>
    public static IReadOnlyList<string> Submarines(SubmarineCatalog? catalog,uint item)
    {
        if(catalog is null || item==0)return [];
        var seas=catalog.Maps.ToDictionary(map=>map.Id,map=>map.Name);
        return [.. catalog.Loot.Values.Where(loot=>loot.Items.Any(row=>row.ItemId==item && row.Total>0))
            .Select(loot=>loot.SectorId).Order()
            .Select(id=>catalog.Sectors.TryGetValue(id,out var sector) && sector.Name.Length>0
                ? (seas.TryGetValue(sector.Map,out var sea) && sea.Length>0 ? $"{sector.Name} ({sea})":sector.Name):null)
            .OfType<string>().Distinct()];
    }
}
