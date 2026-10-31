using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace TenshoAssistant.Items;

[Flags]
public enum ItemTraits { None=0, Unique=1, Untradable=2, Dyeable=4, Repairable=8, Collectable=16, All=Unique|Untradable|Dyeable|Repairable|Collectable }
public sealed record ItemDefinition(uint Id, string Name, bool CanBeHq, uint IconId = 0,
    uint UiCategoryId = 0, string UiCategoryName = "", bool IsMarketable = false, uint TripleTriadCardId = 0, EquipmentSlots Equipment = EquipmentSlots.None,CraftToolDefinition? CraftTool=null,uint OrchestrionId=0,
    uint ItemLevel=0,int EquipLevel=0,int Rarity=0,uint SellPrice=0,ItemTraits Traits=ItemTraits.None,string EnglishName="",
    // 0.141: collection unlocked by the item (ItemAction 1322 = mount, 853 = minion; docs/evidence/0.141.0-itemaction-probe.log).
    uint MountId=0,uint CompanionId=0);
public sealed record CraftToolDefinition(uint JobId,int? GoodQualityPercent);
public sealed record ItemCategoryDefinition(uint Id,string Name);
public static class CraftToolBonuses
{
    public const string EnhancedDescription="Increases to quality are 1.75 times higher than normal when material condition is Good.";
    public static int? FromDeclaredMetadata(string? englishDescription,uint specialBonus,byte specialParam)
    {
        if(englishDescription is null)return null;
        if(englishDescription.Contains(EnhancedDescription,StringComparison.Ordinal) && specialBonus==0 && specialParam==0)return 175;
        return englishDescription.Length==0 && specialBonus==0 && specialParam==0 ? 150:null;
    }
}
public sealed record ItemCatalogMetadata(string Source, string Language, string DataVersion, int Generation);

/// <summary>One immutable index shared by all item consumers; no Lumina row survives here.</summary>
public sealed class ItemCatalog
{
    private readonly FrozenDictionary<uint, ItemDefinition> byId;
    private readonly FrozenDictionary<uint, IReadOnlyList<ItemDefinition>> byTriadCard;
    private readonly FrozenDictionary<uint,IReadOnlyList<ItemDefinition>> byOrchestrion;
    private readonly FrozenDictionary<uint,IReadOnlyList<ItemDefinition>> byMount,byCompanion;
    private readonly (ItemDefinition Item, string SearchName)[] searchable;
    public ItemCatalogMetadata Metadata { get; }
    public int Count => byId.Count;
    public IReadOnlyList<ItemCategoryDefinition> Categories {get;}

    private ItemCatalog(ItemCatalogMetadata metadata, Dictionary<uint, ItemDefinition> items,
        (ItemDefinition Item, string SearchName)[] searchable)
    {
        Metadata = metadata;
        byId = items.ToFrozenDictionary();
        Categories=Array.AsReadOnly(items.Values.Where(item=>item.UiCategoryId!=0 && !string.IsNullOrWhiteSpace(item.UiCategoryName))
            .GroupBy(item=>item.UiCategoryId).Select(group=>new ItemCategoryDefinition(group.Key,group.First().UiCategoryName))
            .OrderBy(category=>category.Name,StringComparer.OrdinalIgnoreCase).ThenBy(category=>category.Id).ToArray());
        byTriadCard = items.Values.Where(item => item.TripleTriadCardId != 0).GroupBy(item => item.TripleTriadCardId)
            .ToFrozenDictionary(group => group.Key, group => (IReadOnlyList<ItemDefinition>)Array.AsReadOnly(group.OrderBy(item => item.Id).ToArray()));
        this.searchable = searchable;
        byOrchestrion=items.Values.Where(item=>item.OrchestrionId!=0).GroupBy(item=>item.OrchestrionId)
            .ToFrozenDictionary(group=>group.Key,group=>(IReadOnlyList<ItemDefinition>)Array.AsReadOnly(group.OrderBy(item=>item.Id).ToArray()));
        byMount=items.Values.Where(item=>item.MountId!=0).GroupBy(item=>item.MountId)
            .ToFrozenDictionary(group=>group.Key,group=>(IReadOnlyList<ItemDefinition>)Array.AsReadOnly(group.OrderBy(item=>item.Id).ToArray()));
        byCompanion=items.Values.Where(item=>item.CompanionId!=0).GroupBy(item=>item.CompanionId)
            .ToFrozenDictionary(group=>group.Key,group=>(IReadOnlyList<ItemDefinition>)Array.AsReadOnly(group.OrderBy(item=>item.Id).ToArray()));
    }

    public static ItemCatalog Build(IEnumerable<ItemDefinition> source, ItemCatalogMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(metadata);
        var items = new Dictionary<uint, ItemDefinition>();
        var names = new List<(ItemDefinition Item, string SearchName)>();
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.Id == 0 || string.IsNullOrWhiteSpace(item.Name)) continue;
            if (!items.TryAdd(item.Id, item)) throw new ArgumentException("Duplicate item ID.", nameof(source));
            names.Add((item, Normalize(item.Name)));
        }
        cancellationToken.ThrowIfCancellationRequested();
        var sorted = names.OrderBy(entry => entry.SearchName, StringComparer.Ordinal)
            .ThenBy(entry => entry.Item.Id).ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        return new ItemCatalog(metadata, items, sorted);
    }

    public ItemDefinition? Find(uint id) => byId.GetValueOrDefault(id);
    public IReadOnlyList<ItemDefinition> ForTriadCard(uint cardId) => byTriadCard.GetValueOrDefault(cardId) ?? [];
    public IReadOnlyList<ItemDefinition> ForOrchestrion(uint musicId)=>byOrchestrion.GetValueOrDefault(musicId) ?? [];
    public IReadOnlyList<ItemDefinition> ForMount(uint mountId)=>byMount.GetValueOrDefault(mountId) ?? [];
    public IReadOnlyList<ItemDefinition> ForCompanion(uint companionId)=>byCompanion.GetValueOrDefault(companionId) ?? [];

    public IReadOnlyList<ItemDefinition> Search(string query, int limit = 30)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        if (query.Length > 128 || string.IsNullOrWhiteSpace(query)) return [];
        var trimmed = query.Trim();
        if (uint.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            return Find(id) is { } item ? Array.AsReadOnly(new[] { item }) : [];

        var tokens = Normalize(trimmed).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return [];
        var results = new List<ItemDefinition>(limit);
        foreach (var entry in searchable)
        {
            if (!MatchesAll(entry.SearchName, tokens)) continue;
            results.Add(entry.Item);
            if (results.Count == limit) break;
        }
        return results.AsReadOnly();
    }

    private static bool MatchesAll(string name, string[] tokens)
    {
        foreach (var token in tokens)
            if (!name.Contains(token, StringComparison.Ordinal)) return false;
        return true;
    }

    private static string Normalize(string value)
    {
        var text = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            text.Append(char.IsWhiteSpace(character) ? ' ' : char.ToUpperInvariant(character));
        }
        return text.ToString();
    }
}
