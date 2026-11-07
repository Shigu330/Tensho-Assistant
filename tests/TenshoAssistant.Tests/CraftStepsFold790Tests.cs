// 0.790 (X-31, user request): each step folds; one way of writing the figures.
internal static class CraftStepsFold790Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Craft steps: a fold per step, open at first, and the same « held / needed » line as the materials",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("ImGui.TreeNodeEx($\"{localization.Format(\"ListPanels.CraftStep\",group.Key)} ({done}/{group.Count()})###ListCraftStep{key}{group.Key}\",ImGuiTreeNodeFlags.DefaultOpen)"),"fold per step");
            Check(page.Contains("DrawListPanelLine(key+\"Crafts\",craft.Item,craft.Needed,GetRecipeHoldings(craft.Item));"),"same line");
            Check(page.Contains("finally{ImGui.TreePop();}"),"popped");
        }),
    ];
}
