// 0.424 (user reports): Crafting › Preparation showed « recorded: unknown »; « Back to recipes » went back to the item.
internal static class CraftReports424Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Craft reports 0.424: holdings refreshed in Preparation; « Back to the item » opens the recipe's item",() =>
        {
            var plan=Read("MainWindow.CraftPlan.cs");
            Check(plan.IndexOf("RefreshRecipeHoldingsCache();",plan.IndexOf("private void DrawCraftPlan(",StringComparison.Ordinal),StringComparison.Ordinal)
                < plan.IndexOf("CraftPlan.Build(recipe",StringComparison.Ordinal),"cache refreshed before the plan reads it");
            Check(Read("MainWindow.CraftSimulation.cs").Contains("if(simulationRecipe is { } shown)SelectItem(shown.ResultItemId); page=Page.Items;")
                && Read("UI/Navigation/MainWindow.CategoryNavigation.cs").Contains("if(simulationRecipe is { } shown)SelectItem(shown.ResultItemId);page=Page.Items;"),"both buttons");
            Check(Read("Localization/Languages/fr.json").Contains("\"Notebook\": \"Retour à l'objet\""),"named after what it does");
        }),
    ];
}
