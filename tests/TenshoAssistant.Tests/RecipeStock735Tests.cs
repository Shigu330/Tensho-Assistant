// 0.735 (user report): « partial » on crystals the character has plenty of.
internal static class RecipeStock735Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Recipe stock: a lower bound that covers the need says OK; one that does not says what is not read",() =>
        {
            var page=Read("MainWindow.Items.cs");
            Check(page.IndexOf("var missing = Math.Max(0, required - summary.Total);")<page.IndexOf("missing == 0 ? \"RecipeUi.PartialEnough\":\"RecipeUi.Partial\""),"the need is known before the word is chosen");
            Check(Read("Localization/Languages/fr.json").Contains("\"PartialEnough\": \"{0} · OK, au moins autant\""),"French");
            // Still a lower bound: a retainer's crystals are never read (Inventory/ItemHoldings.cs, 0.329).
            Check(Read("Inventory/ItemHoldings.cs").Contains("row with { Coverage = InventoryCoverage.Partial }"),"crystals stay a lower bound");
        }),
    ];
}
