// 0.737 (user report): an item's right-click menu closed at once, now and then.
internal static class ItemMenu737Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Item menu: its id counts only the same item at the same place, not every icon of the frame",() =>
        {
            var cell=Read("UI/Components/ItemCell.cs");
            Check(cell.Contains("var key=ImGui.GetID(baseId);") && cell.Contains("menuSeen[key]=seen+1;"),"by place and item");
            Check(!cell.Contains("menuSerial++"),"no frame-wide serial");
            Check(cell.Contains("ItemLinksMenu(UniqueMenuId($\"ItemCell{item.Id}\"),item);") && cell.Contains("ItemLinksMenu($\"ItemCellName{id}\",item);"),"the icon and the name keep apart ids (0.269)");
        }),
    ];
}
