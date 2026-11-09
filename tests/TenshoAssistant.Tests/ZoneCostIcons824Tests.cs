// 0.824 (X-56, user request): the teleport cost in the lists as icons and the amount, no words.
internal static class ZoneCostIcons824Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Zone cost: Teleport's icon, the gil's icon and the amount; the words only on hover",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("private const uint TeleportIcon=111;") && page.Contains("itemCatalog.State.Catalog?.Find(1)"),"icons");
            Check(page.Contains("if(group.Cost is { } gil)DrawListZoneCost(gil);"),"icons, no words in the header");
        }),
    ];
}
