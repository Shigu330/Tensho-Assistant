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
        Interlocked.Exchange(ref shown, snapshot);
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
