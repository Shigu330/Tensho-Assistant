namespace TenshoAssistant.Inventory;

/// <summary>Publishes only the latest acquisition batch; callers retain immutable snapshots safely.</summary>
public sealed class InventoryService
{
    private InventorySnapshot? current;

    public InventorySnapshot? Current => Volatile.Read(ref current);

    // Publish order is authoritative, not wall-clock order. No history or merging is implicit.
    public void Replace(InventorySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Interlocked.Exchange(ref current, snapshot);
    }

    // Logout/owner changes invalidate all previous observations until a new acquisition.
    public void Clear() => Interlocked.Exchange(ref current, null);
}
