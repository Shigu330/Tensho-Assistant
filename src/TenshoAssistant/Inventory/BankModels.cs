namespace TenshoAssistant.Inventory;

public enum BankStorageKind { Retainer, Saddlebag, PremiumSaddlebag }
public readonly record struct BankKey(ulong CharacterId, BankStorageKind Kind, ulong StorageId);
public sealed record BankEntry(BankKey Key, string Name, InventorySnapshot Snapshot);

public static class BankStorageScope
{
    private static readonly IReadOnlyList<int> Retainer = Array.AsReadOnly(Enumerable.Range(10000, 7).ToArray());
    private static readonly IReadOnlyList<int> Saddlebag = Array.AsReadOnly(new[] { 4000, 4001 });
    private static readonly IReadOnlyList<int> Premium = Array.AsReadOnly(new[] { 4100, 4101 });

    public static IReadOnlyList<int> ContainerIds(BankStorageKind kind) => kind switch
    {
        BankStorageKind.Retainer => Retainer,
        BankStorageKind.Saddlebag => Saddlebag,
        BankStorageKind.PremiumSaddlebag => Premium,
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
