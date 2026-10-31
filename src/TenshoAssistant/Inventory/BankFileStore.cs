using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenshoAssistant.Inventory;

/// <summary>Schema 1 stores complete containers and aggregated (item ID, quality) counts.
/// A missing primary can be recovered from .bak; an existing corrupt primary always fails closed.
/// A failed load disables writes through this instance. There is one writer per bank path.</summary>
public sealed class FileInventoryBankStore : IInventoryBankStore
{
    private const int MaximumBytes = 16 * 1024 * 1024;
    private const int MaximumItems = 131072;
    private readonly string path;
    private readonly object gate = new();
    private bool loadFailed;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 16,
    };

    public FileInventoryBankStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        this.path = Path.GetFullPath(path);
    }

    public BankEntry[] Load()
    {
        lock (gate)
        {
            try { return ReadExisting(); }
            catch { loadFailed = true; throw; }
        }
    }

    public void Save(IReadOnlyList<BankEntry> entries)
    {
        lock (gate)
        {
            if (loadFailed) throw new InvalidOperationException("A failed bank load prevents overwriting saved data.");
            // Validate disk even if this caller skipped Load, or the file changed since loading.
            try { _ = ReadExisting(); }
            catch { loadFailed = true; throw; }
            InventoryBankService.ValidateEntries(entries);
            var dto = new FileDto
            {
                SchemaVersion = 1,
                SavedAtUtc = DateTimeOffset.UtcNow,
                Entries = entries.Select(ToDto).ToArray(),
            };
            ValidateItemBounds(dto);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(dto, JsonOptions);
            if (bytes.Length > MaximumBytes) throw new InvalidDataException("Bank file exceeds the size limit.");

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    file.Write(bytes);
                    file.Flush(flushToDisk: true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak", ignoreMetadataErrors: true);
                else File.Move(temporary, path);
            }
            finally
            {
                // Delete only this operation's exact temporary path. Cleanup cannot turn a committed save into failure.
                try { File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private BankEntry[] ReadExisting()
    {
        // File.Exists also returns false on some access failures. Only actual absence is an empty bank.
        try { return ReadFile(path); }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        try { return ReadFile(path + ".bak"); }
        catch (FileNotFoundException) { return []; }
        catch (DirectoryNotFoundException) { return []; }
    }

    private static BankEntry[] ReadFile(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaximumBytes) throw new InvalidDataException("Bank file exceeds the size limit.");
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int length;
        while ((length = stream.Read(chunk)) != 0)
        {
            if (buffer.Length + length > MaximumBytes) throw new InvalidDataException("Bank file exceeds the size limit.");
            buffer.Write(chunk, 0, length);
        }
        try
        {
            using var document = JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            RejectDuplicateProperties(document.RootElement);
            var dto = document.Deserialize<FileDto>(JsonOptions) ?? throw new InvalidDataException("Missing bank document.");
            if (dto.SchemaVersion != 1) throw new InvalidDataException("Unsupported bank schema version.");
            if (dto.SavedAtUtc == default || dto.SavedAtUtc.Offset != TimeSpan.Zero || dto.Entries is null
                || dto.Entries.Length > 4096)
                throw new InvalidDataException("Invalid bank metadata or entry count.");
            ValidateItemBounds(dto);
            var entries = dto.Entries.Select(FromDto).ToArray();
            InventoryBankService.ValidateEntries(entries);
            return entries;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or OverflowException or NullReferenceException)
        {
            throw new InvalidDataException("Invalid bank file.", ex);
        }
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate JSON property.");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item);
    }

    private static void ValidateItemBounds(FileDto dto)
    {
        var total = 0;
        foreach (var entry in dto.Entries)
        {
            if (entry is null || entry.Containers is null || entry.Containers.Length > 7)
                throw new InvalidDataException("Invalid bank containers.");
            foreach (var container in entry.Containers)
            {
                if (container is null || container.Items is null || container.Items.Length > 4096)
                    throw new InvalidDataException("Invalid bank item count.");
                total = checked(total + container.Items.Length);
                if (total > MaximumItems) throw new InvalidDataException("Too many bank items.");
            }
        }
    }

    private static EntryDto ToDto(BankEntry entry) => new()
    {
        CharacterId = entry.Key.CharacterId,
        Kind = entry.Key.Kind,
        StorageId = entry.Key.StorageId,
        Name = entry.Name,
        ObservedAtUtc = entry.Snapshot.ObservedAt,
        Source = entry.Snapshot.Source,
        Containers = entry.Snapshot.Containers.Values.OrderBy(c => c.Location.Container.Value).Select(c => new ContainerDto
        {
            ContainerId = c.Location.Container.Value,
            Slots = c.Stacks.Where(stack => stack.Slot >= 0).Select(stack => new SlotDto
            {
                ItemId = stack.Item.Id.Value, IsHighQuality = stack.Item.IsHighQuality,
                Quantity = stack.Quantity, Slot = stack.Slot, DisplayIndex = stack.DisplayIndex,
            }).ToArray(),
            Items = c.Quantities.OrderBy(q => q.Key.Id.Value).ThenBy(q => q.Key.IsHighQuality).Select(q => new ItemDto
            {
                ItemId = q.Key.Id.Value, IsHighQuality = q.Key.IsHighQuality, Quantity = q.Value,
            }).ToArray(),
        }).ToArray(),
    };

    private static BankEntry FromDto(EntryDto entry)
    {
        if (entry.ObservedAtUtc == default || entry.ObservedAtUtc.Offset != TimeSpan.Zero)
            throw new InvalidDataException("Bank observation time must be UTC.");
        var owner = entry.Kind == BankStorageKind.Retainer ? entry.StorageId : entry.CharacterId;
        var containers = entry.Containers.Select(c =>
        {
            var items = new HashSet<InventoryItem>();
            var stacks = c.Items.Select(i =>
            {
                if (i is null) throw new InvalidDataException("Missing bank item.");
                var item = new InventoryItem(new(i.ItemId), i.IsHighQuality);
                if (!items.Add(item)) throw new InvalidDataException("Duplicate aggregated bank item.");
                return new InventoryStack(item, i.Quantity);
            }).ToArray();
            var aggregate = new InventoryContainerSnapshot(new(new(owner), new(c.ContainerId)), InventoryCoverage.Complete, stacks);
            if (c.Slots is not { Length: > 0 }) return aggregate;
            if (c.Slots.Length > 1000) throw new InvalidDataException("Too many stored slots.");
            var positioned = new InventoryContainerSnapshot(aggregate.Location, InventoryCoverage.Complete,
                c.Slots.Select(slot => slot is null || slot.Slot is < 0 or >= 1000 || slot.DisplayIndex is < -1 or >= 175
                    ? throw new InvalidDataException("Invalid stored slot.")
                    : new InventoryStack(new(new(slot.ItemId), slot.IsHighQuality), slot.Quantity, slot.Slot, slot.DisplayIndex)));
            if (aggregate.Quantities.Count != positioned.Quantities.Count
                || aggregate.Quantities.Any(pair => !positioned.Quantities.TryGetValue(pair.Key, out var count) || count != pair.Value))
                throw new InvalidDataException("Stored slots disagree with quantities.");
            return positioned;
        }).ToArray();
        return new(new(entry.CharacterId, entry.Kind, entry.StorageId), entry.Name,
            new(entry.ObservedAtUtc, entry.Source, containers));
    }

    private sealed class FileDto
    {
        public required int SchemaVersion { get; init; }
        public required DateTimeOffset SavedAtUtc { get; init; }
        public required EntryDto[] Entries { get; init; }
    }

    private sealed class EntryDto
    {
        public required ulong CharacterId { get; init; }
        public required BankStorageKind Kind { get; init; }
        public required ulong StorageId { get; init; }
        public required string Name { get; init; }
        public required DateTimeOffset ObservedAtUtc { get; init; }
        public required string Source { get; init; }
        public required ContainerDto[] Containers { get; init; }
    }

    private sealed class ContainerDto
    {
        public required int ContainerId { get; init; }
        public required ItemDto[] Items { get; init; }
        public SlotDto[]? Slots { get; init; }
    }

    private sealed class SlotDto
    {
        public required uint ItemId { get; init; }
        public required bool IsHighQuality { get; init; }
        public required long Quantity { get; init; }
        public required int Slot { get; init; }
        public required int DisplayIndex { get; init; }
    }

    private sealed class ItemDto
    {
        public required uint ItemId { get; init; }
        public required bool IsHighQuality { get; init; }
        public required long Quantity { get; init; }
    }
}
