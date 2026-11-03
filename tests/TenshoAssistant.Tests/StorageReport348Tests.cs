using TenshoAssistant.Inventory;

// 0.348 (g, I-10): storage report — stacks that fit in fewer slots, armoury pieces outside every gear set; nothing moved.
internal static class StorageReport348Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static InventoryStack Stack(uint item,long quantity,int slot,bool hq = false) => new(new(new(item),hq),quantity,slot);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Storage report: merge only what fits in fewer slots, per quality and storage; stack size 1 or unknown never merged",() =>
        {
            uint Size(uint item) => item switch { 1 => 999,2 => 999,3 => 1,_ => 0 };
            var report = StorageReport.Mergeable(
            [
                ("Sacs",[Stack(1,500,0),Stack(1,400,1),Stack(1,300,2),Stack(2,999,3),Stack(2,999,4),Stack(3,1,5),Stack(3,1,6),Stack(4,5,7),Stack(4,5,8),Stack(1,10,9,true)]),
                ("Servant",[Stack(1,10,0)]),
            ],Size);
            Check(report.Count == 1 && report[0] is { ItemId: 1,Hq: false,Stacks: 3,Needed: 2,Freed: 1,Storage: "Sacs" },"1 200 in 3 stacks fit in 2; full stacks, size 1, unknown size, other quality and other storage left alone");
        }),
        ("Storage report: armoury pieces used by no gear set, soul crystals and unknown containers left out",() =>
        {
            InventoryContainerSnapshot Container(int id,InventoryCoverage coverage,params InventoryStack[] stacks) => new(new(new(9),new(id)),coverage,stacks);
            var outside = StorageReport.OutsideGearsets(
            [
                Container(3500,InventoryCoverage.Complete,Stack(100,1,0),Stack(101,1,1,true)),
                Container(3400,InventoryCoverage.Complete,Stack(200,1,0)),
                Container(3201,InventoryCoverage.Unknown),
            ],new HashSet<uint> { 101 });
            Check(outside.Count == 1 && outside[0].Item.Id.Value == 100,"101 (HQ folded) is in a set; soul crystal 200 ignored");
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/GearsetReader.Dalamud.cs"));
            Check(reader.Contains("GearsetFlag.Exists",StringComparison.Ordinal) && !reader.Contains("IsValidGearset(",StringComparison.Ordinal),"sets found by their flag: no game function called");
        }),
    ];
}
