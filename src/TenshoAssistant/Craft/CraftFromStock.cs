using TenshoAssistant.Inventory;

namespace TenshoAssistant.Craft;

/// <summary>0.592 (C21): a recipe the recorded stocks can make now. LowerBound: a storage holding one of its ingredients was not seen, there may be more.</summary>
public sealed record CraftFromStockRow(RecipeDefinition Recipe,long Count,bool LowerBound);
/// <summary>Undecided: the recipes that nothing rules out but with an ingredient whose stock was never seen; they are not listed.</summary>
public sealed record CraftFromStockResult(IReadOnlyList<CraftFromStockRow> Rows,int Undecided);

public static class CraftFromStock
{
    /// <summary>Direct ingredients only: what a sub-recipe could make first is the chain's work. Highest level first, then the most crafts.</summary>
    public static CraftFromStockResult Find(IEnumerable<RecipeDefinition> recipes,Func<uint,ItemHoldingsSummary> holdings,CancellationToken cancellation=default)
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
            if(possible==0)continue;
            if(unseen){undecided++;continue;}
            rows.Add(new(recipe,possible,lowerBound));
        }
        return new(rows.OrderByDescending(row=>row.Recipe.Level).ThenByDescending(row=>row.Count).ThenBy(row=>row.Recipe.RecipeId).ToArray(),undecided);
    }

    public static CraftFromStockRow[] Filter(IEnumerable<CraftFromStockRow> rows,string? job,int minLevel,int maxLevel)
        =>rows.Where(row=>(string.IsNullOrEmpty(job) || row.Recipe.CraftType==job) && row.Recipe.Level>=minLevel && row.Recipe.Level<=maxLevel).ToArray();
}
