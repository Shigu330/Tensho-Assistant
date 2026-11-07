using TenshoAssistant.Inventory;

namespace TenshoAssistant.Craft;

/// <summary>0.592 (C21): a recipe the recorded stocks can make now. LowerBound: a storage holding one of its ingredients was not seen, there may be more.</summary>
public sealed record CraftFromStockRow(RecipeDefinition Recipe,long Count,bool LowerBound,bool Chained=false);
/// <summary>Undecided: the recipes that nothing rules out but with an ingredient whose stock was never seen; they are not listed.</summary>
public sealed record CraftFromStockResult(IReadOnlyList<CraftFromStockRow> Rows,int Undecided);

public static class CraftFromStock
{
    /// <summary>How deep sub-recipes are followed; the recursion below stops here whatever the game's data holds.</summary>
    public const int MaxDepth=8;
    public const long MaxChained=9999;

    /// <summary>
    /// 0.696 (user request): how many crafts the stocks allow when what is missing is made first, ore to ingot to part, all drawing on one shared stock. Each item uses the
    /// recipe <paramref name="recipeOf"/> gives. A lower count than the game might reach: leftovers of a sub-craft are not reused, and other recipes of an item are not tried.
    /// </summary>
    public static long ChainedCount(RecipeDefinition recipe,Func<uint,ItemHoldingsSummary> stock,Func<uint,RecipeDefinition?> recipeOf,long atLeast=0)
    {
        bool Can(long crafts)
        {
            var used=new Dictionary<uint,long>();
            var open=new HashSet<uint>{recipe.ResultItemId};
            bool Need(uint item,long amount,int depth)
            {
                var held=stock(item);
                var free=Math.Max(0,(held.HasObservation ? held.Total:0)-used.GetValueOrDefault(item));
                var taken=Math.Min(free,amount);
                used[item]=used.GetValueOrDefault(item)+taken;
                amount-=taken;
                if(amount==0)return true;
                if(depth<=0 || recipeOf(item) is not { Ingredients.Count:>0 } sub || sub.ResultAmount<=0 || !open.Add(item))return false;
                var runs=(amount+sub.ResultAmount-1)/sub.ResultAmount;
                var done=sub.Ingredients.All(ingredient=>ingredient.Amount>0 && Need(ingredient.ItemId,ingredient.Amount*runs,depth-1));
                open.Remove(item);
                return done;
            }
            return recipe.Ingredients.All(ingredient=>ingredient.Amount>0 && Need(ingredient.ItemId,ingredient.Amount*crafts,MaxDepth));
        }
        var low=Math.Max(0,atLeast);
        if(!Can(low+1))return low;
        var high=low+1;
        while(high<MaxChained && Can(Math.Min(high*2,MaxChained)))high=Math.Min(high*2,MaxChained);
        if(high>=MaxChained)return MaxChained;
        var top=Math.Min(high*2,MaxChained);
        while(top-high>1)
        {
            var middle=high+(top-high)/2;
            if(Can(middle))high=middle;else top=middle;
        }
        return high;
    }

    /// <summary>Direct ingredients, and sub-recipes when <paramref name="recipeOf"/> is given. Highest level first, then the most crafts.</summary>
    public static CraftFromStockResult Find(IEnumerable<RecipeDefinition> recipes,Func<uint,ItemHoldingsSummary> holdings,CancellationToken cancellation=default,Func<uint,RecipeDefinition?>? recipeOf=null)
    {
        ArgumentNullException.ThrowIfNull(recipes);
        ArgumentNullException.ThrowIfNull(holdings);
        var seen=new Dictionary<uint,ItemHoldingsSummary>();
        ItemHoldingsSummary Stock(uint item)
        {
            if(!seen.TryGetValue(item,out var summary))seen[item]=summary=holdings(item);
            return summary;
        }
        var rows=new List<CraftFromStockRow>();var undecided=0;
        foreach(var recipe in recipes)
        {
            cancellation.ThrowIfCancellationRequested();
            if(recipe.Ingredients.Count==0)continue;
            var possible=long.MaxValue;var lowerBound=false;var unseen=false;
            foreach(var ingredient in recipe.Ingredients)
            {
                if(ingredient.Amount<=0){possible=0;break;}
                var stock=Stock(ingredient.ItemId);
                if(!stock.HasObservation){unseen=true;continue;}
                possible=Math.Min(possible,stock.Total/ingredient.Amount);
                lowerBound|=stock.HasUnknown;
                if(possible==0)break;
            }
            var direct=unseen || possible==long.MaxValue ? 0:possible;
            if(recipeOf is not null && recipe.Ingredients.All(ingredient=>ingredient.Amount>0))
            {
                var chained=ChainedCount(recipe,Stock,recipeOf,direct);
                if(chained>direct)
                {
                    rows.Add(new(recipe,chained,lowerBound || recipe.Ingredients.Any(ingredient=>Stock(ingredient.ItemId).HasUnknown),true));
                    continue;
                }
            }
            if(possible==0)continue;
            if(unseen){undecided++;continue;}
            rows.Add(new(recipe,possible,lowerBound));
        }
        return new(rows.OrderByDescending(row=>row.Recipe.Level).ThenByDescending(row=>row.Count).ThenBy(row=>row.Recipe.RecipeId).ToArray(),undecided);
    }

    /// <summary>0.709 (user request): one page of a list. A page past the end gives the last one; the page returned tells which.</summary>
    public static (T[] Rows,int Page,int Pages) Paged<T>(IReadOnlyList<T> rows,int page,int size)
    {
        var pages=Math.Max(1,(rows.Count+size-1)/size);
        var shown=Math.Clamp(page,0,pages-1);
        return (rows.Skip(shown*size).Take(size).ToArray(),shown,pages);
    }

    /// <summary>W-06 (0.707): most valuable first, unknown values last in their order. A value is what the crafts would bring, without the materials, which are already in stock.</summary>
    public static CraftFromStockRow[] Ranked(IEnumerable<CraftFromStockRow> rows,Func<CraftFromStockRow,decimal?> value)
        =>rows.Select((row,index)=>(row,index,worth:value(row))).OrderBy(entry=>entry.worth is null ? 1:0).ThenByDescending(entry=>entry.worth ?? 0).ThenBy(entry=>entry.index).Select(entry=>entry.row).ToArray();

    /// <summary>
    /// 0.725 (user request): how many of the row's pieces can sell in that many days, at the speed of the market: a stock nobody buys is worth nothing soon. Null: speed unknown, which is not zero.
    /// The best case: other sellers share these sales, and the speed is that of the past seven days.
    /// </summary>
    public static long? Sellable(CraftFromStockRow row,decimal? perDay,int days)
    {
        if(perDay is not { } speed)return null;
        var pieces=row.Count*Math.Max(1,row.Recipe.ResultAmount);
        // Rounded first: one sale in seven days comes back as 0.14285715 a day, and 7 days of it are one piece.
        return Math.Min(pieces,(long)Math.Floor(Math.Round(Math.Max(0,speed)*Math.Max(1,days),2)));
    }

    /// <summary>What the crafts of a row bring at a unit price: crafts × items per craft × price.</summary>
    public static decimal Worth(CraftFromStockRow row,decimal unit)=>row.Count*Math.Max(1,row.Recipe.ResultAmount)*unit;

    public static CraftFromStockRow[] Filter(IEnumerable<CraftFromStockRow> rows,string? job,int minLevel,int maxLevel)
        =>rows.Where(row=>(string.IsNullOrEmpty(job) || row.Recipe.CraftType==job) && row.Recipe.Level>=minLevel && row.Recipe.Level<=maxLevel).ToArray();
}
