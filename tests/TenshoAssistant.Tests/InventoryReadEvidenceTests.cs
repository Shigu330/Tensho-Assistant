using TenshoAssistant.Inventory;

internal static class InventoryReadEvidenceTests
{
    private static readonly InventoryReadEvidence Loaded = new(true, true, true, 35);

    public static (string Name, Action Run)[] Cases =>
    [
        ("Loaded stable full-span bags prove completeness", () =>
        {
            Check(InventoryReadEvidence.GetCoverage(Loaded, Loaded, 35, true) == InventoryCoverage.Complete,
                "loaded header, stable buffer and all slots required");
            var container = new InventoryContainerSnapshot(new(new(1), new(0)), InventoryCoverage.Complete, []);
            var snapshot = new InventorySnapshot(DateTimeOffset.UtcNow, "test", [container]);
            Check(snapshot.GetQuantity(new(new(12892), false), [container.Location]).ExactQuantity == 0,
                "a fully read bag without the item proves zero in that bag");
        }),
        ("Unavailable headers cannot prove completeness from slot count", () =>
        {
            InventoryReadEvidence[] invalid =
            [default, Loaded with { IsLoaded = false }, Loaded with { HasItems = false },
                Loaded with { MatchesType = false }, Loaded with { Size = 0 },
                Loaded with { Size = -1 }, Loaded with { Size = 1001 }];
            foreach (var evidence in invalid)
            {
                Check(!evidence.CanRead, "unavailable header cannot be read");
                Check(InventoryReadEvidence.GetCoverage(evidence, Loaded, 35, true) == InventoryCoverage.Unknown,
                    "missing initial evidence is unknown");
                Check(InventoryReadEvidence.GetCoverage(Loaded, evidence, 35, true) == InventoryCoverage.Unknown,
                    "unloading or invalid header after reading invalidates observation");
            }
        }),
        ("Incomplete or unstable native reads never claim an exact quantity", () =>
        {
            Check(InventoryReadEvidence.GetCoverage(Loaded, Loaded, 0, true) == InventoryCoverage.Partial, "empty span not empty bag");
            Check(InventoryReadEvidence.GetCoverage(Loaded, Loaded, 34, true) == InventoryCoverage.Partial, "missing slot");
            Check(InventoryReadEvidence.GetCoverage(Loaded, Loaded, 36, true) == InventoryCoverage.Partial, "extra slot");
            Check(InventoryReadEvidence.GetCoverage(Loaded, Loaded, 35, false) == InventoryCoverage.Partial, "buffer changed");
            Check(InventoryReadEvidence.GetCoverage(Loaded, Loaded with { Size = 36 }, 35, true) == InventoryCoverage.Partial,
                "size changed during copy");
        }),
        ("Four complete bags distinguish zero NQ and positive HQ", () =>
        {
            var containers = Enumerable.Range(0, 4).Select(i => new InventoryContainerSnapshot(new(new(1), new(i)),
                InventoryCoverage.Complete, i == 2 ? [new(new(new(12892), true), 156)] : [])).ToArray();
            var snapshot = new InventorySnapshot(DateTimeOffset.UtcNow, "test", containers);
            var scope = containers.Select(c => c.Location).ToArray();
            Check(snapshot.GetQuantity(new(new(12892), false), scope).ExactQuantity == 0, "zero NQ exact");
            Check(snapshot.GetQuantity(new(new(12892), true), scope).ExactQuantity == 156, "156 HQ exact");
            var missingBag = new InventoryLocation(new(1), new(4));
            Check(snapshot.GetQuantity(new(new(12892), true), scope.Append(missingBag)).ExactQuantity is null,
                "other storage excluded from exact total");
        }),
        ("Partial bag evidence is retried until complete", () =>
        {
            var service = new InventoryService();
            var reads = 0;
            using var coordinator = new InventoryCaptureCoordinator(service, () => new InventoryOwnerId(1), owner =>
                new InventorySnapshot(DateTimeOffset.UtcNow, "test", [new(new(owner, new(0)),
                    ++reads == 1 ? InventoryCoverage.Partial : InventoryCoverage.Complete, [])]), _ => { });
            coordinator.Tick(TimeSpan.Zero);
            Check(service.Current!.Containers.Values.Single().Coverage == InventoryCoverage.Partial, "partial first");
            coordinator.Tick(TimeSpan.FromSeconds(4));
            Check(reads == 1, "retry bounded");
            coordinator.Tick(TimeSpan.FromSeconds(5));
            Check(service.Current!.Containers.Values.Single().Coverage == InventoryCoverage.Complete, "complete after retry");
            coordinator.Tick(TimeSpan.FromSeconds(10));
            Check(reads == 2, "stop background scans once complete");
        }),
    ];

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
