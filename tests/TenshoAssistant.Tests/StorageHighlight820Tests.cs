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
            Check(reader.Contains("if(depth>4 || !UiNodes.Walkable(manager) || manager->NodeListCount>1024 || found.Count>=400)return;"),"bounded, R-14 guard");
            var page=Read("MainWindow.StorageHighlight.cs");
            Check(page.Contains("new[]{\"CraftList\",\"Workshop\"}") && reader.Contains("icons.Add(1_000_000+shown);"),"lists, HQ");
            Check(new TenshoAssistant.Configuration().ListPanelsStorageHighlight,"on by default");
        }),
        ("Storage highlight (0.839, X-66): two items sharing an icon told apart by the quantity; alike in both, left out; not read, the icon alone",() =>
        {
            const uint Wine=24021;var wanted=new HashSet<uint>{Wine,1_000_000+Wine};
            TenshoAssistant.Inventory.StorageCellMatch.Stack[] stacks=[new(Wine,595,false),new(Wine,1,true)];
            Check(!TenshoAssistant.Inventory.StorageCellMatch.Marks(Wine,595,stacks,wanted),"the plain wine, 595: not framed");
            Check(TenshoAssistant.Inventory.StorageCellMatch.Marks(Wine,1,stacks,wanted),"the quality wine, 1: framed");
            Check(!TenshoAssistant.Inventory.StorageCellMatch.Marks(Wine,null,stacks,wanted),"no number shown and both there: left out");
            Check(TenshoAssistant.Inventory.StorageCellMatch.Marks(Wine,7,[],wanted),"storage not read: the icon");
            Check(TenshoAssistant.Inventory.StorageCellMatch.Quantity("1.053")==1053 && TenshoAssistant.Inventory.StorageCellMatch.Quantity("")==null,"quantity text");
            var reader=Read("Inventory/StorageHighlightReader.Dalamud.cs");
            Check(reader.Contains("if(cell->IconImage!=null && cell->IconImage->IsVisible() && Shown(node))"),"hidden tabs and empty cells left out");
        }),
    ];
}
