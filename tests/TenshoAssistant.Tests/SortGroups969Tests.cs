using TenshoAssistant.Inventory;

// X-88 (0.969): more than 64 categories a zone, and whole groups of categories added at once.
internal static class SortGroups969Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Sort groups 969: limit above the game's categories, group entries in the add menu",() =>
        {
            Check(InventorySortRules.MaxZoneCategories>=200,"limit");
            var rules=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.SortRules.cs"));
            Check(rules.Contains("catalog.Categories.GroupBy(value=>groupOf(value.Id))") && rules.Contains("localization.Format(\"SortUi.AddGroup\""),"groups");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("ItemUICategory>().GetRowOrDefault(id)?.OrderMajor"),"game groups");
        }),
    ];
}
