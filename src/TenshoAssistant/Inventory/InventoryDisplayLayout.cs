namespace TenshoAssistant.Inventory;

/// <summary>Projects the game's display permutation without changing physical quantity snapshots.</summary>
public static class InventoryDisplayLayout
{
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
                throw new InvalidOperationException("Ordre visuel indisponible : actualisez ce rangement.");
            pages[stack.DisplayIndex / 35][stack.DisplayIndex % 35] = stack;
        }
        return pages;
    }
}
