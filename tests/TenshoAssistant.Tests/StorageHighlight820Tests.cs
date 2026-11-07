// 0.820 (X-54, user request): the items of the crafting and workshop lists framed in an open retainer, saddlebag or company chest.
internal static class StorageHighlight820Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Storage highlight: other storages only, the crafting and workshop lists, NQ and HQ icons, behind its setting",() =>
        {
            var reader=Read("Inventory/StorageHighlightReader.Dalamud.cs");
            Check(reader.Contains("\"InventoryRetainerLarge\"") && reader.Contains("\"InventoryBuddy\"") && reader.Contains("\"FreeCompanyChest\"") && !reader.Contains("\"InventoryGrid"),"other storages, not the bags");
            Check(reader.Contains("if(manager==null || depth>4 || manager->NodeList==null || manager->NodeListCount>1024 || found.Count>=400)return;"),"bounded");
            var page=Read("MainWindow.StorageHighlight.cs");
            Check(page.Contains("new[]{\"CraftList\",\"Workshop\"}") && page.Contains("icons.Add(1_000_000+found.IconId);"),"lists, HQ");
            Check(new TenshoAssistant.Configuration().ListPanelsStorageHighlight,"on by default");
        }),
    ];
}
