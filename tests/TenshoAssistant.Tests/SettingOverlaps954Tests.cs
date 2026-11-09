// X-76 step 3 (0.954): the three overlaps of the settings groups pass: origin groups greyed with « finished only », ids box greyed in developer mode,
// the from-stock price box with its own label.
internal static class SettingOverlaps954Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Setting overlaps 954: finished only greys the groups, developer mode greys the ids box, from-stock box has its own label",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("MainWindow.ListPanels.cs").Contains("ImGui.BeginDisabled(configuration.ListPanelsFinishedOnly);"),"groups");
            Check(Read("MainWindow.Advanced.cs").Contains("ImGui.BeginDisabled(developerMode);"),"ids");
            Check(Read("MainWindow.CraftFromStock.cs").Contains("localization.Get(\"CraftStock.Market\")+\"###FromStockMarket\""),"from stock");
        }),
    ];
}
