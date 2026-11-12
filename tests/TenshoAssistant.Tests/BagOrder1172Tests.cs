using TenshoAssistant.Inventory;

// 0.1172 (X-187, user request of 12/11): Tensho's visual sort fills the player's bags in the order 1, 3, 2, 4 (not 1, 2, 3, 4), so the sorted order
// reads coherently on the game's screen; the preview shows the same placement; zones keep the positions they name; the other storages are unchanged.
internal static class BagOrder1172Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly InventorySortScope Bags = InventorySortScope.Personal(7);
    private static (string Name, uint Category) Describe(uint id) => ("Item " + id.ToString("D4"), 1);
    // 40 stacks: the 35 slots of bag 1 and 5 of bag 2, item ids in reverse so the sort moves them.
    private static InventorySnapshot Snapshot() => new(DateTimeOffset.UtcNow, "fixture", Enumerable.Range(0, 4).Select(bag => new InventoryContainerSnapshot(new(new(7), new(bag)), InventoryCoverage.Complete,
        bag switch
        {
            0 => Enumerable.Range(0, 35).Select(slot => new InventoryStack(new(new((uint)(1000 - slot)), false), 1, slot, slot)).ToArray(),
            1 => Enumerable.Range(0, 5).Select(slot => new InventoryStack(new(new((uint)(900 - slot)), false), 1, slot, 35 + slot)).ToArray(),
            _ => Array.Empty<InventoryStack>(),
        })));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Bag order 1172: the player's bags are filled 1, 3, 2, 4; a storage of another kind position after position", () =>
        {
            var order = InventorySortRules.FillOrder(Bags);
            Check(order.Length == 140 && order.Distinct().Count() == 140, "every position once");
            Check(order.Take(35).SequenceEqual(Enumerable.Range(0, 35)) && order.Skip(35).Take(35).SequenceEqual(Enumerable.Range(70, 35))
                && order.Skip(70).Take(35).SequenceEqual(Enumerable.Range(35, 35)) && order.Skip(105).SequenceEqual(Enumerable.Range(105, 35)), "bag 1, bag 3, bag 2, bag 4");
            var saddle = InventorySortScope.FromStorage(new BankKey(7, BankStorageKind.Saddlebag, 0));
            Check(InventorySortRules.FillOrder(saddle).SequenceEqual(Enumerable.Range(0, saddle.Capacity)), "the saddlebag unchanged");
        }),
        ("Bag order 1172: 40 stacks sorted fill bag 1, then bag 3; bag 2 stays empty; the plan applies that placement", () =>
        {
            var snapshot = Snapshot();
            var preview = InventorySortRules.Build(snapshot.Containers.Values, Bags, InventorySortRules.Default(Bags), Describe);
            Check(preview.Slots is { Count: 140 } slots && Enumerable.Range(0, 35).All(index => slots[index] is not null) && Enumerable.Range(70, 5).All(index => slots[index] is not null)
                && Enumerable.Range(35, 35).All(index => slots[index] is null) && slots[75] is null, "bag 1 full, 5 in bag 3, bag 2 empty");
            var sorted = Enumerable.Range(0, 35).Concat(Enumerable.Range(70, 5)).Select(index => preview.Slots![index]!.Stack.Item.Id.Value).ToArray();
            Check(sorted.SequenceEqual(sorted.Order()), "the sorted order reads bag 1 then bag 3");
            var plan = InventoryVisualSortPlan.Build(snapshot, preview, Bags);
            Check(plan.Order.Count == 140 && plan.Order.Distinct().Count() == 140, "a whole permutation");
            var third = preview.Slots![70]!;
            Check(plan.Order[70] == new InventoryPhysicalSlot(third.Origin.Container.Value, third.Stack.Slot), "the first position of bag 3 gets the 36th sorted stack");
        }),
        ("Bag order 1172: the stacks outside a zone follow the same order; the zone keeps its positions", () =>
        {
            var snapshot = Snapshot();
            var profile = InventorySortRules.Default(Bags) with { Zones = [new InventorySortZone("First", 0, 2, 1)] };
            var preview = InventorySortRules.Build(snapshot.Containers.Values, Bags, profile, Describe);
            Check(preview.Slots![0] is not null && preview.Slots[1] is not null, "the zone filled");
            Check(Enumerable.Range(2, 33).All(index => preview.Slots[index] is not null) && Enumerable.Range(70, 5).All(index => preview.Slots[index] is not null)
                && Enumerable.Range(35, 35).All(index => preview.Slots[index] is null), "the rest: bag 1, then bag 3");
        }),
    ];
}
