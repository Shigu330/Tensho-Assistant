namespace TenshoAssistant.Inventory;

public enum StorageFilter { All, PlayerBags, Chocobo, Retainers }
public enum StorageCategory { PlayerBags, Chocobo, Retainer, Armoury, GlamourDresser, Armoire }

public sealed record StorageHolding(StorageCategory Category, string Name, InventoryCoverage Coverage,
    bool IsCached, DateTimeOffset? ObservedAt, long Nq, long Hq);

public sealed record ItemHoldingsSummary(IReadOnlyList<StorageHolding> Rows, long Nq, long Hq,
    bool HasObservation, bool HasUnknown, bool HasCached, bool IsExact)
{
    public long Total => checked(Nq + Hq);
}

/// <summary>Pure projection of live bags and dated imports; cached sums are never current exact totals.</summary>
public static class ItemHoldings
{
    public static ItemHoldingsSummary Build(uint itemId, ulong characterId, InventorySnapshot? live,
        IReadOnlyList<BankEntry> bankEntries, StorageFilter filter = StorageFilter.All, bool includeCrystals = false)
    {
        if (itemId == 0) throw new ArgumentOutOfRangeException(nameof(itemId));
        ArgumentNullException.ThrowIfNull(bankEntries);
        if (!Enum.IsDefined(filter)) throw new ArgumentOutOfRangeException(nameof(filter));

        var rows = new List<StorageHolding>();
        if (filter is StorageFilter.All or StorageFilter.PlayerBags)
        {
            rows.Add(characterId == 0 || live is null
                ? Unknown(StorageCategory.PlayerBags, "Sacs du personnage")
                : Observe(StorageCategory.PlayerBags, "Sacs du personnage", false, live,
                    Enumerable.Range(0, 4).Select(id => new InventoryLocation(new(characterId), new(id)))));
            if (includeCrystals && characterId != 0 && live is not null)
                rows.Add(Observe(StorageCategory.PlayerBags, "Cristaux", false, live,
                    [new InventoryLocation(new(characterId), new(2001))]));
        }

        // Armoury chest (0.235): counted with the character's own storage when it has been read.
        if (filter is StorageFilter.All or StorageFilter.PlayerBags)
            foreach (var entry in bankEntries)
                if (characterId != 0 && entry.Key.CharacterId == characterId && entry.Key.Kind is BankStorageKind.Armoury or BankStorageKind.GlamourDresser or BankStorageKind.Armoire)
                    rows.Add(ObserveBank(entry, entry.Key.Kind switch { BankStorageKind.GlamourDresser => StorageCategory.GlamourDresser, BankStorageKind.Armoire => StorageCategory.Armoire, _ => StorageCategory.Armoury }, entry.Name));

        if (filter is StorageFilter.All or StorageFilter.Chocobo)
        {
            var hasStandard = false;
            foreach (var entry in bankEntries)
            {
                if (characterId == 0 || entry.Key.CharacterId != characterId
                    || entry.Key.Kind is not (BankStorageKind.Saddlebag or BankStorageKind.PremiumSaddlebag)) continue;
                hasStandard |= entry.Key.Kind == BankStorageKind.Saddlebag;
                rows.Add(ObserveBank(entry, StorageCategory.Chocobo, entry.Name));
            }
            if (!hasStandard) rows.Add(Unknown(StorageCategory.Chocobo, "Sacoche chocobo non importée"));
        }

        if (filter is StorageFilter.All or StorageFilter.Retainers)
        {
            var hasRetainer = false;
            foreach (var entry in bankEntries)
            {
                if (characterId == 0 || entry.Key.CharacterId != characterId
                    || entry.Key.Kind != BankStorageKind.Retainer) continue;
                hasRetainer = true;
                rows.Add(ObserveBank(entry, StorageCategory.Retainer, $"Servant : {entry.Name}"));
            }
            if (!hasRetainer) rows.Add(Unknown(StorageCategory.Retainer, "Servants non importés"));
        }

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
            filter == StorageFilter.PlayerBags && rows[0].Coverage == InventoryCoverage.Complete);

        StorageHolding ObserveBank(BankEntry entry, StorageCategory category, string name)
        {
            var owner = entry.Key.Kind == BankStorageKind.Retainer ? entry.Key.StorageId : characterId;
            // A malformed owner cannot be queried; it conveys no observation of this storage.
            if (owner == 0) return Unknown(category, name) with { IsCached = true };
            return Observe(category, name, true, entry.Snapshot,
                BankStorageScope.ContainerIds(entry.Key.Kind).Select(id => new InventoryLocation(new(owner), new(id))));
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
