using TenshoAssistant.Inventory;

// 0.357 (g, C-03): items the armoire takes, kept elsewhere; spare copies first, gear set pieces left out, unknown armoire stays unknown.
internal static class Armoire357Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static InventoryStack Stack(uint id,int slot,bool hq = false) => new(new(new(id),hq),1,slot);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Armoire: cabinet items outside the armoire, spare copies first, gear set pieces and unknown armoire handled",() =>
        {
            var cabinet = new HashSet<uint> { 10,20,30 };
            var storages = new List<(string,IEnumerable<InventoryStack>)>
            {
                ("Bags",[Stack(10,0),Stack(10,1),Stack(99,2)]),
                ("Armoury",[Stack(20,0),Stack(30,1)]),
            };
            var found = StorageReport.ForArmoire(storages,cabinet,new HashSet<uint>{30},new HashSet<uint>{20});
            Check(found.Count == 2 && found[0] is { ItemId:30,InArmoire:true } && found[1] is { ItemId:10,InArmoire:false,Storage:"Bags" },"spare first, once per item, set piece left out");
            var unknown = StorageReport.ForArmoire(storages,cabinet,null,null);
            Check(unknown.Count == 3 && unknown.All(entry => entry.InArmoire is null),"armoire not read: unknown, never « not there »");
            var page = Read("MainWindow.StorageReport.cs");
            Check(page.Contains("stack.Slot<StorageReport.DresserSlots",StringComparison.Ordinal)
                && page.Contains("entry.Key.Kind==BankStorageKind.Armoury && sets is not null",StringComparison.Ordinal),"outfit pieces and unknown gear sets left out");
        }),
    ];
}
