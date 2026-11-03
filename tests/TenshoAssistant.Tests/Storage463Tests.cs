using TenshoAssistant.Inventory;

// 0.463 (in-game campaign): the company chest has no display order: it is shown in slot order instead of refusing the tab.
internal static class Storage463Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Company chest: no bag layout, like the armoury chest; the bags and retainers still need theirs",() =>
        {
            foreach(var id in new[]{20000,20004,22000,22001,3200,3500,25000})Check(InventoryDisplayLayout.HasNoBagLayout(id),"no layout: "+id);
            foreach(var id in new[]{0,3,4000,4100,10000,10006})Check(!InventoryDisplayLayout.HasNoBagLayout(id),"layout: "+id);
            var bank=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Bank.cs"));
            Check(bank.Contains("when (copied.All(container => InventoryDisplayLayout.HasNoBagLayout(container.Location.Container.Value)))"),"used by the page");
        }),
    ];
}
