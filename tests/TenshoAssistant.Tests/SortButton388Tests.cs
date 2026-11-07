// 0.388 (user request): a « Sort » button above the game's inventory window — same plan as the sort page's « Apply », on click only,
// window position read on the framework thread, on by default and in the reset list.
internal static class SortButton388Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Sort button: on by default, applies the bags' saved rules on click through the visual sort request, window read off Draw",() =>
        {
            Check(new TenshoAssistant.Configuration().InventorySortButton && TenshoAssistant.ConfigurationReset.Reset.Contains("InventorySortButton"),"default and reset list");
            var button = Read("MainWindow.InventorySortButton.cs");
            Check(button.Contains("InventorySortRules.Load(configuration.InventorySortProfiles,scope,",StringComparison.Ordinal)
                && button.Contains("visualSort.Request(InventoryVisualSortPlan.Build(snapshot,result,scope),DateTimeOffset.UtcNow)",StringComparison.Ordinal),"same plan as the page");
            Check(button.Contains("inventorySortButtonMessage=ApplyBagSort(character);",StringComparison.Ordinal),"on click");
            var reader = Read("Inventory/InventoryWindowReader.Dalamud.cs");
            Check(reader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);",StringComparison.Ordinal) && reader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);",StringComparison.Ordinal),"framework thread, cleaned up");
        }),
    ];
}
