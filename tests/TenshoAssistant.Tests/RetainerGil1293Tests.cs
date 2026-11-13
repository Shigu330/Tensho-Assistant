using TenshoAssistant.Inventory;

// X-291 (0.1293): user request of 13/11 about 23:45 (Storage › Bank, retainers Shirune and Zet-suna): the retainer's gil showed as an
// item cell (gil icon, « 999 », « 528 ») in a « Bag 6 ». The gil purses (12000 retainer, 22000 company chest) are an amount, never a page.
internal static class RetainerGil1293Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static InventoryContainerSnapshot Container(int id, InventoryCoverage coverage, params InventoryStack[] stacks) => new(new(new(77), new(id)), coverage, stacks);
    private static InventoryStack Stack(uint item, long quantity, int slot, int display = -1) => new(new(new(item), false), quantity, slot, display);
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-291: a retainer's gil is an exact amount, never a cell of a bag page; its crystals stay items", () =>
        {
            var source = new[]
            {
                Container(10000, InventoryCoverage.Complete, Stack(5057, 3, 0, 0)),
                Container(12000, InventoryCoverage.Complete, Stack(1, 1234567, 0)),
                Container(12001, InventoryCoverage.Complete, Stack(2, 40, 0)),
            };
            var layout = InventoryDisplayLayout.Pages(source);
            Check(layout.Pages.SelectMany(page => page.Container.Stacks).All(stack => stack.Item.Id.Value != 1), "no gil cell in the pages");
            Check(layout.Pages.SelectMany(page => page.Container.Stacks).Any(stack => stack.Item.Id.Value == 2), "crystals kept as items");
            Check(InventoryDisplayLayout.Gil(source) == 1234567, "exact amount");
            Check(1234567L.ToString("N0", TenshoAssistant.Localization.TenshoCulture.For("en")) == "1,234,567"
                && 1234567L.ToString("N0", TenshoAssistant.Localization.TenshoCulture.For("fr")) == "1\u00a0234\u00a0567", "thousands separators of each language");
        }),
        ("X-291: a purse not read is unknown, not zero; the company chest's gil too", () =>
        {
            Check(InventoryDisplayLayout.Gil([Container(10000, InventoryCoverage.Complete)]) is null, "no purse: unknown");
            Check(InventoryDisplayLayout.Gil([Container(12000, InventoryCoverage.Unknown)]) is null, "purse unread: unknown");
            Check(InventoryDisplayLayout.Gil([Container(12000, InventoryCoverage.Complete)]) == 0, "purse read empty: zero");
            Check(InventoryDisplayLayout.Gil([Container(20000, InventoryCoverage.Complete, Stack(500, 1, 0)), Container(22000, InventoryCoverage.Complete, Stack(1, 50, 0))]) == 50, "company chest");
            Check(InventoryDisplayLayout.IsGil(12000) && InventoryDisplayLayout.IsGil(22000) && !InventoryDisplayLayout.IsGil(12001) && !InventoryDisplayLayout.IsGil(22001), "purses only");
        }),
        ("X-291: the Bank page gives the gil on its own line and keeps it out of the item lists", () =>
        {
            var bank = Read("MainWindow.Bank.cs");
            Check(bank.Contains("sources.Where(container=>!InventoryDisplayLayout.IsGil(container.Location.Container.Value))"), "out of the grid's rows and filters");
            Check(bank.Contains("localization.Format(\"BankUi.GilLine\",purseName,held.ToString(\"N0\",Numbers))"), "amount line with separators");
            Check(Read("MainWindow.BankList.cs").Contains("Values.Where(container=>!InventoryDisplayLayout.IsGil(container.Location.Container.Value))"), "out of the list view");
        }),
    ];
}
