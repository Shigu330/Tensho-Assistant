using System.Globalization;
using System.Text;

namespace TenshoAssistant.Gathering;

/// <summary>
/// R-06 (0.409): collectable thresholds and rewards of an item, from the game's CollectablesShopItem rows (CollectablesShopRefine:
/// low / mid / high collectability; CollectablesShopRewardScrip: reward of each tier). Scrips: the row belongs to a shop whose
/// RewardType is 1 (scrip exchanges; RewardType 2 shops give items). The scrip's kind (Currency column) is not mapped: unknown, not shown.
/// Evidence: docs/evidence/0.409-collectables.md.
/// </summary>
public sealed record CollectableRule(uint ItemId,string Name,int LevelMin,int LevelMax,int Low,int Mid,int High,int RewardLow,int RewardMid,int RewardHigh,bool Scrips,string Group="",int ScripKind=-1);

public enum CollectableTier { Below, Low, Mid, High }

/// <summary>0.523 (user report): which of a node's collectables pays the most, at the high tier. Only between items of the same kind of scrip (the sheet's Currency column): the kinds are not worth the same, and their names are not known here.</summary>
public static class CollectableChoice
{
    public static bool MixedKinds(IEnumerable<CollectableRule> rules)=>rules.Select(rule=>rule.ScripKind).Distinct().Count()>1;
    /// <summary>The item, or null: fewer than two items, a tie, or different kinds.</summary>
    public static uint? Best(IReadOnlyCollection<CollectableRule> rules)
    {
        if(rules.Count<2 || MixedKinds(rules))return null;
        var top=rules.Max(rule=>rule.RewardHigh);
        var best=rules.Where(rule=>rule.RewardHigh==top).Select(rule=>rule.ItemId).Distinct().ToArray();
        return top>0 && best.Length==1 ? best[0]:null;
    }
}

