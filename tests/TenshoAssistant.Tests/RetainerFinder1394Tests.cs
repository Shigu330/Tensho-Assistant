using TenshoAssistant.Inventory;

// X-307 (0.1394): the comparison of the game's item finder copy of a retainer with the bank (pure): slots, HQ encoding, crystals; the summary
// holds numbers only.
internal static class RetainerFinder1394Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static FinderRetainer Finder(params (int Slot, uint Id, ushort Count)[] items)
    {
        var slots = new (uint, ushort)[RetainerFinderCompare.Slots];
        foreach (var (slot, id, count) in items) slots[slot] = (id, count);
        var crystals = new ushort[RetainerFinderCompare.Crystals]; crystals[0] = 50;
        return new(77, true, slots, new uint[RetainerFinderCompare.Equipped], crystals);
    }

    private static BankEntry Bank(bool withCrystals, params (int Page, int Slot, uint Id, bool Hq, long Quantity)[] stacks)
    {
        var owner = new InventoryOwnerId(77);
        var containers = Enumerable.Range(10000, 7).Select(id => new InventoryContainerSnapshot(new(owner, new(id)), InventoryCoverage.Complete,
            stacks.Where(stack => stack.Page == id - 10000).Select(stack => new InventoryStack(new(new(stack.Id), stack.Hq), stack.Quantity, stack.Slot)))).ToList();
        if (withCrystals) containers.Add(new(new(owner, new(BankStorageScope.RetainerCrystals)), InventoryCoverage.Complete, [new InventoryStack(new(new(2), false), 50, 0)]));
        return new(new(1, BankStorageKind.Retainer, 77), "Servant", new(DateTimeOffset.UtcNow, "test", containers));
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Retainer finder 1394: same items in the same slots, HQ from the id offset, crystals", () =>
        {
            var finder = Finder((0, 5057, 3), (26, 1_005_057, 2));
            var row = RetainerFinderCompare.Compare(finder, Bank(true, (0, 0, 5057, false, 3), (1, 1, 5057, true, 2)));
            Check(row.InBank && row.SameItems && row.SameOrder && row.FinderHighId == 1 && row.BankHq == 1 && row.SameCrystals == true, "all the same: " + row);
            Check(RetainerFinderCompare.Decode(1_005_057) == new InventoryItem(new(5057), true) && RetainerFinderCompare.Decode(5057) == new InventoryItem(new(5057), false), "decoding");
        }),
        ("Retainer finder 1394: other slots, other contents, no crystals saved, not in the bank", () =>
        {
            var finder = Finder((0, 5057, 3), (26, 1_005_057, 2));
            var moved = RetainerFinderCompare.Compare(finder, Bank(false, (0, 5, 5057, false, 3), (1, 1, 5057, true, 2)));
            Check(moved.SameItems && !moved.SameOrder && moved.SameCrystals is null, "same items, another slot, crystals unknown: " + moved);
            var other = RetainerFinderCompare.Compare(finder, Bank(true, (0, 0, 5057, false, 4), (1, 1, 5057, true, 2)));
            Check(!other.SameItems && !other.SameOrder, "another quantity: not the same");
            var missing = RetainerFinderCompare.Compare(finder, null);
            Check(!missing.InBank && missing.FinderStacks == 2 && missing.FinderHighId == 1, "not in the bank: counted only");
            var text = RetainerFinderCompare.Summary([finder], [moved, missing]);
            Check(text.StartsWith("1 retainers in the game's copy (1 current), 2 stacks", StringComparison.Ordinal) && !text.Contains("Servant", StringComparison.Ordinal), text);
        }),
    ];
}
