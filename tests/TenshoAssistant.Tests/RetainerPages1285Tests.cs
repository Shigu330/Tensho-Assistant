using TenshoAssistant.Inventory;

// X-275 (0.1285): a retainer read with its gil (12000) and crystals (12001), recorded since 0.767, showed « visual order unavailable »
// and none of its bags: the extras have no visual position. Behaviour tests on InventoryDisplayLayout.Pages (pure), with the shape of the
// 13/11 reading (7 pages of 25 slots with a full visual order, gil without a position, empty crystals).
internal static class RetainerPages1285Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static InventoryContainerSnapshot Container(int id, params InventoryStack[] stacks) =>
        new(new(new(77), new(id)), InventoryCoverage.Complete, stacks);

    private static InventoryContainerSnapshot[] Retainer(bool visualOrder)
    {
        var containers = new List<InventoryContainerSnapshot>();
        for (var page = 0; page < 7; page++)
            containers.Add(Container(10000 + page, Enumerable.Range(0, 25).Select(slot =>
                new InventoryStack(new(new((uint)(1000 + page * 25 + slot)), false), slot + 1, slot,
                    visualOrder ? (page * 25 + slot) * 37 % 175 : -1)).ToArray()));
        containers.Add(Container(12000, new InventoryStack(new(new(1), false), 999, 0, -1)));
        containers.Add(Container(12001));
        return containers.ToArray();
    }

    private static long Total(IEnumerable<InventoryContainerSnapshot> containers) =>
        containers.SelectMany(container => container.Stacks).Sum(stack => stack.Quantity);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-275: a retainer with its gil keeps the visual order of its bags and shows the gil after them", () =>
        {
            var source = Retainer(true);
            var layout = InventoryDisplayLayout.Pages(source);
            Check(!layout.PhysicalOrder, "visual order kept");
            Check(layout.Pages.Count == 6 && layout.Pages.Select(page => page.Number).SequenceEqual([1, 2, 3, 4, 5, 6]), "5 visual pages, then the extras: " + layout.Pages.Count);
            Check(layout.Pages[5].Container.Stacks.Single().Item.Id.Value == 1, "gil on the last page");
            Check(Total(layout.Pages.Select(page => page.Container)) == Total(source), "nothing lost");
            var first = layout.Pages[0].Container.Stacks.Single(stack => stack.Slot == 0);
            Check(first.DisplayIndex == 0 && first.Item.Id.Value == 1000, "cell 0 holds the stack the game shows first");
        }),
        ("X-275: without a visual order, each read page is shown in its slot order with the note, nothing lost", () =>
        {
            var source = Retainer(false);
            var layout = InventoryDisplayLayout.Pages(source);
            Check(layout.PhysicalOrder, "note asked");
            Check(layout.Pages.Count == 8 && layout.Pages[6].Number == 7 && layout.Pages[7].Number == 8, "7 physical pages, then the extras");
            Check(layout.Pages[2].Container.Stacks.Single(stack => stack.Slot == 4).Item.Id.Value == 1054, "slot order of page 3");
            Check(Total(layout.Pages.Select(page => page.Container)) == Total(source), "nothing lost");
        }),
        ("X-275: a doubled position also falls back to the slot order; an unread bag is left out of it", () =>
        {
            var bags = new[]
            {
                Container(0, new InventoryStack(new(new(42), false), 3, 0, 5), new InventoryStack(new(new(43), false), 4, 1, 5)),
                new InventoryContainerSnapshot(new(new(77), new(1)), InventoryCoverage.Unknown, []),
                Container(2, new InventoryStack(new(new(44), true), 2, 7, 80)),
            };
            var layout = InventoryDisplayLayout.Pages(bags);
            Check(layout.PhysicalOrder && layout.Pages.Select(page => page.Number).SequenceEqual([1, 3]), "pages 1 and 3");
            Check(Total(layout.Pages.Select(page => page.Container)) == 9, "nothing lost");
        }),
        ("X-275: storages without any layout keep their slot order, 35 per page, as in 0.463", () =>
        {
            var chest = Container(20000, Enumerable.Range(0, 40).Select(slot => new InventoryStack(new(new((uint)(500 + slot)), false), 1, slot, -1)).ToArray());
            var layout = InventoryDisplayLayout.Pages([chest, Container(22000, new InventoryStack(new(new(1), false), 50, 0, -1))]);
            Check(!layout.PhysicalOrder && layout.Pages.Count == 2 && layout.Pages[1].Container.Stacks.Count == 6, "35 then 6");
            Check(layout.Pages[0].Container.Stacks[0].Item.Id.Value == 500, "slot order");
        }),
    ];
}
