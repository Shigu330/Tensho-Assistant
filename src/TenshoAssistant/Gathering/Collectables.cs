using System.Globalization;
using System.Text;

namespace TenshoAssistant.Gathering;

/// <summary>
/// R-06 (0.409): collectable thresholds and rewards of an item, from the game's CollectablesShopItem rows (CollectablesShopRefine:
/// low / mid / high collectability; CollectablesShopRewardScrip: reward of each tier). Scrips: the row belongs to a shop whose
/// RewardType is 1 (scrip exchanges; RewardType 2 shops give items). The scrip's kind (Currency column) is not mapped: unknown, not shown.
/// Evidence: docs/evidence/0.409-collectables.md.
/// </summary>
public sealed record CollectableRule(uint ItemId,string Name,int LevelMin,int LevelMax,int Low,int Mid,int High,int RewardLow,int RewardMid,int RewardHigh,bool Scrips,string Group="");

public enum CollectableTier { Below, Low, Mid, High }

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
    public IReadOnlyList<CollectableRule> Named(string? shown)=>byName.GetValueOrDefault(Collectables.CleanName(shown)) ?? [];
}

/// <summary>What Tensho read in the game's Collectable window (AddonGatheringMasterpiece): texts as shown, numbers when they parse.</summary>
public sealed record CollectableWindowFrame(string ItemName,string? Chance,int? Gp,int? MaxGp,int? Integrity,int? MaxIntegrity,
    IReadOnlyList<string> RawValues,float X,float Y,float Width,DateTimeOffset ObservedAt);

public static class Collectables
{
    /// <summary>
    /// A-01 (0.419): the quality a crafted collectable needs for a collectability (collectability = quality / 10, rounded down: the
    /// game's rule as Teamcraft (MIT) and Craftimizer code it, read only), capped at the recipe's maximum quality.
    /// </summary>
    public static int QualityFor(int collectability,int maximumQuality)=>(int)Math.Clamp(collectability*10L,0,Math.Max(0,maximumQuality));

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
