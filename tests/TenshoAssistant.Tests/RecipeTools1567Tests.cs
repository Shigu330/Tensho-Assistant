// 0.1567 (X-599, the user's report of 18/11): in the item's recipes tab, the related tool « Recipes, simulation and craft macros » led back to
// that same tab (a dead button). It opens the craft simulation on the item's recipe now, and is not shown for an item without a recipe.
internal static class RecipeTools1567Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Body(string file, string head)
    {
        var code = File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n", "\n");
        var start = code.IndexOf(head, StringComparison.Ordinal);
        Check(start >= 0, head + " not found");
        var end = code.IndexOf("\n    }\n", start, StringComparison.Ordinal);
        return code[start..end];
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Recipe tools 1567: no related tool of the recipes tab leads back to the recipes tab", () =>
        {
            var tools = Body("src/TenshoAssistant/MainWindow.Items.cs", "private void DrawRecipeTools()");
            var open = Body("src/TenshoAssistant/MainWindow.Tools.cs", "private void OpenContextTool(string reference)");
            // Every tool the recipes tab lists, and the tab each leads to through OpenContextTool.
            foreach (var reference in new[] { "Craftimizer", "Item Use", "GatherBuddy", "Price Insight", "Monster Loot Hunter" })
            {
                Check(tools.Contains("\"" + reference + "\""), reference + " is no longer listed");
                var line = open.Split('\n').FirstOrDefault(row => row.Contains("case \"" + reference + "\":")) ?? "";
                var leadsBack = line.Contains("focusCraftTab = true");
                Check(!leadsBack || tools.Contains("if (tool.Reference == \"" + reference + "\" && simulated is not null) OpenCraftSimulation(simulated);"),
                    reference + " leads back to the recipes tab where it is drawn");
            }
        }),
        ("Recipe tools 1567: the simulation button needs the item's recipe, and is hidden without one", () =>
        {
            var tools = Body("src/TenshoAssistant/MainWindow.Items.cs", "private void DrawRecipeTools()");
            Check(tools.Contains("var simulated = recipeCatalog.State.Catalog?.ForResult((uint)itemId).FirstOrDefault();"), "the item's recipe is not looked up");
            Check(tools.Contains("if (tool.Reference == \"Craftimizer\" && simulated is null) continue;"), "shown for an item without a recipe");
        }),
    ];
}
