namespace TenshoAssistant.Inventory;

/// <summary>Publishes only the latest acquisition batch; callers retain immutable snapshots safely.</summary>
public sealed class InventoryService
{
    private InventorySnapshot? current;
    private InventorySnapshot? shown;

    public InventorySnapshot? Current => Volatile.Read(ref current);

    /// <summary>
    /// 0.741 (user report, docs/evidence/0.740-list-blink.md): for stock counts on screen only. After a change in the bags, <see cref="Current"/> is unknown until the next
    /// reading, up to 250 ms; counts shown from it fell to 0 and came back at each item gathered. This keeps the last reading of the same character meanwhile. Anything that acts
    /// (the bag sort) keeps using <see cref="Current"/>.
    /// </summary>
    public InventorySnapshot? Shown => Volatile.Read(ref shown);

    // Publish order is authoritative, not wall-clock order. No history or merging is implicit.
    public void Replace(InventorySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Interlocked.Exchange(ref current, snapshot);
        Interlocked.Exchange(ref shown, ForDisplay(Volatile.Read(ref shown), snapshot));
    }

    /// <summary>
    /// 0.742 (user report: the blink came back in 0.741): a reading made while the bags change can find a bag unreadable, and an unreadable bag counts nothing.
    /// For display, such a bag keeps its last complete reading from the same character; the next complete one replaces it. <see cref="Current"/> keeps what was read.
    /// </summary>
    public static InventorySnapshot ForDisplay(InventorySnapshot? before, InventorySnapshot next)
    {
        if (before is null || next.Containers.Values.All(container => container.Coverage == InventoryCoverage.Complete)) return next;
        var kept = false;
        var containers = next.Containers.Values.Select(container =>
        {
            if (container.Coverage == InventoryCoverage.Complete
                || !before.Containers.TryGetValue(container.Location, out var known) || known.Coverage != InventoryCoverage.Complete) return container;
            kept = true;
            return known;
        }).ToArray();
        return kept ? new InventorySnapshot(next.ObservedAt, next.Source, containers) : next;
    }

    /// <summary>The bags changed: unknown until read again, but the last reading stays for display.</summary>
    public void Invalidate() => Interlocked.Exchange(ref current, null);

    // Logout/owner changes invalidate all previous observations until a new acquisition.
    public void Clear()
    {
        Interlocked.Exchange(ref current, null);
        Interlocked.Exchange(ref shown, null);
    }
}
