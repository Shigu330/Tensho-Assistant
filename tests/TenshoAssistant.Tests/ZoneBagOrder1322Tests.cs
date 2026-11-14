using TenshoAssistant.Inventory;

// 0.1322 (X-321, user report of 2026-11-14 with captures of the game's inventory and Tensho's bank page): the user's sort profile has one zone
// over the 140 slots (« Nouvelle zone », start 0, count 140, a category list); a zone filled its positions one after the other, so the bags
// came out 1, 2, 3, 4 while the game shows bag 1 top left, bag 2 top right, bag 3 bottom left (the order wanted: 1, 3, 2, 4, X-187). A zone's
// positions now follow FillOrder too; a zone inside one bag and the other storages are unchanged.
internal static class ZoneBagOrder1322Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly InventorySortScope Bags=InventorySortScope.Personal(7);
    private static (string Name,uint Category) Describe(uint id)=>("Item "+id.ToString("D4"),1);
    // 101 stacks as in the capture (101/140): bags 1 and 2 full, 31 in bag 3, item ids in reverse so the sort moves them.
    private static InventorySnapshot Snapshot(int count=101)=>new(DateTimeOffset.UtcNow,"fixture",Enumerable.Range(0,4).Select(bag=>new InventoryContainerSnapshot(new(new(7),new(bag)),InventoryCoverage.Complete,
        Enumerable.Range(0,35).Where(slot=>bag*35+slot<count).Select(slot=>new InventoryStack(new(new((uint)(2000-bag*35-slot)),false),1,slot,bag*35+slot)).ToArray())));
    private static InventorySortProfile Whole(bool reverse=false)=>InventorySortRules.Default(Bags) with
    { Zones=[new InventorySortZone("Nouvelle zone",0,140,0){ CategoryIds=[1], ShareUnused=true, ReverseFill=reverse }] };

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Zone bag order 1322: a zone over the four bags fills bag 1, then bag 3, then bag 2 (the user's profile, 101 stacks)",() =>
        {
            var snapshot=Snapshot();
            var preview=InventorySortRules.Build(snapshot.Containers.Values,Bags,Whole(),Describe);
            var slots=preview.Slots!;
            Check(Enumerable.Range(0,35).All(index=>slots[index] is not null) && Enumerable.Range(70,35).All(index=>slots[index] is not null),"bags 1 and 3 full");
            Check(Enumerable.Range(35,31).All(index=>slots[index] is not null) && Enumerable.Range(66,4).All(index=>slots[index] is null)
                && Enumerable.Range(105,35).All(index=>slots[index] is null),"31 in bag 2, bag 4 empty");
            var reading=Enumerable.Range(0,35).Concat(Enumerable.Range(70,35)).Concat(Enumerable.Range(35,31)).Select(index=>slots[index]!.Stack.Item.Id.Value).ToArray();
            Check(reading.SequenceEqual(reading.Order()),"the sorted order reads bag 1, bag 3, bag 2");
            var plan=InventoryVisualSortPlan.Build(snapshot,preview,Bags);
            Check(plan.Order.Count==140 && plan.Order.Distinct().Count()==140,"« Apply » gets a whole permutation of that placement");
            Check(plan.Order[70]==new InventoryPhysicalSlot(slots[70]!.Origin.Container.Value,slots[70]!.Stack.Slot),"the first position of bag 3 gets the 36th sorted stack");
        }),
        ("Zone bag order 1322: filled from the end, the zone's order is the reverse (bag 4, 2, 3, 1 from their last slot)",() =>
        {
            var preview=InventorySortRules.Build(Snapshot(40).Containers.Values,Bags,Whole(true),Describe);
            var slots=preview.Slots!;
            Check(Enumerable.Range(105,35).All(index=>slots[index] is not null) && Enumerable.Range(65,5).All(index=>slots[index] is not null)
                && Enumerable.Range(0,65).All(index=>slots[index] is null) && Enumerable.Range(70,35).All(index=>slots[index] is null),"bag 4 full, then the end of bag 2");
            Check(slots[139]!.Stack.Item.Id.Value<slots[105]!.Stack.Item.Id.Value,"the first sorted stack at the very end");
        }),
        ("Zone bag order 1322: a zone inside one bag and another storage keep their order position after position",() =>
        {
            var profile=InventorySortRules.Default(Bags) with { Zones=[new InventorySortZone("Bag 2",35,10,0){ CategoryIds=[1] }] };
            var slots=InventorySortRules.Build(Snapshot(20).Containers.Values,Bags,profile,Describe).Slots!;
            Check(Enumerable.Range(35,10).All(index=>slots[index] is not null) && slots[45] is null,"the zone's ten positions in a row");
            Check(slots[35]!.Stack.Item.Id.Value<slots[44]!.Stack.Item.Id.Value,"from its start");
            var saddle=InventorySortScope.FromStorage(new BankKey(7,BankStorageKind.Saddlebag,0));
            Check(InventorySortRules.FillOrder(saddle).SequenceEqual(Enumerable.Range(0,saddle.Capacity)),"the saddlebag: position after position, zones too");
        }),
    ];
}
