namespace TenshoAssistant.Inventory;

/// <summary>Serializes publication, invalidation and disposal; never schedules work itself.</summary>
internal sealed class InventoryCaptureCoordinator(
    InventoryService service,
    Func<InventoryOwnerId?> readOwner,
    Func<InventoryOwnerId, InventorySnapshot> capture,
    Action<Exception> reportError) : IDisposable
{
    private readonly object gate = new();
    private InventoryOwnerId? owner;
    private bool dirty = true;
    private bool disposed;
    private bool errorReported;
    private long generation;
    private TimeSpan nextCapture;

    public void Invalidate()
    {
        lock (gate)
        {
            if (disposed) return;
            generation++;
            dirty = true;
            service.Clear();
        }
    }

    // Only the adapter's framework callback may invoke this method. now is monotonic.
    public void Tick(TimeSpan now)
    {
        lock (gate)
        {
            if (disposed) return;
            try
            {
                var currentOwner = readOwner();
                if (currentOwner is null || currentOwner.Value.Value == 0)
                {
                    owner = null;
                    dirty = true;
                    nextCapture = now;
                    service.Clear();
                    return;
                }

                if (owner != currentOwner)
                {
                    owner = currentOwner;
                    dirty = true;
                    nextCapture = now;
                    service.Clear();
                }

                if (!dirty || now < nextCapture) return;
                nextCapture = now + TimeSpan.FromMilliseconds(250);
                var startedGeneration = generation;
                var snapshot = capture(currentOwner.Value);
                if (disposed || generation != startedGeneration || readOwner() != currentOwner)
                {
                    dirty = true;
                    service.Clear();
                    return;
                }

                // A mismatched batch is a producer error, never data for the current character.
                if (snapshot.Containers.Count == 0 || snapshot.Containers.Keys.Any(k => k.Owner != currentOwner.Value))
                    throw new InvalidOperationException("Capture did not contain exclusively the current owner's containers.");
                service.Replace(snapshot);
                dirty = snapshot.Containers.Values.Any(c => c.Coverage != InventoryCoverage.Complete);
                if (dirty) nextCapture = now + TimeSpan.FromSeconds(5);
                errorReported = false;
            }
            catch (Exception error)
            {
                dirty = true;
                service.Clear();
                nextCapture = now + TimeSpan.FromSeconds(5);
                if (!errorReported)
                {
                    errorReported = true;
                    reportError(error);
                }
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
            service.Clear();
        }
    }
}
