namespace TenshoAssistant.Items;

// Persisted copy of the managed item rows only; the catalogue indexes are always rebuilt from it.
public sealed record ItemCatalogCacheData(string Source,string Language,string DataVersion,IReadOnlyList<ItemDefinition> Items);

/// <summary>Keys the disk copy on game data, not on the plugin version: a client update or a new copy rule invalidates it.</summary>
public static class ItemCatalogCache
{
    public const string Family="catalog-items";
    public const int MaximumItems=200_000;
    // Bump when the Lumina → ItemDefinition copy rules change without a change of record shape.
    private const int CopyRules=1;
    public static readonly string Shape=Caching.CatalogCacheKeys.Shape(typeof(ItemDefinition),typeof(CraftToolDefinition),typeof(EquipmentSlots),typeof(ItemTraits));

    public static string Scope(ItemCatalogMetadata metadata)=>metadata.Language;
    public static string Version(ItemCatalogMetadata metadata)=>Caching.CatalogCacheKeys.Version(metadata.DataVersion,CopyRules,Shape);
    public static ItemCatalogCacheData Snapshot(ItemCatalogMetadata metadata,IEnumerable<ItemDefinition> items)
        =>new(metadata.Source,metadata.Language,metadata.DataVersion,Array.AsReadOnly(items.ToArray()));
    public static ItemCatalogMetadata FromCache(ItemCatalogMetadata metadata)=>metadata with{Source=metadata.Source+" · DataCache"};

    public static bool Valid(ItemCatalogCacheData? data,ItemCatalogMetadata metadata)
    {
        if(data is null || data.Source!=metadata.Source || data.Language!=metadata.Language || data.DataVersion!=metadata.DataVersion
            || data.Items is null || data.Items.Count is <1 or >MaximumItems)return false;
        var seen=new HashSet<uint>();
        foreach(var item in data.Items)
        {
            if(item is null || item.Id==0 || string.IsNullOrWhiteSpace(item.Name) || item.UiCategoryName is null || item.EnglishName is null || !seen.Add(item.Id)
                || (item.Equipment&~EquipmentSlots.All)!=0 || item.CraftTool is { JobId: <8 or >15 }
                || (item.Traits&~ItemTraits.All)!=0 || item.EquipLevel is <0 or >255 || item.Rarity is <0 or >255)return false;
        }
        return true;
    }
}
