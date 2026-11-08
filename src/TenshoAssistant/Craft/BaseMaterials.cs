namespace TenshoAssistant.Craft;

/// <summary>W-09 (0.699): where an item comes from, for the on-screen lists. What several sources give goes under the first of these.</summary>
public enum ItemOrigin { Gathering,Vendor,Monster,Other,Crystal }

public static class BaseMaterials
{
    /// <summary>How deep recipes are followed; the recursion stops here whatever the game's data holds.</summary>
    public const int MaxDepth=8;

    /// <summary>0.701 (user request): an offer counts when the player ticked every currency it costs. None ticked at the start; an offer with no cost read, or a currency the game encodes (0), does not count.</summary>
    public static bool VendorCounts(bool vendorsOn,IEnumerable<IReadOnlyList<uint>> offersCurrencies,IReadOnlySet<uint> currenciesOn)=>
        vendorsOn && offersCurrencies.Any(currencies=>currencies.Count>0 && currencies.All(currenciesOn.Contains));

    /// <summary>0.702 (user request): the currencies of what vendors sell that is really a material: an ingredient of a recipe or a supply of a workshop project. Encoded currencies (0) stay out.</summary>
    public static IReadOnlySet<uint> MaterialCurrencies(IEnumerable<(uint Item,IEnumerable<uint> Currencies)> sales,IReadOnlySet<uint> materials)=>
        sales.Where(sale=>materials.Contains(sale.Item)).SelectMany(sale=>sale.Currencies).Where(id=>id!=0).ToHashSet();

    /// <summary>0.703 (user request): a requirement blocks an offer only when it is known not to be met: not read is not « not met ».</summary>
    public static bool RequirementBlocks(bool? quest,bool? achievement)=>quest==false || achievement==false;

    public static ItemOrigin OriginOf(bool gathered,bool sold,bool dropped)=>gathered ? ItemOrigin.Gathering:sold ? ItemOrigin.Vendor:dropped ? ItemOrigin.Monster:ItemOrigin.Other;

    /// <summary>
    /// What has to be fetched for a list: a crafted item not in stock is replaced by its ingredients, unless it can be gathered or bought (<paramref name="fetched"/>). Crafted items in stock
    /// are used first, all from one stock. Base items keep their whole need, so that the list shows their stock against it. Each once, in the order it first comes.
    /// </summary>
    /// <summary>
    /// 0.752 (X-05, user request): the gathered lines of a list, by zone, for fewer trips: the zone where the most remaining items grow comes first and takes them,
    /// then the next among what is left. An item with no zone goes last, under an empty name. Zones of equal weight keep their name order.
    /// </summary>
    public static IReadOnlyList<(string Zone,IReadOnlyList<T> Lines)> ByZone<T>(IReadOnlyList<T> lines,Func<T,IReadOnlyCollection<string>> zonesOf)
    {
        var left=lines.Select((line,index)=>(Line:line,Index:index,Zones:zonesOf(line))).ToList();
        var groups=new List<(string,IReadOnlyList<T>)>();
        while(left.Any(entry=>entry.Zones.Count>0))
        {
            var best=left.SelectMany(entry=>entry.Zones.Distinct()).GroupBy(zone=>zone)
                .OrderByDescending(group=>group.Count()).ThenBy(group=>group.Key,StringComparer.CurrentCulture).First().Key;
            var taken=left.Where(entry=>entry.Zones.Contains(best)).ToArray();
            groups.Add((best,taken.OrderBy(entry=>entry.Index).Select(entry=>entry.Line).ToArray()));
            left.RemoveAll(entry=>entry.Zones.Contains(best));
        }
        if(left.Count>0)groups.Add(("",left.Select(entry=>entry.Line).ToArray()));
        return groups;
    }

    /// <summary>
    /// 0.757 (X-12, user request): the zones of a list in the order to go: the one the character stands in first, then the cheapest to teleport to, then those whose
    /// price is unknown; within each, the one with the most lines first, then by name.
    /// </summary>
    public static IReadOnlyList<(string Zone,IReadOnlyList<T> Lines)> OrderZones<T>(IReadOnlyList<(string Zone,IReadOnlyList<T> Lines)> groups,string? here,Func<string,uint?> cost)=>
        groups.OrderBy(group=>here is { Length:>0 } && group.Zone==here ? 0:1)
            .ThenBy(group=>cost(group.Zone) is null ? 1:0).ThenBy(group=>cost(group.Zone) ?? 0)
            .ThenByDescending(group=>group.Lines.Count).ThenBy(group=>group.Zone,StringComparer.CurrentCulture).ToArray();

