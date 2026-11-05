namespace TenshoAssistant.Inventory;

/// <summary>Projects the game's display permutation without changing physical quantity snapshots.</summary>
public static class InventoryDisplayLayout
{
    /// <summary>
    /// Containers without a bag layout, shown in slot order: armoury chest, glamour dresser, armoire (0.237) and, 0.463 (in-game campaign), the company chest (20000 to 22001: pages, gil,
    /// crystals), for which the game keeps no display order.
    /// </summary>
    public static bool HasNoBagLayout(int container)=>container is >= 3200 and < 4000 or >= 20000 and <= 22001 or >= 25000;

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
