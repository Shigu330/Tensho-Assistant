namespace TenshoAssistant.Inventory;

public enum StorageFilter { All, PlayerBags, Chocobo, Retainers }
public enum StorageCategory { PlayerBags, Chocobo, Retainer, Armoury, GlamourDresser, Armoire, FreeCompany }

/// <summary>Which storage a row is, for its translated name (0.322: the names were French literals shown to every player).</summary>
public enum StorageLabel { PlayerBags, Crystals, SaddlebagMissing, Saddlebag, PremiumSaddlebag, Retainer, RetainersMissing, Armoury, GlamourDresser, Armoire, FreeCompanyChest, Worn }

/// <summary>A storage's quantities; <see cref="Name"/> is the retainer's own name (from the game) and empty for the other storages,
/// whose shown name comes from <see cref="Label"/>.</summary>
public sealed record StorageHolding(StorageCategory Category, string Name, InventoryCoverage Coverage,
    bool IsCached, DateTimeOffset? ObservedAt, long Nq, long Hq)
{
    public StorageLabel Label { get; init; }
}

public sealed record ItemHoldingsSummary(IReadOnlyList<StorageHolding> Rows, long Nq, long Hq,
    bool HasObservation, bool HasUnknown, bool HasCached, bool IsExact)
{
    public long Total => checked(Nq + Hq);
}

