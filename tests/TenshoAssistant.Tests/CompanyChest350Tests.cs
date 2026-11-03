using TenshoAssistant.Inventory;

// 0.350 (g, I-09): the company chest as a storage — its containers, a stock row once opened, the import panel beside its window.
internal static class CompanyChest350Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Company chest: five pages, gil and crystals; added last; a stock row only once opened; never sorted",() =>
        {
            Check((int)BankStorageKind.FreeCompanyChest == 9,"added last");
            Check(BankStorageScope.ContainerIds(BankStorageKind.FreeCompanyChest).SequenceEqual([20000,20001,20002,20003,20004,22000,22001]),"containers");
            Check(!BankStorageScope.Sortable(BankStorageKind.FreeCompanyChest),"not offered to the sort preview");
            var containers = BankStorageScope.ContainerIds(BankStorageKind.FreeCompanyChest).Select(id => new InventoryContainerSnapshot(new(new(7),new(id)),InventoryCoverage.Complete,
                id == 20002 ? [new InventoryStack(new(new(5057),false),12,0)] : [])).ToArray();
            var chest = new BankEntry(new(7,BankStorageKind.FreeCompanyChest,0),"Company chest",new InventorySnapshot(DateTimeOffset.UtcNow,"test",containers));
            var held = ItemHoldings.Build(5057,7,null,[chest]);
            Check(held.Rows.Any(row => row.Label == StorageLabel.FreeCompanyChest && row.Nq == 12),"counted in the stocks");
            Check(!ItemHoldings.Build(5057,7,null,[]).Rows.Any(row => row.Label == StorageLabel.FreeCompanyChest),"no row when never opened (it may not exist)");
            Check(Read("BankImportOverlay.cs").Contains("menus.FreeCompany is { } company",StringComparison.Ordinal) && Read("Inventory/BankInventoryBridge.Dalamud.cs").Contains("FindVisible(\"FreeCompanyChest\")",StringComparison.Ordinal),
                "import panel beside the game's chest window, read only while it is open");
        }),
    ];
}
