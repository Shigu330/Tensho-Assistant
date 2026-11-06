namespace TenshoAssistant.Craft;

/// <summary>W-09 (0.699): where an item comes from, for the on-screen lists. What several sources give goes under the first of these.</summary>
public enum ItemOrigin { Gathering,Vendor,Monster,Other,Crystal }

public sealed record BaseMaterial(uint Item,long Needed,ItemOrigin Origin);

public static class BaseMaterials
{
    /// <summary>How deep recipes are followed; the recursion stops here whatever the game's data holds.</summary>
    public const int MaxDepth=8;

    /// <summary>0.701 (user request): an offer counts when the player ticked every currency it costs. None ticked at the start; an offer with no cost read, or a currency the game encodes (0), does not count.</summary>
    public static bool VendorCounts(bool vendorsOn,IEnumerable<IReadOnlyList<uint>> offersCurrencies,IReadOnlySet<uint> currenciesOn)=>
        vendorsOn && offersCurrencies.Any(currencies=>currencies.Count>0 && currencies.All(currenciesOn.Contains));

    public static ItemOrigin OriginOf(bool gathered,bool sold,bool dropped)=>gathered ? ItemOrigin.Gathering:sold ? ItemOrigin.Vendor:dropped ? ItemOrigin.Monster:ItemOrigin.Other;

    /// <summary>
    /// What has to be fetched for a list: a crafted item not in stock is replaced by its ingredients, unless it can be gathered or bought (<paramref name="fetched"/>). Crafted items in stock
    /// are used first, all from one stock. Base items keep their whole need, so that the list shows their stock against it. Each once, in the order it first comes.
    /// </summary>
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
