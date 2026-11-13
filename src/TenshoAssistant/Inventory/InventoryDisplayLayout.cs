namespace TenshoAssistant.Inventory;

/// <summary>Projects the game's display permutation without changing physical quantity snapshots.</summary>
public static class InventoryDisplayLayout
{
    /// <summary>
    /// Containers without a bag layout, shown in slot order: armoury chest, glamour dresser, armoire (0.237) and, 0.463 (in-game campaign), the company chest (20000 to 22001: pages, gil,
    /// crystals), for which the game keeps no display order.
    /// </summary>
    public static bool HasNoBagLayout(int container)=>container is >= 3200 and < 4000 or >= 20000 and <= 22001 or >= 25000;

    /// <summary>
    /// X-275 (0.1285): the containers the game orders visually (bags 0–3, saddlebags, retainer pages 10000–10006). Every other container
    /// of a storage (a retainer's gil 12000 and crystals 12001 since 0.767, currencies, crystals) is shown in slot order: mixed in the
    /// visual order, its stacks without a position made the retainer's whole tab fail.
    /// </summary>
    public static bool HasBagLayout(int container)=>container is >= 0 and <= 3 or 4000 or 4001 or 4100 or 4101 or >= 10000 and <= 10006;

    /// <summary>
    /// X-291 (0.1293, user request of 13/11 about 23:45, Storage › Bank, retainers Shirune and Zet-suna: the gil shown as an item cell, « 999 »,
    /// in a « Bag 6 »): a storage's gil purse, a retainer's (12000) or the company chest's (22000), is an amount, never an item page.
    /// </summary>
    public static bool IsGil(int container)=>container is 12000 or 22000;

    /// <summary>X-291 (0.1293): the gil held by the purses read among these containers; null when none was read (unknown, not zero).</summary>
    public static long? Gil(IEnumerable<InventoryContainerSnapshot> source)
    {
        var purses=source.Where(container=>IsGil(container.Location.Container.Value) && container.Coverage!=InventoryCoverage.Unknown).ToArray();
        return purses.Length==0 ? null:purses.Sum(container=>container.Stacks.Sum(stack=>stack.Quantity));
    }

    /// <summary>One displayed page: its number (1-based, in the storage) and its stacks, Slot being the cell (0–34).</summary>
    public readonly record struct DisplayPage(int Number, InventoryContainerSnapshot Container);

    /// <summary>The pages of a storage, and whether the bags are shown in their physical slot order (visual order unreadable).</summary>
    public sealed record DisplayPages(IReadOnlyList<DisplayPage> Pages, bool PhysicalOrder);

    /// <summary>
    /// X-275 (0.1285): the pages of one storage. The bags with a visual layout follow the game's order; when it cannot be used (a position
    /// missing, out of range or doubled), each read bag is shown in its own slot order and <see cref="DisplayPages.PhysicalOrder"/> is set
    /// (0.329 did this for the character's live bags only; a retainer showed nothing). The other containers follow, in slot order, 35 per
    /// page. No stack is dropped.
    /// </summary>
    public static DisplayPages Pages(IEnumerable<InventoryContainerSnapshot> source)
    {
        var copied = source.OrderBy(container => container.Location.Container.Value).ToArray();
        var pages = new List<DisplayPage>();
        var physical = false;
        var bags = copied.Where(container => HasBagLayout(container.Location.Container.Value)).ToArray();
        if (bags.Length > 0)
        {
            IReadOnlyList<InventoryStack?[]>? layout;
            try { layout = Build(bags); }
            catch (InvalidOperationException) { layout = null; }
            if (layout is not null)
            {
                var coverage = Coverage(bags);
                foreach (var page in layout)
                    pages.Add(new(pages.Count + 1, new InventoryContainerSnapshot(bags[0].Location, coverage,
                        page.Select((stack, index) => (stack, index)).Where(pair => pair.stack.HasValue)
                            .Select(pair => pair.stack!.Value with { Slot = pair.index }))));
            }
            else
            {
                physical = true;
                for (var index = 0; index < bags.Length; index++)
                    if (bags[index].Coverage != InventoryCoverage.Unknown) pages.Add(new(index + 1, bags[index]));
            }
        }
        // X-291 (0.1293): the gil purses are left out of the pages: an amount (Gil), shown on its own line.
        var others = copied.Where(container => !HasBagLayout(container.Location.Container.Value) && !IsGil(container.Location.Container.Value)).ToArray();
        if (others.Length > 0)
        {
            var stacks = others.SelectMany(container => container.Stacks.OrderBy(stack => stack.Slot)).ToArray();
            var coverage = Coverage(others);
            var first = pages.Count == 0 ? 0 : pages.Max(page => page.Number);
            for (var page = 0; page * 35 < stacks.Length; page++)
                pages.Add(new(first + page + 1, new InventoryContainerSnapshot(others[0].Location, coverage,
                    stacks.Skip(page * 35).Take(35).Select((stack, index) => stack with { Slot = index, DisplayIndex = -1 }))));
        }
        return new(pages, physical);
    }

    private static InventoryCoverage Coverage(IEnumerable<InventoryContainerSnapshot> containers) =>
        containers.All(container => container.Coverage == InventoryCoverage.Complete) ? InventoryCoverage.Complete : InventoryCoverage.Partial;

    public static IReadOnlyList<InventoryStack?[]> Build(IEnumerable<InventoryContainerSnapshot> source)
    {
        var containers = source.OrderBy(container => container.Location.Container.Value).ToArray();
        var stacks = containers.SelectMany(container => container.Stacks).ToArray();
        var retainer = containers.Any(container => container.Location.Container.Value is >= 10000 and <= 10006);
        var capacity = retainer ? 175 : containers.Length * 35;
        var pages = Enumerable.Range(0, (capacity + 34) / 35).Select(_ => new InventoryStack?[35]).ToArray();
        var seen = new HashSet<int>();
        foreach (var stack in stacks)
        {
            if (stack.DisplayIndex < 0 || stack.DisplayIndex >= capacity || !seen.Add(stack.DisplayIndex))
                throw new InvalidOperationException("Visual order unavailable: refresh this storage.");
            pages[stack.DisplayIndex / 35][stack.DisplayIndex % 35] = stack;
        }
        return pages;
    }
}
