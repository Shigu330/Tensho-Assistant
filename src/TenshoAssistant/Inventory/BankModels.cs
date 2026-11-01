namespace TenshoAssistant.Inventory;

public enum BankStorageKind { Retainer, Saddlebag, PremiumSaddlebag, Armoury, GlamourDresser, Armoire }
public readonly record struct BankKey(ulong CharacterId, BankStorageKind Kind, ulong StorageId);
public sealed record BankEntry(BankKey Key, string Name, InventorySnapshot Snapshot);

public static class BankStorageScope
{
    private static readonly IReadOnlyList<int> Retainer = Array.AsReadOnly(Enumerable.Range(10000, 7).ToArray());
    private static readonly IReadOnlyList<int> Saddlebag = Array.AsReadOnly(new[] { 4000, 4001 });
    private static readonly IReadOnlyList<int> Premium = Array.AsReadOnly(new[] { 4100, 4101 });
    // Armoury chest (0.235): main hand, off hand, head, body, hands, legs, feet, ears, neck, wrists, rings, soul crystals (InventoryType ids;
    // the old waist container 3204 is left out).
    private static readonly IReadOnlyList<int> Armoury = Array.AsReadOnly(new[] { 3500, 3200, 3201, 3202, 3203, 3205, 3206, 3207, 3208, 3209, 3300, 3400 });

    public static IReadOnlyList<int> ContainerIds(BankStorageKind kind) => kind switch
    {
        BankStorageKind.Retainer => Retainer,
        BankStorageKind.Saddlebag => Saddlebag,
        BankStorageKind.PremiumSaddlebag => Premium,
        BankStorageKind.Armoury => Armoury,
        // Glamour dresser and armoire (0.236): one virtual container each (not game InventoryType ids), filled from MirageManager and Cabinet.
        BankStorageKind.GlamourDresser => [25000],
        BankStorageKind.Armoire => [25001],
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}

public static class BankEntryValidator
{
    public static void Validate(BankEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Key.CharacterId == 0 || !Enum.IsDefined(entry.Key.Kind)
            || (entry.Key.Kind == BankStorageKind.Retainer ? entry.Key.StorageId == 0 : entry.Key.StorageId != 0))
            throw new ArgumentException("Invalid bank owner or storage key.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Name) || entry.Name.Length > 1024 || entry.Snapshot is null
            || entry.Snapshot.Source.Length > 1024)
            throw new ArgumentException("A bank name and snapshot with bounded metadata are required.", nameof(entry));

        var expected = BankStorageScope.ContainerIds(entry.Key.Kind);
        var owner = entry.Key.Kind == BankStorageKind.Retainer ? entry.Key.StorageId : entry.Key.CharacterId;
        if (entry.Snapshot.Containers.Count != expected.Count || entry.Snapshot.Containers.Values.Any(c =>
            c.Location.Owner.Value != owner || !expected.Contains(c.Location.Container.Value)
            || c.Coverage != InventoryCoverage.Complete))
            throw new ArgumentException("Bank imports require exactly the complete containers of the selected storage group.", nameof(entry));
        // Native acquisition permits at most 1000 slots, each with an Int32 quantity.
        // Bound persisted totals too, so a modified cache cannot overflow combined search totals.
        const long maximumContainerQuantity = 1000L * int.MaxValue;
        foreach (var container in entry.Snapshot.Containers.Values)
        {
            long total = 0;
            foreach (var quantity in container.Quantities.Values)
            {
                if (quantity > maximumContainerQuantity - total)
                    throw new ArgumentException("Bank container quantity exceeds native acquisition bounds.", nameof(entry));
                total += quantity;
            }
        }
    }
}

public sealed class BankState
{
    public IReadOnlyList<BankEntry> Entries { get; }
    public bool Loading { get; }
    public bool Importing { get; }
    public string? Error { get; }

    public BankState(IReadOnlyList<BankEntry> entries, bool loading, bool importing, string? error)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = Array.AsReadOnly(entries.ToArray());
        Loading = loading;
        Importing = importing;
        Error = error;
    }
}

public sealed record BankImportResult(bool Success, string Message);

public interface IInventoryBankStore
{
    BankEntry[] Load();
    void Save(IReadOnlyList<BankEntry> entries);
}
