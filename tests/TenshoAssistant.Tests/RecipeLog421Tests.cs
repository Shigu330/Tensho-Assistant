using TenshoAssistant.Craft;

// A-11 (0.421): recipes never crafted — read from the game, marked on the recipes, « never crafted only » in the craft book.
internal static class RecipeLog421Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Recipe log: every recipe asked, function checked, read only while asked; marker and filter",() =>
        {
            var catalog=RecipeCatalog.Build([
                new RecipeDefinition(1,100,1,"Carpenter",10,[]),new RecipeDefinition(2,101,1,"Carpenter",10,[]),new RecipeDefinition(3,100,1,"Smith",10,[]),
            ],"test");
            Check(catalog.All.Select(recipe=>recipe.RecipeId).Order().SequenceEqual([1u,2u,3u]),"all recipes");
            var reader=Read("Craft/RecipeLogReader.Dalamud.cs");
            Check(reader.Contains("MemberFunctionPointers.IsRecipeComplete==0") && reader.Contains("now.UtcTicks>Volatile.Read(ref wantedUntil)")
                && reader.Contains("public bool? WasCrafted(uint recipeId)=>Total==0 ? null:Crafted.Contains(recipeId);"),"checked, on demand, unknown when not read");
            var items=Read("MainWindow.Items.cs");
            Check(items.Contains("DrawRecipeCrafted(recipe);") && items.Contains("(!neverOnly || recipeLog!.WasCrafted(recipe.RecipeId) == false)"),"marker and filter");
        }),
    ];
}
