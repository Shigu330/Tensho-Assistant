// 0.408 (user request, 2026-11-03): the item sheet's Craft tab was too heavy. It keeps a short view (recipe, ingredients, stocks,
// links); the full preparation (quantities, cost, sub-recipe chain) is the Crafting page's « Preparation » tab, linked both ways.
internal static class CraftPreparation408Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Craft preparation: short view in Items, full preparation in a Crafting tab, linked both ways",() =>
        {
            var items=Read("MainWindow.Items.cs");
            Check(items.Contains("if (!showOutputs) DrawCraftLinks(recipe);") && !items.Contains("DrawCraftPlan("),"item sheet keeps the short view only");
            var plan=Read("MainWindow.CraftPlan.cs");
            Check(plan.Contains("OpenCraftPreparation(recipe)") && plan.Contains("focusCraftPage=CraftPageTab.Preparation"),"link to the Crafting tab");
            Check(plan.Contains("OpenCraftItem(material.ItemId)") && Read("MainWindow.CraftChain.cs").Contains("OpenCraftItem(id)"),"items named in the preparation open their sheet");
            var page=Read("MainWindow.CraftSimulation.cs");
            Check(page.Contains("###CraftPreparationTab") && page.Contains("if(preparation) { DrawCraftPlan(recipe); return; }"),"Crafting page tab");
            Check(page.IndexOf("###CraftPreparationTab",StringComparison.Ordinal) < page.IndexOf("CraftSimulation.Unsupported(recipe)",StringComparison.Ordinal),
                "the preparation does not depend on the simulator's support of the recipe");
            Check(Read("MainWindow.CraftChain.cs").Contains("craftChain.ValidateContext(character, recipe.ResultItemId,")
                && Read("MainWindow.PreparationCost.cs").Contains("world.WorldId,recipe.ResultItemId,recipe.RecipeId"),
                "chain and prices keyed by the recipe's item, not by the item selected elsewhere");
        }),
    ];
}
