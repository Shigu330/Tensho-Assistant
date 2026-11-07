// 0.785 (X-27, user request): the list panels can show only the finished items.
internal static class FinishedOnly785Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Finished items only: off by default, for the crafting list, the workshop and the preparation, the gathering help unchanged",() =>
        {
            Check(!new TenshoAssistant.Configuration().ListPanelsFinishedOnly,"off by default");
            var page=Read("MainWindow.ListPanels.cs");
            Check(page.Contains("\"CraftList\"=>craftListPanelShown?.Entries.Select(entry=>(entry.ItemId,entry.Quantity)).ToArray(),") && page.Contains("\"Workshop\"=>WorkshopLines(),")
                && page.Contains("\"Preparation\"=>preparationPanelShown is { } recipe ? new[]{(recipe.ResultItemId,(long)configuration.PreparationPanelTarget)}:null,"),"the three lists");
            Check(page.IndexOf("listPanelSources[key]=entries;")<page.IndexOf("if(configuration.ListPanelsFinishedOnly && ListPanelFinished(key) is { } finished)"),"the gathering help still gets the materials");
            Check(page.Contains("BoolSetting(\"ListPanels.FinishedOnly\",\"ListPanels.FinishedOnlyTip\""),"its setting");
        }),
    ];
}
