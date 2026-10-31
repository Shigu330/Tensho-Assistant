using TenshoAssistant.Inventory;

internal static class ItemHoldingsTests
{
    private static readonly DateTimeOffset Time = new(2026, 10, 27, 15, 0, 0, TimeSpan.FromHours(2));

    public static (string Name, Action Run)[] Cases =>
    [
        ("Holdings screenshot records 182 NQ plus 26 HQ without global exactness", () =>
        {
            var result = ItemHoldings.Build(42, 1, Bags(1, 182), [Bank(1, BankStorageKind.Saddlebag, 0, 0, 26)]);
            Check(result.Nq == 182 && result.Hq == 26 && result.Total == 208, "recorded quantities");
            Check(result.HasObservation && result.HasCached && result.HasUnknown && !result.IsExact, "mixed scope flags");
            Check(result.Rows.Count == 3 && result.Rows[2].Name == "Servants non importés", "missing retainers explicit");
            Check(result.Rows[1].ObservedAt == Time.ToUniversalTime()
                && result.Rows[1].ObservedAt!.Value.Offset == TimeSpan.Zero, "cached UTC timestamp");
        }),
        ("Holdings keeps same item and container IDs on distinct retainers", () =>
        {
            var result = ItemHoldings.Build(42, 1, null,
                [Bank(1, BankStorageKind.Retainer, 100, 7), Bank(1, BankStorageKind.Retainer, 101, 11)], StorageFilter.Retainers);
            Check(result.Rows.Count == 2 && result.Nq == 18, "both retainers counted");
            Check(result.Rows.Select(r => r.Name).Distinct().Count() == 2, "separate names");
            Check(result.HasCached && !result.HasUnknown && !result.IsExact, "complete caches still stale");
        }),
        ("Holdings restricts live bags to current owner and IDs zero through three", () =>
        {
            var live = new InventorySnapshot(Time, "test", Bags(1, 5).Containers.Values
                .Concat(Bags(2, 99).Containers.Values).Append(Container(1, 4000, 700)));
            var result = ItemHoldings.Build(42, 1, live, [], StorageFilter.PlayerBags);
            Check(result.Nq == 5 && result.IsExact && !result.HasCached && !result.HasUnknown, "exact requested bags only");
            var wrongOwner = ItemHoldings.Build(42, 1, Bags(2, 99), [], StorageFilter.PlayerBags);
            Check(wrongOwner.Total == 0 && !wrongOwner.HasObservation && wrongOwner.HasUnknown && !wrongOwner.IsExact,
                "another live owner is unknown");
            Check(wrongOwner.Rows[0].ObservedAt is null, "no fabricated observation time");
        }),
        ("Holdings excludes bank entries from other characters", () =>
        {
            var entries = new[] { Bank(2, BankStorageKind.Saddlebag, 0, 90),
                Bank(2, BankStorageKind.Retainer, 100, 80), Bank(1, BankStorageKind.Retainer, 101, 3) };
            var result = ItemHoldings.Build(42, 1, null, entries);
            Check(result.Total == 3 && result.Rows.Count == 3, "current character only");
            Check(result.Rows.All(r => !r.Name.Contains("100")), "other retainer name excluded");
        }),
        ("Holdings character zero exposes only unknown selected groups", () =>
        {
            foreach (var filter in Enum.GetValues<StorageFilter>())
            {
                var result = ItemHoldings.Build(42, 0, Bags(1, 9),
                    [Bank(1, BankStorageKind.Saddlebag, 0, 8), Bank(1, BankStorageKind.Retainer, 100, 7),
                     Bank(0, BankStorageKind.Saddlebag, 0, 6)], filter);
                Check(result.Rows.Count == (filter == StorageFilter.All ? 3 : 1), "selected groups present");
                Check(result.Total == 0 && result.HasUnknown && !result.HasObservation && !result.HasCached && !result.IsExact,
                    "no identity means no observations");
                Check(result.Rows.All(r => r.Coverage == InventoryCoverage.Unknown && r.ObservedAt is null), "unknown rows");
            }
        }),
        ("Holdings missing bags and never imported storage retain uncertainty", () =>
        {
            var live = new InventorySnapshot(Time, "test", [Container(1, 0, 6)]);
            var result = ItemHoldings.Build(42, 1, live, []);
            Check(result.Rows[0].Coverage == InventoryCoverage.Partial && result.Total == 6, "missing pages yield lower bound");
            Check(result.HasObservation && result.HasUnknown && !result.IsExact && !result.HasCached, "partial flags");
            Check(result.Rows.Skip(1).All(r => r.Coverage == InventoryCoverage.Unknown), "never imported unknown");
            var absent = ItemHoldings.Build(42, 1, null, []);
            Check(!absent.HasObservation && absent.HasUnknown && absent.Total == 0, "zero is not known absence");
        }),
        ("Holdings complete empty cache is an observed zero", () =>
        {
            var result = ItemHoldings.Build(42, 1, null, [Bank(1, BankStorageKind.Saddlebag, 0, 0)], StorageFilter.Chocobo);
            Check(result.Total == 0 && result.HasObservation && result.HasCached && !result.HasUnknown && !result.IsExact,
                "complete empty imported cache");
            Check(result.Rows.Count == 1 && result.Rows[0].ObservedAt == Time, "premium is optional");
        }),
        ("Holdings can include crystals for craft calculations", () =>
        {
            var live = new InventorySnapshot(Time, "test", [
                Container(1, 0, 10), Container(1, 1, 0), Container(1, 2, 0), Container(1, 3, 0),
                Container(1, 2001, 25)]);
            var result = ItemHoldings.Build(42, 1, live, [], StorageFilter.All, includeCrystals: true);
            Check(result.Total == 35 && result.Rows.Any(row => row.Name == "Cristaux" && row.Nq == 25),
                "crystals are included only when requested");
        }),
        ("Holdings includes optional premium but requires standard chocobo placeholder", () =>
        {
            var premium = Bank(1, BankStorageKind.PremiumSaddlebag, 0, 4);
            var result = ItemHoldings.Build(42, 1, null, [premium], StorageFilter.Chocobo);
            Check(result.Total == 4 && result.Rows.Count == 2 && result.HasUnknown, "premium does not replace standard");
            Check(result.Rows.Any(r => r.Name == "Sacoche chocobo non importée" && !r.IsCached), "standard placeholder");
            var both = ItemHoldings.Build(42, 1, null, [premium, Bank(1, BankStorageKind.Saddlebag, 0, 3)], StorageFilter.Chocobo);
            Check(both.Total == 7 && both.Rows.Count == 2 && !both.HasUnknown, "both imports included");
        }),
        ("Holdings preserves incomplete bank coverage and excludes unobserved cache flag", () =>
        {
            var entry = Bank(1, BankStorageKind.Saddlebag, 0, 12);
            var partial = entry with { Snapshot = new(Time, "partial", [Container(1, 4000, 12, coverage: InventoryCoverage.Partial)]) };
            var result = ItemHoldings.Build(42, 1, null, [partial], StorageFilter.Chocobo);
            Check(result.Nq == 12 && result.Rows[0].Coverage == InventoryCoverage.Partial && result.HasUnknown
                && result.HasObservation && result.HasCached && !result.IsExact, "partial cache lower bound");
            var unknown = entry with { Snapshot = new(Time, "unknown", [Container(1, 4000, 0, coverage: InventoryCoverage.Unknown)]) };
            result = ItemHoldings.Build(42, 1, null, [unknown], StorageFilter.Chocobo);
            Check(result.Rows[0].IsCached && !result.HasCached && !result.HasObservation && result.HasUnknown,
                "unobserved import is not an observed cache");
        }),
        ("Holdings filters select only their own rows and totals", () =>
        {
            BankEntry[] entries = [Bank(1, BankStorageKind.Saddlebag, 0, 20, 2), Bank(1, BankStorageKind.Retainer, 100, 30, 3)];
            foreach (var (filter, category, nq, hq) in new[] {
                (StorageFilter.PlayerBags, StorageCategory.PlayerBags, 10L, 0L),
                (StorageFilter.Chocobo, StorageCategory.Chocobo, 20L, 2L),
                (StorageFilter.Retainers, StorageCategory.Retainer, 30L, 3L) })
            {
                var result = ItemHoldings.Build(42, 1, Bags(1, 10), entries, filter);
                Check(result.Rows.Count == 1 && result.Rows[0].Category == category && result.Nq == nq && result.Hq == hq, "filter scope");
                Check(result.IsExact == (filter == StorageFilter.PlayerBags), "exact only live bag filter");
            }
            Check(!ItemHoldings.Build(42, 1, Bags(1, 10), entries).IsExact, "all complete still not current exact");
            Check(ItemHoldings.Build(99, 1, Bags(1, 10), [], StorageFilter.PlayerBags).Total == 0, "other item excluded");
        }),
        ("Holdings results detach from mutable inputs and reject row mutation", () =>
        {
            var entries = new List<BankEntry> { Bank(1, BankStorageKind.Saddlebag, 0, 5) };
            var result = ItemHoldings.Build(42, 1, null, entries, StorageFilter.Chocobo);
            entries.Clear();
            Check(result.Total == 5 && result.Rows.Count == 1, "caller list detached");
            Throws<NotSupportedException>(() => ((IList<StorageHolding>)result.Rows).Clear());
            Throws<NotSupportedException>(() => ((IList<StorageHolding>)result.Rows)[0] = result.Rows[0] with { Nq = 99 });
            var changed = result.Rows[0] with { Nq = 99 };
            Check(changed.Nq == 99 && result.Rows[0].Nq == 5, "record copies leave originals intact");
        }),
        ("Holdings checked arithmetic detects row totals and combined quality overflow", () =>
        {
            Throws<OverflowException>(() => ItemHoldings.Build(42, 1, null,
                [Bank(1, BankStorageKind.Retainer, 100, long.MaxValue), Bank(1, BankStorageKind.Retainer, 101, 1)], StorageFilter.Retainers));
            Throws<OverflowException>(() => ItemHoldings.Build(42, 1, null,
                [Bank(1, BankStorageKind.Retainer, 100, 0, long.MaxValue), Bank(1, BankStorageKind.Retainer, 101, 0, 1)], StorageFilter.Retainers));
            var result = ItemHoldings.Build(42, 1, null, [Bank(1, BankStorageKind.Saddlebag, 0, long.MaxValue, 1)], StorageFilter.Chocobo);
            Throws<OverflowException>(() => { _ = result.Total; });
        }),
    ];

    private static InventoryContainerSnapshot Container(ulong owner, int id, long nq, long hq = 0,
        InventoryCoverage coverage = InventoryCoverage.Complete)
    {
        var stacks = new List<InventoryStack>();
        if (nq > 0) stacks.Add(new(new(new(42), false), nq));
        if (hq > 0) stacks.Add(new(new(new(42), true), hq));
        return new(new(new(owner), new(id)), coverage, stacks);
    }

    private static InventorySnapshot Bags(ulong owner, long nq) =>
        new(Time, "live-test", Enumerable.Range(0, 4).Select(id => Container(owner, id, id == 0 ? nq : 0)));

    private static BankEntry Bank(ulong character, BankStorageKind kind, ulong storage, long nq, long hq = 0)
    {
        var owner = kind == BankStorageKind.Retainer ? storage : character;
        // Character zero is deliberately allowed in a key to test projection isolation, with a valid snapshot owner.
        if (owner == 0) owner = 999;
        var ids = BankStorageScope.ContainerIds(kind);
        return new(new(character, kind, storage), $"{kind} {storage}",
            new(Time, "cache-test", ids.Select(id => Container(owner, id, id == ids[0] ? nq : 0, id == ids[0] ? hq : 0))));
    }

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
