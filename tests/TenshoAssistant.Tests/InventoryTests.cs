using TenshoAssistant.Inventory;

internal static class InventoryTests
{
    private static readonly InventoryLocation Bag = new(new(1), new(0));
    private static readonly InventoryLocation OtherBag = new(new(1), new(1));
    private static readonly InventoryLocation Retainer = new(new(2), new(0));
    private static readonly InventoryItem Nq = new(new(42), false);
    private static readonly InventoryItem Hq = new(new(42), true);
    private static readonly DateTimeOffset Time = new(2026, 10, 27, 15, 0, 0, TimeSpan.FromHours(1));

    public static (string Name, Action Run)[] Cases =>
    [
        ("Inventory absence is zero only in complete scope", () =>
        {
            foreach (var coverage in Enum.GetValues<InventoryCoverage>())
            {
                var result = Snapshot(new InventoryContainerSnapshot(Bag, coverage, [])).GetQuantity(Nq, [Bag]);
                Check(result.Coverage == coverage && result.ObservedQuantity == 0, "coverage retained");
                Check(result.ExactQuantity == (coverage == InventoryCoverage.Complete ? 0L : null), "unknown is not zero");
            }
            var outside = Snapshot(new InventoryContainerSnapshot(Bag, InventoryCoverage.Complete, [])).GetQuantity(Nq, [Retainer]);
            Check(outside.Coverage == InventoryCoverage.Unknown && outside.ExactQuantity is null, "owner outside scope unknown");
            Check(Snapshot().GetQuantity(Nq, [Bag]).ExactQuantity is null, "empty snapshot is not complete");
        }),
        ("Inventory partial counts remain lower bounds", () =>
        {
            var snapshot = Snapshot(new InventoryContainerSnapshot(Bag, InventoryCoverage.Partial, [new(Nq, 7)]));
            var result = snapshot.GetQuantity(Nq, [Bag]);
            Check(result.ObservedQuantity == 7 && result.ExactQuantity is null, "partial positive count not exact");
            Check(snapshot.GetQuantity(Hq, [Bag]).ExactQuantity is null, "missing HQ still unknown");
        }),
        ("Inventory owner, container and quality isolation", () =>
        {
            var snapshot = Snapshot(
                new(Bag, InventoryCoverage.Complete, [new(Nq, 3), new(Hq, 2), new(Nq, 4)]),
                new(OtherBag, InventoryCoverage.Complete, [new(Nq, 5)]),
                new(Retainer, InventoryCoverage.Complete, [new(Nq, 100)]));
            Check(snapshot.GetQuantity(Nq, [Bag]).ExactQuantity == 7, "stacks aggregate");
            Check(snapshot.GetQuantity(Hq, [Bag]).ExactQuantity == 2, "HQ separated");
            Check(snapshot.GetQuantity(Nq, [Bag, OtherBag]).ExactQuantity == 12, "only requested bags");
            Check(snapshot.GetQuantity(Nq, [Retainer]).ExactQuantity == 100, "retainer separated");
            Check(snapshot.GetQuantity(Nq, [Bag, Bag]).ExactQuantity == 7, "duplicate query scopes not double counted");
        }),
        ("Inventory mixed scope cannot claim completeness", () =>
        {
            var snapshot = Snapshot(new InventoryContainerSnapshot(Bag, InventoryCoverage.Complete, [new(Nq, 3)]));
            var result = snapshot.GetQuantity(Nq, [Bag, OtherBag]);
            Check(result.Coverage == InventoryCoverage.Partial && result.ObservedQuantity == 3
                && result.ExactQuantity is null, "unobserved bag prevents exact total");
            var unknown = Snapshot(new InventoryContainerSnapshot(Bag, InventoryCoverage.Unknown, [])).GetQuantity(Nq, [Bag, OtherBag]);
            Check(unknown.Coverage == InventoryCoverage.Unknown, "all unknown remains unknown");
        }),
        ("Inventory snapshot copies inputs and exposes immutable containers", () =>
        {
            var stacks = new List<InventoryStack> { new(Nq, 3) };
            var containers = new List<InventoryContainerSnapshot> { new(Bag, InventoryCoverage.Complete, stacks) };
            var snapshot = new InventorySnapshot(Time, "test", containers);
            stacks.Clear();
            containers.Clear();
            Check(snapshot.GetQuantity(Nq, [Bag]).ExactQuantity == 3, "caller changes cannot alter snapshot");
            Check(snapshot.Containers.Count == 1, "container list copied");
            Check(snapshot.Containers is not Dictionary<InventoryLocation, InventoryContainerSnapshot>, "no mutable dictionary exposed");
            Check(snapshot.ObservedAt.Offset == TimeSpan.Zero && snapshot.ObservedAt == Time, "timestamp normalized preserving instant");
            Check(snapshot.Source == "test", "source retained");
        }),
        ("Inventory service replaces and clears rather than merging", () =>
        {
            var service = new InventoryService();
            Check(service.Current is null, "initial state unknown");
            var first = Snapshot(new InventoryContainerSnapshot(Bag, InventoryCoverage.Complete, [new(Nq, 3)]));
            service.Replace(first);
            service.Replace(Snapshot(new InventoryContainerSnapshot(Retainer, InventoryCoverage.Complete, [])));
            Check(service.Current!.GetQuantity(Nq, [Bag]).ExactQuantity is null, "old owner not retained");
            Check(first.GetQuantity(Nq, [Bag]).ExactQuantity == 3, "existing readers retain old immutable snapshot");
            service.Clear();
            Check(service.Current is null, "logout clears published state");
            Throws<ArgumentNullException>(() => service.Replace(null!));
        }),
        ("Inventory rejects contradictory or invalid observations", () =>
        {
            Throws<ArgumentException>(() => new InventoryContainerSnapshot(Bag, InventoryCoverage.Unknown, [new(Nq, 1)]));
            Throws<ArgumentException>(() => new InventoryContainerSnapshot(Bag, InventoryCoverage.Complete, [new(Nq, -1)]));
            Throws<ArgumentException>(() => new InventoryContainerSnapshot(Bag, InventoryCoverage.Complete, [new(Nq, 0)]));
            Throws<ArgumentException>(() => new InventoryContainerSnapshot(Bag, InventoryCoverage.Complete, [new(default, 1)]));
            Throws<ArgumentException>(() => new InventoryContainerSnapshot(default, InventoryCoverage.Complete, []));
            Throws<ArgumentOutOfRangeException>(() => new InventoryContainerSnapshot(Bag, (InventoryCoverage)99, []));
            Throws<ArgumentException>(() => Snapshot(new(Bag, InventoryCoverage.Complete, []), new(Bag, InventoryCoverage.Complete, [])));
            Throws<ArgumentException>(() => new InventorySnapshot(Time, " ", []));
            Throws<ArgumentException>(() => new InventorySnapshot(default, "test", []));
            Throws<ArgumentException>(() => Snapshot().GetQuantity(Nq, []));
            Throws<ArgumentException>(() => Snapshot().GetQuantity(default, [Bag]));
            Throws<ArgumentException>(() => Snapshot().GetQuantity(Nq, [default]));
        }),
        ("Inventory arithmetic never wraps", () =>
        {
            Throws<OverflowException>(() => new InventoryContainerSnapshot(Bag, InventoryCoverage.Complete,
                [new(Nq, long.MaxValue), new(Nq, 1)]));
            var snapshot = Snapshot(new(Bag, InventoryCoverage.Complete, [new(Nq, long.MaxValue)]),
                new(OtherBag, InventoryCoverage.Complete, [new(Nq, 1)]));
            Throws<OverflowException>(() => snapshot.GetQuantity(Nq, [Bag, OtherBag]));
        }),
    ];

    private static InventorySnapshot Snapshot(params InventoryContainerSnapshot[] containers) => new(Time, "test", containers);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}");
    }
}
