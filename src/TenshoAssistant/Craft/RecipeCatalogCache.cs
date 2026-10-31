using TenshoAssistant.Caching;

namespace TenshoAssistant.Craft;

/// <summary>Disk copy of managed recipe rows, reused until the client data, the language or the copy shape changes.</summary>
public static class RecipeCatalogCache
{
    public const string Family="catalog-recipes";
    public const int MaximumRecipes=100_000;
    // Bump when the Lumina → RecipeDefinition copy rules change without a change of record shape.
    private const int CopyRules=1;
    public static readonly string Shape=CatalogCacheKeys.Shape(typeof(RecipeDefinition),typeof(RecipeIngredient),typeof(CraftRecipeParameters));
    public static string Version(string dataVersion)=>CatalogCacheKeys.Version(dataVersion,CopyRules,Shape);
    public static CatalogCacheData<RecipeDefinition> Snapshot(string dataVersion,IEnumerable<RecipeDefinition> recipes)
        =>new(dataVersion,Array.AsReadOnly(recipes.Select(recipe=>recipe with{Ingredients=Array.AsReadOnly(recipe.Ingredients.ToArray())}).ToArray()));

    public static bool Valid(CatalogCacheData<RecipeDefinition>? data,string dataVersion)
    {
        if(data is null || data.DataVersion!=dataVersion || data.Rows is null || data.Rows.Count is <1 or >MaximumRecipes)return false;
        var seen=new HashSet<uint>();
        foreach(var recipe in data.Rows)
        {
            if(recipe is null || recipe.RecipeId==0 || recipe.ResultItemId==0 || recipe.ResultAmount<=0 || recipe.CraftType is null
                || recipe.Ingredients is null || !seen.Add(recipe.RecipeId)
                || recipe.Ingredients.Any(ingredient=>ingredient is null || ingredient.ItemId==0 || ingredient.Amount<=0))return false;
        }
        return true;
    }
}
