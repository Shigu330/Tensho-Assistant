using TenshoAssistant.Inventory;

internal static class InventoryCaptureTests
{
    private static readonly InventoryOwnerId Owner = new(1);
    private static readonly InventoryItem Item = new(new(42), false);
    private static InventorySnapshot Batch(InventoryOwnerId owner, InventoryCoverage coverage = InventoryCoverage.Complete) =>
        new(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero), "test",
            [new(new(owner, new(0)), coverage, coverage == InventoryCoverage.Unknown ? [] : [new(Item, 3)])]);

    public static (string Name, Action Run)[] Cases =>
    [
        ("Capture initial observation and event bursts are bounded", () =>
        {
            var service = new InventoryService();
            var reads = 0;
            using var coordinator = new InventoryCaptureCoordinator(service, () => Owner,
                owner => { reads++; return Batch(owner); }, _ => throw new Exception("unexpected error"));
            coordinator.Tick(TimeSpan.Zero);
            Check(reads == 1 && service.Current is not null, "initial capture");
            coordinator.Tick(TimeSpan.FromSeconds(10));
            Check(reads == 1, "no polling of already observed bags");
            for (var i = 0; i < 100; i++) coordinator.Invalidate();
            Check(service.Current is null, "event immediately invalidates old data");
            coordinator.Tick(TimeSpan.FromSeconds(10));
            Check(reads == 2, "burst coalesced");
            coordinator.Invalidate();
            coordinator.Tick(TimeSpan.FromSeconds(10.1));
            Check(reads == 2 && service.Current is null, "throttled data stays unknown");
            coordinator.Tick(TimeSpan.FromSeconds(10.25));
            Check(reads == 3, "capture after throttle");
        }),
        ("Capture never retains prior character after logout or owner change", () =>
        {
            var service = new InventoryService();
            InventoryOwnerId? currentOwner = Owner;
            var reads = 0;
            using var coordinator = new InventoryCaptureCoordinator(service, () => currentOwner,
                owner => { reads++; return Batch(owner); }, _ => { });
            coordinator.Tick(TimeSpan.Zero);
            currentOwner = null;
            coordinator.Tick(TimeSpan.FromSeconds(1));
            Check(service.Current is null && reads == 1, "logout does not read bags");
            currentOwner = new InventoryOwnerId(2);
            coordinator.Tick(TimeSpan.FromSeconds(2));
            Check(service.Current!.Containers.Keys.All(k => k.Owner.Value == 2), "new character only");
            currentOwner = Owner;
            coordinator.Tick(TimeSpan.FromSeconds(2.01));
            Check(service.Current!.Containers.Keys.All(k => k.Owner == Owner), "owner switch bypasses stale throttle");
        }),
        ("Unavailable bags retry on monotonic schedule", () =>
        {
            var service = new InventoryService();
            var reads = 0;
            using var coordinator = new InventoryCaptureCoordinator(service, () => Owner,
                owner => Batch(owner, ++reads == 1 ? InventoryCoverage.Unknown : InventoryCoverage.Complete), _ => { });
            coordinator.Tick(TimeSpan.Zero);
            Check(service.Current!.Containers.Values.Single().Coverage == InventoryCoverage.Unknown, "unknown retained");
            coordinator.Tick(TimeSpan.FromSeconds(4.99));
            Check(reads == 1, "no per-frame reads");
            coordinator.Tick(TimeSpan.FromSeconds(5));
            Check(reads == 2, "retry unavailable bags");
            coordinator.Tick(TimeSpan.FromHours(1));
            Check(reads == 2, "stop retries once all bags observed");
        }),
        ("Capture failure clears data and rate limits retries and logs", () =>
        {
            var service = new InventoryService();
            service.Replace(Batch(Owner));
            var errors = 0;
            var reads = 0;
            var fail = true;
            using var coordinator = new InventoryCaptureCoordinator(service, () => Owner,
                owner => { reads++; return fail ? throw new InvalidOperationException() : Batch(owner); }, _ => errors++);
            coordinator.Tick(TimeSpan.Zero);
            Check(service.Current is null && errors == 1, "failure invalidates");
            coordinator.Tick(TimeSpan.FromSeconds(1));
            coordinator.Tick(TimeSpan.FromSeconds(5));
            Check(reads == 2 && errors == 1, "retry throttled and error logged once");
            fail = false;
            coordinator.Tick(TimeSpan.FromSeconds(10));
            Check(service.Current is not null, "recovery");
            fail = true;
            coordinator.Invalidate();
            coordinator.Tick(TimeSpan.FromSeconds(11));
            Check(errors == 2 && service.Current is null, "new failure after recovery logged");
        }),
        ("Capture discards invalidated or changed-owner in-flight batches", () =>
        {
            var service = new InventoryService();
            InventoryOwnerId? currentOwner = Owner;
            InventoryCaptureCoordinator? coordinator = null;
            var invalidateDuringRead = true;
            using (coordinator = new InventoryCaptureCoordinator(service, () => currentOwner, owner =>
            {
                if (invalidateDuringRead) coordinator!.Invalidate();
                else currentOwner = new InventoryOwnerId(2);
                return Batch(owner);
            }, _ => { }))
            {
                coordinator.Tick(TimeSpan.Zero);
                Check(service.Current is null, "invalidation during read prevents stale publication");
                invalidateDuringRead = false;
                coordinator.Tick(TimeSpan.FromSeconds(1));
                Check(service.Current is null, "owner changed during read prevents publication");
            }
        }),
        ("Capture rejects a batch from another owner", () =>
        {
            var service = new InventoryService();
            var errors = 0;
            using var coordinator = new InventoryCaptureCoordinator(service, () => Owner,
                _ => Batch(new InventoryOwnerId(2)), _ => errors++);
            coordinator.Tick(TimeSpan.Zero);
            Check(service.Current is null && errors == 1, "wrong-owner batch rejected");
        }),
        ("Capture disposal prevents future reads and publication", () =>
        {
            var service = new InventoryService();
            var reads = 0;
            InventoryCaptureCoordinator? coordinator = null;
            coordinator = new InventoryCaptureCoordinator(service, () => Owner,
                owner => { reads++; coordinator!.Dispose(); return Batch(owner); }, _ => { });
            coordinator.Tick(TimeSpan.Zero);
            Check(service.Current is null, "dispose during capture prevents publication");
            coordinator.Invalidate();
            coordinator.Tick(TimeSpan.FromSeconds(10));
            coordinator.Dispose();
            Check(reads == 1 && service.Current is null, "no work after dispose");
        }),
    ];

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
