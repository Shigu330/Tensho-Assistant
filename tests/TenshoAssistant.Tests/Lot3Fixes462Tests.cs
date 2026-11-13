// 0.462 (in-game campaign, batch 3): comparison grid, « all tools » entry gone, empty slots of the company chest. Native and ImGui code pinned in the source.
internal static class Lot3Fixes462Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gear comparison: stats in a grid, nothing side by side on one line",() =>
        {
            var ui=Read("MainWindow.GearCompare.cs");
            Check(ui.Contains("ImGui.BeginTable(id,4,ImGuiTableFlags.SizingFixedFit)") && ui.Contains("DrawStatPairs(\"HoverCompare\"+comparison.Worn.Id") && !ui.Contains("ImGui.SameLine()"),"table (X-290: the shared DrawStatPairs)");
        }),
        ("Item menus: the « all tools » entry is no longer offered",() =>
        {
            Check(!Read("MainWindow.ItemContextMenu.cs").Contains("routes.Add(ItemContextRoute.Tools);"),"Tensho menu");
            Check(!Read("Items/ItemContextMenuBridge.Dalamud.cs").Contains("ItemContextRoute.Sources,ItemContextRoute.Tools]"),"game menu default");
        }),
        ("Inventory capture: a slot with quantity 0 is empty; a negative one still fails, with its detail",() =>
        {
            var reader=Read("Inventory/InventoryNativeReader.Dalamud.cs");
            Check(reader.Contains("if (slot.IsEmpty || slot.Quantity == 0) continue;") && reader.Contains("|| slot.BaseItemId == 0 || slot.Quantity < 0)") && reader.Contains("new InvalidDataException($\"bag {(int)bag},"),"capture");
        }),
    ];
}
