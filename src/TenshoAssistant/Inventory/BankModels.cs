namespace TenshoAssistant.Inventory;

// 0.345 (g, I-03): Bags, Crystals and Currency added last (saved as numbers: existing files keep their kinds). They are the character's
// own bags, crystals and currencies, recorded so the other characters' can be seen; the logged-in character shows its live ones.
// 0.350 (g, I-09): FreeCompanyChest added last; recorded per character who opened it (the chest belongs to the company).
public enum BankStorageKind { Retainer, Saddlebag, PremiumSaddlebag, Armoury, GlamourDresser, Armoire, Bags, Crystals, Currency, FreeCompanyChest }
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
    private static readonly IReadOnlyList<int> Bags = Array.AsReadOnly(new[] { 0, 1, 2, 3 });
    private static readonly IReadOnlyList<int> Crystals = Array.AsReadOnly(new[] { 2001 });
    private static readonly IReadOnlyList<int> Currency = Array.AsReadOnly(new[] { 2000 });
    // 0.350 (g, I-09): the five pages of the company chest, its gil and its crystals (InventoryType 20000–20004, 22000, 22001).
    private static readonly IReadOnlyList<int> FreeCompany = Array.AsReadOnly(new[] { 20000, 20001, 20002, 20003, 20004, 22000, 22001 });

    /// <summary>0.345: the character's own bags, crystals and currencies (recorded for the overview of every character).</summary>
    public static bool IsCharacterOwn(BankStorageKind kind) => kind is BankStorageKind.Bags or BankStorageKind.Crystals or BankStorageKind.Currency;
    /// <summary>0.345: storages the sort preview can arrange (the game sorts these; a list of what may be sorted, not of what may not).</summary>
    public static bool Sortable(BankStorageKind kind) => kind is BankStorageKind.Retainer or BankStorageKind.Saddlebag or BankStorageKind.PremiumSaddlebag;

    /// <summary>
    /// 0.767 (I10): a retainer's gil (12000) and crystals (12001), recorded with its pages when the game has them loaded; a copy saved before has none, and stays valid.
    /// Read on 2026-11-07 with the retainer window open: both loaded, 11 and 18 slots (docs/evidence/0.767-retainer-gil-crystals.md).
    /// </summary>
    public static IReadOnlyList<int> OptionalContainerIds(BankStorageKind kind) => kind == BankStorageKind.Retainer ? RetainerExtras : [];
    private static readonly IReadOnlyList<int> RetainerExtras = Array.AsReadOnly(new[] { 12000, 12001 });
    public const int RetainerGil = 12000, RetainerCrystals = 12001;

    public static IReadOnlyList<int> ContainerIds(BankStorageKind kind) => kind switch
    {
        BankStorageKind.Retainer => Retainer,
        BankStorageKind.Saddlebag => Saddlebag,
        BankStorageKind.PremiumSaddlebag => Premium,
        BankStorageKind.Armoury => Armoury,
        // Glamour dresser and armoire (0.236): one virtual container each (not game InventoryType ids), filled from MirageManager and Cabinet.
        BankStorageKind.GlamourDresser => [25000],
        BankStorageKind.Armoire => [25001],
        BankStorageKind.Bags => Bags,
        BankStorageKind.Crystals => Crystals,
        BankStorageKind.Currency => Currency,
        BankStorageKind.FreeCompanyChest => FreeCompany,
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
        var optional = BankStorageScope.OptionalContainerIds(entry.Key.Kind);
        var owner = entry.Key.Kind == BankStorageKind.Retainer ? entry.Key.StorageId : entry.Key.CharacterId;
        var present = entry.Snapshot.Containers.Values.Select(c => c.Location.Container.Value).ToHashSet();
        if (!expected.All(present.Contains) || entry.Snapshot.Containers.Values.Any(c =>
            c.Location.Owner.Value != owner || !expected.Contains(c.Location.Container.Value) && !optional.Contains(c.Location.Container.Value)
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

    /// <summary>
    /// X-141 (0.1118): one targeted change persisted (a storage written or forgotten, or a character erased on the player's command) and
    /// the entries to show afterwards. This default saves the whole list (one file, one writer); PerCharacterBankStore reads the character
    /// again under the profile's lock so that what another client saved is kept.
    /// </summary>
    BankEntry[] Apply(BankChange change, IReadOnlyList<BankEntry> memory)
    {
        var next = change.On(memory);
        // 0.605: one file, one writer: the memory is what is saved, an unchanged reading needs no disk.
        if (change.SkipWhenSame) return next;
        Save(Array.AsReadOnly(next));
        return next;
    }
}

/// <summary>X-141 (0.1118): one change of the bank: Put (a storage read), Forget (one storage, on a click) or Erase (every storage of one
/// character, on « Erase this character » only).</summary>
/// <remarks>X-182 (0.1173, night review N26): SkipWhenSame, a reading the client believes unchanged: the store writes it only when what is saved differs.</remarks>
public sealed record BankChange(BankEntry? Put = null, BankKey? Forget = null, ulong Erase = 0, bool SkipWhenSame = false)
{
    public ulong Character => Put?.Key.CharacterId ?? Forget?.CharacterId ?? Erase;

    /// <summary>The change applied to a list of entries.</summary>
    public BankEntry[] On(IEnumerable<BankEntry> entries) => Forget is { } forgotten ? entries.Where(e => e.Key != forgotten).ToArray()
        : Erase != 0 ? entries.Where(e => e.Key.CharacterId != Erase).ToArray()
        : entries.Where(e => e.Key != Put!.Key).Append(Put!).ToArray();
}
