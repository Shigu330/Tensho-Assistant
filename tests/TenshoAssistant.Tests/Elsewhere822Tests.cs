// 0.822 (X-55, user request): the tracking lists say when an item is not on the character.
internal static class Elsewhere822Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Elsewhere: shown when the bags do not cover the need and another storage holds some; the storages named",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("var onMe=stock.Rows.Where(row=>row.Category==Inventory.StorageCategory.PlayerBags).Sum(row=>row.Nq+row.Hq);") && page.Contains("if(onMe>=required)return null;"),"bags first");
            Check(page.Contains("localization.Format(\"ListPanels.Elsewhere\",elsewhere)"),"named");
        }),
    ];
}