public sealed class CollectableCatalog
{
    private readonly Dictionary<uint,CollectableRule[]> byItem;
    private readonly Dictionary<string,CollectableRule[]> byName;
    public int Count { get; }
    /// <summary>R-11 (0.413): every rule kept, by level then name, for the collectables table.</summary>
    public IReadOnlyList<CollectableRule> All { get; }
    public CollectableCatalog(IEnumerable<CollectableRule> rules)
    {
        var kept=rules.Where(rule=>rule.ItemId!=0 && rule.Low>0 && rule.Low<=rule.Mid && rule.Mid<=rule.High).ToArray();
        Count=kept.Length;
        All=Array.AsReadOnly(kept.OrderBy(rule=>rule.LevelMin).ThenBy(rule=>rule.Name,StringComparer.CurrentCultureIgnoreCase).ToArray());
        byItem=kept.GroupBy(rule=>rule.ItemId).ToDictionary(group=>group.Key,group=>group.ToArray());
        byName=kept.Where(rule=>Collectables.CleanName(rule.Name).Length>0)
            .GroupBy(rule=>Collectables.CleanName(rule.Name),StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group=>group.Key,group=>group.ToArray(),StringComparer.OrdinalIgnoreCase);
    }
    public IReadOnlyList<CollectableRule> For(uint itemId)=>byItem.GetValueOrDefault(itemId) ?? [];
    /// <summary>The rules of the item named in the game's Collectable window (its name as shown, icons removed).</summary>
    public IReadOnlyList<CollectableRule> Named(string? shown)
    {
        var name=Collectables.CleanName(shown).Trim();
        if(byName.TryGetValue(name,out var exact))return exact;
        // 0.478 (in-game report, docs/evidence/0.478-collectable-truncated-name.md): the game's window cuts long names; the start of a name counts only if it names one item.
        var cut=name.EndsWith("...",StringComparison.Ordinal) ? name[..^3]:name.EndsWith('…') ? name[..^1]:null;
        if(cut is null || cut.Trim().Length<4)return [];
        var found=byName.Where(row=>row.Key.StartsWith(cut,StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        return found.Length==1 ? found[0].Value:[];
    }
}

/// <summary>What Tensho read in the game's Collectable window (AddonGatheringMasterpiece): texts as shown, numbers when they parse.</summary>
/// <summary>0.816 (X-51): where the window shows an icon (screen pixels); a zero size: the window does not show it.</summary>
public sealed record CollectableIconRect(uint Icon,float X,float Y,float Width,float Height);
public sealed record CollectableWindowFrame(string ItemName,string? Chance,int? Gp,int? MaxGp,int? Integrity,int? MaxIntegrity,
    IReadOnlyList<string> RawValues,float X,float Y,float Width,DateTimeOffset ObservedAt);

/// <summary>A-15 (0.432): a collectable held in the bags (base item, collectability as the game stores it).</summary>
public sealed record HeldCollectable(uint ItemId,int Collectability);
/// <summary>Its turn-in value: the scrip rule used, the tier reached and the scrips of that tier (0 below the low threshold).</summary>
public sealed record CollectableValue(HeldCollectable Held,CollectableRule Rule,CollectableTier Tier,int Scrips);

public static class Collectables
{
    /// <summary>A-15 (0.432): the value of a held collectable by the first scrip rule of its item; null when the game takes none.</summary>
    public static CollectableValue? Value(CollectableCatalog catalog,HeldCollectable held)
    {
        if(catalog.For(held.ItemId).FirstOrDefault(rule=>rule.Scrips) is not { } rule)return null;
        var tier=Tier(rule,held.Collectability);
        return new(held,rule,tier,tier switch{CollectableTier.High=>rule.RewardHigh,CollectableTier.Mid=>rule.RewardMid,CollectableTier.Low=>rule.RewardLow,_=>0});
    }

    /// <summary>
    /// A-01 (0.419): the quality a crafted collectable needs for a collectability (collectability = quality / 10, rounded down: the
    /// game's rule as Teamcraft (MIT) and Craftimizer code it, read only), capped at the recipe's maximum quality.
    /// </summary>
    public static int QualityFor(int collectability,int maximumQuality)=>(int)Math.Clamp(collectability*10L,0,Math.Max(0,maximumQuality));

    /// <summary>0.478 (user request): the table's columns: 0 item, 1 group, 2 level, 3 high threshold, 4 high reward.</summary>
    public static CollectableRule[] Sorted(IEnumerable<CollectableRule> rules,int column,bool descending)
    {
        IEnumerable<CollectableRule> Order<T>(Func<CollectableRule,T> key)=>descending ? rules.OrderByDescending(key):rules.OrderBy(key);
        return (column switch
        {
            0=>rules.Order(Comparer<CollectableRule>.Create((a,b)=>(descending ? -1:1)*string.Compare(a.Name,b.Name,StringComparison.CurrentCultureIgnoreCase))),
            1=>rules.Order(Comparer<CollectableRule>.Create((a,b)=>(descending ? -1:1)*string.Compare(a.Group,b.Group,StringComparison.CurrentCultureIgnoreCase))),
            3=>Order(rule=>rule.High),
            4=>Order(rule=>rule.Scrips ? rule.RewardHigh:-1),
            _=>Order(rule=>rule.LevelMin),
        }).ToArray();
    }

    /// <summary>R-11 (0.413): rules whose item or group name holds every word of the query (case ignored).</summary>
    public static CollectableRule[] Filter(IEnumerable<CollectableRule> rules,string? query)
    {
        var words=(query ?? "").Split(' ',StringSplitOptions.RemoveEmptyEntries);
        return rules.Where(rule=>words.All(word=>rule.Name.Contains(word,StringComparison.CurrentCultureIgnoreCase)
            || rule.Group.Contains(word,StringComparison.CurrentCultureIgnoreCase))).ToArray();
    }

    public static CollectableTier Tier(CollectableRule rule,int collectability)
        =>collectability>=rule.High ? CollectableTier.High : collectability>=rule.Mid ? CollectableTier.Mid : collectability>=rule.Low ? CollectableTier.Low : CollectableTier.Below;

    /// <summary>A whole number shown by the game (« 1,234 », « 1 234 », « 600 »); null when the text holds anything else.</summary>
    public static int? Number(string? text)
    {
        if(string.IsNullOrWhiteSpace(text))return null;
        var digits=new StringBuilder();
        foreach(var character in text.Trim())
        {
            if(char.IsAsciiDigit(character))digits.Append(character);
            else if(character is ',' or '.' or ' ' or ' ' or ' ' or '\'')continue;
            else return null;
        }
        return digits.Length is > 0 and <= 9 ? int.Parse(digits.ToString(),CultureInfo.InvariantCulture):null;
    }

    /// <summary>An item name without the game's private-use icons (collectable mark…) and extra spaces, to compare names.</summary>
    public static string CleanName(string? name)
    {
        var text=new StringBuilder();
        foreach(var character in name ?? "")
        {
            if(character is >= '' and <= '' || char.IsControl(character))continue;
            text.Append(char.IsWhiteSpace(character) ? ' ':character);
        }
        return string.Join(' ',text.ToString().Split(' ',StringSplitOptions.RemoveEmptyEntries));
    }
}