/// <summary>Pure projection of live bags and dated imports; cached sums are never current exact totals.</summary>
public static class ItemHoldings
{
    public static ItemHoldingsSummary Build(uint itemId, ulong characterId, InventorySnapshot? live,
        IReadOnlyList<BankEntry> bankEntries, StorageFilter filter = StorageFilter.All, bool includeCrystals = false, bool crystal = false)
    {
        if (itemId == 0) throw new ArgumentOutOfRangeException(nameof(itemId));
        ArgumentNullException.ThrowIfNull(bankEntries);
        if (!Enum.IsDefined(filter)) throw new ArgumentOutOfRangeException(nameof(filter));

        var rows = new List<StorageHolding>();
        if (filter is StorageFilter.All or StorageFilter.PlayerBags)
        {
            rows.Add(characterId == 0 || live is null
                ? Unknown(StorageCategory.PlayerBags, "") with { Label = StorageLabel.PlayerBags }
                : Observe(StorageCategory.PlayerBags, "", false, live,
                    Enumerable.Range(0, 4).Select(id => new InventoryLocation(new(characterId), new(id)))) with { Label = StorageLabel.PlayerBags });
            if (includeCrystals && characterId != 0 && live is not null)
                rows.Add(Observe(StorageCategory.PlayerBags, "", false, live,
                    [new InventoryLocation(new(characterId), new(2001))]) with { Label = StorageLabel.Crystals });
            // 0.549 (I0): what is worn counts as owned, on its own line. A copy saved before this version has no such container: no line, not an unknown one.
            if (characterId != 0 && live is not null && live.Containers.ContainsKey(new InventoryLocation(new(characterId), new(1000))))
                rows.Add(Observe(StorageCategory.PlayerBags, "", false, live,
                    [new InventoryLocation(new(characterId), new(1000))]) with { Label = StorageLabel.Worn });
        }

        // Armoury chest (0.235): counted with the character's own storage when it has been read.
        if (filter is StorageFilter.All or StorageFilter.PlayerBags)
            foreach (var entry in bankEntries)
                if (characterId != 0 && entry.Key.CharacterId == characterId && entry.Key.Kind is BankStorageKind.Armoury or BankStorageKind.GlamourDresser or BankStorageKind.Armoire)
                    rows.Add(ObserveBank(entry, entry.Key.Kind switch { BankStorageKind.GlamourDresser => StorageCategory.GlamourDresser, BankStorageKind.Armoire => StorageCategory.Armoire, _ => StorageCategory.Armoury }, "")
                        with { Label = entry.Key.Kind switch { BankStorageKind.GlamourDresser => StorageLabel.GlamourDresser, BankStorageKind.Armoire => StorageLabel.Armoire, _ => StorageLabel.Armoury } });

        if (filter is StorageFilter.All or StorageFilter.Chocobo)
        {
            var hasStandard = false;
            foreach (var entry in bankEntries)
            {
                if (characterId == 0 || entry.Key.CharacterId != characterId
                    || entry.Key.Kind is not (BankStorageKind.Saddlebag or BankStorageKind.PremiumSaddlebag)) continue;
                hasStandard |= entry.Key.Kind == BankStorageKind.Saddlebag;
                rows.Add(ObserveBank(entry, StorageCategory.Chocobo, "") with { Label = entry.Key.Kind == BankStorageKind.Saddlebag ? StorageLabel.Saddlebag : StorageLabel.PremiumSaddlebag });
            }
            if (!hasStandard) rows.Add(Unknown(StorageCategory.Chocobo, "") with { Label = StorageLabel.SaddlebagMissing });
        }

        if (filter is StorageFilter.All or StorageFilter.Retainers)
        {
            var hasRetainer = false;
            foreach (var entry in bankEntries)
            {
                if (characterId == 0 || entry.Key.CharacterId != characterId
                    || entry.Key.Kind != BankStorageKind.Retainer) continue;
                hasRetainer = true;
                var row = ObserveBank(entry, StorageCategory.Retainer, entry.Name) with { Label = StorageLabel.Retainer };
                // 0.329: a retainer's crystal container was never imported: for a crystal its row is a lower bound, not a known zero.
                // 0.767 (I10): a copy that has it counts it, and is exact.
                var hasCrystals = entry.Snapshot.Containers.Keys.Any(location => location.Container.Value == BankStorageScope.RetainerCrystals);
                rows.Add(crystal && !hasCrystals && row.Coverage == InventoryCoverage.Complete ? row with { Coverage = InventoryCoverage.Partial } : row);
            }
            if (!hasRetainer) rows.Add(Unknown(StorageCategory.Retainer, "") with { Label = StorageLabel.RetainersMissing });
        }

        // 0.350 (g, I-09): the company chest, as recorded when this character last opened it (no row when never opened: it may not exist).
        if (filter is StorageFilter.All)
            foreach (var entry in bankEntries)
                if (characterId != 0 && entry.Key.CharacterId == characterId && entry.Key.Kind == BankStorageKind.FreeCompanyChest)
                    rows.Add(ObserveBank(entry, StorageCategory.FreeCompany, "") with { Label = StorageLabel.FreeCompanyChest });

        long nq = 0, hq = 0;
        foreach (var row in rows)
        {
            nq = checked(nq + row.Nq);
            hq = checked(hq + row.Hq);
        }
        return new(Array.AsReadOnly(rows.ToArray()), nq, hq,
            rows.Any(r => r.Coverage != InventoryCoverage.Unknown),
            rows.Any(r => r.Coverage != InventoryCoverage.Complete),
            rows.Any(r => r.IsCached && r.Coverage != InventoryCoverage.Unknown),
            // 0.329: every summed row counts, not only the bags: a cached armoury, dresser or armoire copy is never a current total.
            filter == StorageFilter.PlayerBags && rows.All(r => !r.IsCached && r.Coverage == InventoryCoverage.Complete));

        StorageHolding ObserveBank(BankEntry entry, StorageCategory category, string name)
        {
            var owner = entry.Key.Kind == BankStorageKind.Retainer ? entry.Key.StorageId : characterId;
            // A malformed owner cannot be queried; it conveys no observation of this storage.
            if (owner == 0) return Unknown(category, name) with { IsCached = true };
            return Observe(category, name, true, entry.Snapshot,
                BankStorageScope.ContainerIds(entry.Key.Kind).Concat(BankStorageScope.OptionalContainerIds(entry.Key.Kind)
                    .Where(id => entry.Snapshot.Containers.ContainsKey(new InventoryLocation(new(owner), new(id)))))
                    .Select(id => new InventoryLocation(new(owner), new(id))));
        }

        StorageHolding Observe(StorageCategory category, string name, bool cached,
            InventorySnapshot snapshot, IEnumerable<InventoryLocation> scope)
        {
            var locations = scope.ToArray();
            var nqQuantity = snapshot.GetQuantity(new(new(itemId), false), locations);
            var hqQuantity = snapshot.GetQuantity(new(new(itemId), true), locations);
            return new(category, name, nqQuantity.Coverage, cached,
                nqQuantity.Coverage == InventoryCoverage.Unknown ? null : snapshot.ObservedAt,
                nqQuantity.ObservedQuantity, hqQuantity.ObservedQuantity);
        }
    }

    private static StorageHolding Unknown(StorageCategory category, string name) =>
        new(category, name, InventoryCoverage.Unknown, false, null, 0, 0);
}