    /// <summary>0.789 (X-30): one craft still to do: the item, its recipe, how many times, and its step (1 first: made before what uses it).</summary>
    public sealed record CraftStep(uint Item,RecipeDefinition Recipe,long Runs,int Step)
    {
        /// <summary>0.790 (X-31): how many the lists ask for in all, stock included, shown as « held / needed » like the other lines.</summary>
        public long Needed { get; init; }
    }

    /// <summary>
    /// 0.789 (X-30, user request: see the crafting steps left for the workshop and crafting lists): the crafts Expand goes through, same rules (stock taken first,
    /// gathered or sold items not crafted), counted per item. An item's step comes after every ingredient it is crafted from.
    /// </summary>
    public static IReadOnlyList<CraftStep> Crafts(IEnumerable<(uint Item,long Amount)> wanted,Func<uint,long> stock,Func<uint,RecipeDefinition?> recipeOf,Func<uint,bool> fetched)
    {
        var runs=new Dictionary<uint,long>();var asked=new Dictionary<uint,long>();var level=new Dictionary<uint,int>();var recipes=new Dictionary<uint,RecipeDefinition>();var used=new Dictionary<uint,long>();var open=new HashSet<uint>();
        void Add(uint item,long amount,int depth)
        {
            if(amount<=0 || fetched(item) || depth<=0 || recipeOf(item) is not { Ingredients.Count:>0 } recipe || recipe.ResultAmount<=0 || open.Contains(item))return;
            asked[item]=asked.GetValueOrDefault(item)+amount;
            var free=Math.Max(0,stock(item)-used.GetValueOrDefault(item));
            var taken=Math.Min(free,amount);
            used[item]=used.GetValueOrDefault(item)+taken;
            amount-=taken;
            if(amount==0)return;
            var count=(amount+recipe.ResultAmount-1)/recipe.ResultAmount;
            runs[item]=runs.GetValueOrDefault(item)+count;recipes[item]=recipe;
            level[item]=Math.Max(level.GetValueOrDefault(item),MaxDepth-depth);
            open.Add(item);
            foreach(var ingredient in recipe.Ingredients)Add(ingredient.ItemId,ingredient.Amount*count,depth-1);
            open.Remove(item);
        }
        foreach(var (item,amount) in wanted)Add(item,amount,MaxDepth);
        var deepest=level.Values.DefaultIfEmpty(0).Max();
        return runs.Select(pair=>new CraftStep(pair.Key,recipes[pair.Key],pair.Value,deepest-level[pair.Key]+1){Needed=asked[pair.Key]}).OrderBy(step=>step.Step).ToArray();
    }

    public static IReadOnlyList<(uint Item,long Needed)> Expand(IEnumerable<(uint Item,long Amount)> wanted,Func<uint,long> stock,Func<uint,RecipeDefinition?> recipeOf,Func<uint,bool> fetched)
    {
        var needed=new Dictionary<uint,long>();var order=new List<uint>();var used=new Dictionary<uint,long>();var open=new HashSet<uint>();
        void Base(uint item,long amount)
        {
            if(!needed.ContainsKey(item)){needed[item]=0;order.Add(item);}
            needed[item]+=amount;
        }
        void Add(uint item,long amount,int depth)
        {
            if(amount<=0)return;
            if(fetched(item) || depth<=0 || recipeOf(item) is not { Ingredients.Count:>0 } recipe || recipe.ResultAmount<=0 || open.Contains(item)){Base(item,amount);return;}
            var free=Math.Max(0,stock(item)-used.GetValueOrDefault(item));
            var taken=Math.Min(free,amount);
            used[item]=used.GetValueOrDefault(item)+taken;
            // 0.838 (X-65, user report: the birch syrup held, 157/1, was in no group, so neither seen nor marked as kept by a retainer): an ingredient that is
            // crafted but taken from the stock stays a line, for the part taken; the list's own items are left as before.
            // 0.841 (X-65, log « List panel CraftList »: the crafting list hands its direct ingredients, the syrup among them, at the first level): every level.
            if(taken>0)Base(item,taken);
            amount-=taken;
            if(amount==0)return;
            var runs=(amount+recipe.ResultAmount-1)/recipe.ResultAmount;
            open.Add(item);
            foreach(var ingredient in recipe.Ingredients)Add(ingredient.ItemId,ingredient.Amount*runs,depth-1);
            open.Remove(item);
        }
        foreach(var (item,amount) in wanted)Add(item,amount,MaxDepth);
        return order.Select(item=>(item,needed[item])).ToArray();
    }
}
