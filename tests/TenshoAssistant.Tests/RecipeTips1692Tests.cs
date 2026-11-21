using TenshoAssistant.Craft;

// 0.1692 (X-684, eleventh part, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one recipe tooltip
// (DrawRecipeTooltip, facts in Craft/RecipeTips): crafter's icon, item made, crafter and level, numbers, minimum attributes, what it allows
// and asks, crafted before, ingredients; the craft book's list (before: crafter and level only), a list panel's crafts and the crafts from
// stock go through it; built once while hovered.
internal static class RecipeTips1692Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");
    private static CraftRecipeParameters Recipe(int durability, int progress, int quality, int craftsmanship, int control)
        => new(durability, progress, quality, 100, 100, 100, 100, craftsmanship, control, 0);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Recipe tips 1692: the numbers the game gives and the minimums it asks, 0 left out", () =>
        {
            Check(RecipeTips.Stats(Recipe(80, 3000, 7000, 0, 0)).SequenceEqual([("Durability", 80), ("Progress", 3000), ("Quality", 7000)]), "durability, progress, quality");
            Check(RecipeTips.Stats(Recipe(40, 100, 0, 0, 0)).SequenceEqual([("Durability", 40), ("Progress", 100)]), "no quality: left out");
            Check(RecipeTips.Minimums(Recipe(80, 3000, 7000, 4000, 3800)).SequenceEqual([("Craftsmanship", 4000), ("Control", 3800)]), "the minimums");
            Check(RecipeTips.Minimums(Recipe(80, 3000, 7000, 0, 0)).Count == 0, "none asked: nothing said");
        }),
        ("Recipe tips 1692: the facts: name trimmed, blank and repeated lines left out", () =>
        {
            var facts = RecipeTips.Build(" Bronze Ingot × 1 ", 62108, "Blacksmith · level 1", ["Durability 40 · Progress 20", " ", null, "Ingredients: Copper Ore × 2", "Durability 40 · Progress 20"]);
            Check(facts.Name == "Bronze Ingot × 1" && facts.Icon == 62108 && facts.Job == "Blacksmith · level 1"
                  && facts.Lines.SequenceEqual(["Durability 40 · Progress 20", "Ingredients: Copper Ore × 2"]), string.Join(" | ", facts.Lines));
            Check(RecipeTips.Build("X", 0, " ", []).Job is null, "no job: none said");
        }),
        ("Recipe tips 1692: wiring: one body; the craft book, a list panel's crafts, the crafts from stock; the facts line shared with readiness", () =>
        {
            var body = Read("src/TenshoAssistant/MainWindow.RecipeTooltip.cs");
            Check(body.Contains("private void DrawRecipeTooltip(RecipeTipFacts facts,string? after=null)", StringComparison.Ordinal) && body.Contains("using(TooltipScope.Begin())", StringComparison.Ordinal), "the body");
            Check(body.Contains("cached.Recipe==recipe.RecipeId && cached.Revision==localization.Revision", StringComparison.Ordinal), "built once while hovered");
            Check(body.Contains("lines.Add(RecipeFactsText(p));", StringComparison.Ordinal) && body.Contains("\"RecipeUi.Crafted\":\"RecipeUi.NeverCrafted\"", StringComparison.Ordinal), "facts and crafted before");
            Check(Read("src/TenshoAssistant/MainWindow.CraftReadiness.cs").Contains("ImGui.TextDisabled(RecipeFactsText(p));", StringComparison.Ordinal), "readiness shares the facts line");
            Check(Read("src/TenshoAssistant/MainWindow.Items.cs").Contains("if (ImGui.IsItemHovered()) DrawRecipeTooltip(RecipeTipOf(recipe));", StringComparison.Ordinal), "the craft book");
            Check(Read("src/TenshoAssistant/MainWindow.ListPanels.cs").Contains("if(ImGui.IsItemHovered())DrawRecipeTooltip(RecipeTipOf(craft.Recipe),", StringComparison.Ordinal), "a list panel's crafts");
            Check(Read("src/TenshoAssistant/MainWindow.CraftFromStock.cs").Contains("if(ImGui.IsItemHovered())DrawRecipeTooltip(RecipeTipOf(recipe),localization.Format(\"CraftStock.JobIconTip\"", StringComparison.Ordinal), "the crafts from stock");
            foreach (var code in new[] { "en", "fr" })
            {
                var language = Read($"src/TenshoAssistant/Localization/Languages/{code}.json");
                Check(language.Contains("\"RecipeTip\": {\n    \"Minimum\": \"", StringComparison.Ordinal) && language.Contains("\"Ingredients\": \"", StringComparison.Ordinal), code + ": the texts");
            }
        }),
    ];
}
